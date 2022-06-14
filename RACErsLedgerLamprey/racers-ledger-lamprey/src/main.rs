use racers_ledger_datatypes::*;

use async_tungstenite::{tokio::connect_async, tungstenite::Message};
use clap::{AppSettings, Clap};
use colored;
use futures::prelude::*;
use serde::Serialize;
use std::{collections::HashMap, sync::Arc};
use tokio::sync::{broadcast, mpsc, oneshot, RwLock};
use tracing::{info, trace, Level};
use tracing_subscriber::fmt::format::FmtSpan;
use url::Url;

#[derive(Clap)]
#[clap(version = "0.2", author = "Sariya Melody <sariya@sariya.garden>")]
#[clap(setting = AppSettings::ColoredHelp)]
struct Opts {
    /// Port for lamprey to connect to and echo events from
    connect_port: u16,
    /// Port for lamprey to listen on for subclients (i.e. visualizers, other plugins, etc)
    listen_port: u16,
    /// Level of logging verbosity. No -v = Error only, -v = Info, -vv = Debug, -vvv = Trace.
    #[clap(short, long, parse(from_occurrences))]
    verbose: i32,
    /// Expose lamprey API on 0.0.0.0 instead of 127.0.0.1?
    #[clap(long)]
    expose: bool,
    /// Pick your favorite log format. Options: full (default), compact, pretty, pretty_and_all_spans (warning: noisy)
    // TODO(sariya) make this an enum somehow lol
    #[clap(long, default_value = "full")]
    log_format: String,
    /// Disable colored output.
    #[clap(long)]
    nocolorize: bool,
    /// Suppress TimeTickEvent printing to console
    #[clap(long)]
    notime_tick: bool,
}

/// State of currently connected clients.
///
/// Key is "ID" (increasing atomic usize handlers::NEXT_USER_ID) (which is gross and tech debt but whatever i'm not dealing with this right now)
/// Value is a handle to send things to that client.
pub type Clients =
    Arc<RwLock<HashMap<usize, mpsc::UnboundedSender<Result<warp::ws::Message, warp::Error>>>>>;

/// Data about the current state-of-the-world. Right now it's just if we're in shift or not. Maybe more eventually.
#[derive(Default, Serialize, Debug)]
pub struct LedgerState {
    in_shift: bool,
}
/// Utility type for what we're actually going to be passing around.
pub type State = Arc<RwLock<LedgerState>>;

/// `filters` is all about Warp routing and how we set it up.
/// API endpoints:
/// - /api/v0/status: Emits the data described in `LedgerState`
/// - /api/v0/racers-ledger-proxy: Websocket endpoint. All data the Lamprey gets from the mod is echoed here.

mod filters {
    use std::convert::Infallible;

    use super::handlers;
    use super::Clients;
    use super::State;
    use warp::Filter;

    /// Describes the entire API we're exporting.
    #[tracing::instrument]
    pub fn api(
        state: State,
        clients: Clients,
    ) -> impl Filter<Extract = impl warp::Reply, Error = warp::Rejection> + Clone {
        warp::path("api")
            .and(warp::path("v0").and(status(state.clone()).or(ledger_proxy(clients.clone()))))
    }

    /// route /api/v0/status
    #[tracing::instrument]
    pub fn status(
        state: State,
    ) -> impl Filter<Extract = impl warp::Reply, Error = warp::Rejection> + Clone {
        warp::path!("status")
            .and(warp::path::end())
            .and(warp::get())
            .and(with_state(state.clone()))
            .and_then(handlers::handle_status)
    }

    /// route /api/v0/racers-ledger-proxy
    #[tracing::instrument]
    pub fn ledger_proxy(
        clients: Clients,
    ) -> impl Filter<Extract = impl warp::Reply, Error = warp::Rejection> + Clone {
        warp::path!("racers-ledger-proxy")
            .and(warp::ws())
            .and(with_clients(clients))
            .map(move |ws: warp::ws::Ws, clients| {
                ws.on_upgrade(move |socket| {
                    handlers::handle_websocket_ledger_proxy_connected(socket, clients)
                })
            })
    }

    /// Warp filter for adding in a State
    #[tracing::instrument]
    fn with_state(state: State) -> impl Filter<Extract = (State,), Error = Infallible> + Clone {
        warp::any().map(move || state.clone())
    }

    /// Warp filter for adding in a Clients
    #[tracing::instrument]
    fn with_clients(
        clients: Clients,
    ) -> impl Filter<Extract = (Clients,), Error = Infallible> + Clone {
        warp::any().map(move || clients.clone())
    }
}

/// `handlers` is all about responding to connections that were routed to us via `filters`.
mod handlers {
    use std::{
        convert::Infallible,
        sync::atomic::{AtomicUsize, Ordering},
    };

    use futures::{FutureExt, StreamExt};
    use log::{debug, error, info};
    use tokio::sync::mpsc;
    use tokio_stream::wrappers::UnboundedReceiverStream;
    use warp::ws::WebSocket;

    use super::Clients;
    use super::State;

    /// global unique user id counter, key for Clients
    static NEXT_USER_ID: AtomicUsize = AtomicUsize::new(1);

    /// When websocket clients connect, stick 'em in Clients.
    #[tracing::instrument]
    pub async fn handle_websocket_ledger_proxy_connected(websocket: WebSocket, clients: Clients) {
        let my_id = NEXT_USER_ID.fetch_add(1, Ordering::Relaxed);
        let (user_ws_tx, mut user_ws_rx) = websocket.split();
        let (tx, rx) = mpsc::unbounded_channel();
        let rx = UnboundedReceiverStream::new(rx);
        debug!("new client connected wooooo");
        tokio::task::spawn(rx.forward(user_ws_tx).map(|result| {
            if let Err(e) = result {
                error!("websocket send error: {}", e);
            }
        }));
        clients.write().await.insert(my_id, tx);
        while let Some(result) = user_ws_rx.next().await {
            match result {
                Err(e) => {
                    error!("websocket error (uid={}): {}", my_id, e);
                    break;
                }
                _ => {}
            };
        }
        handle_websocket_ledger_proxy_disconnected(my_id, &clients).await;
    }

    /// Internal helper function for its `connected` counterpart.
    #[tracing::instrument]
    async fn handle_websocket_ledger_proxy_disconnected(my_id: usize, clients: &Clients) {
        info!("disconnecting websocket user {}", my_id);
        clients.write().await.remove(&my_id);
    }

    /// When clients query for status via the API, here's how it gets to them.
    #[tracing::instrument]
    pub async fn handle_status(state: State) -> Result<impl warp::Reply, Infallible> {
        let state = state.read().await;
        Ok(warp::reply::json(&*state))
    }
}

/// `sinks` is all of the long-running internal "helper processes" that keep an eye on what's happening in the
/// `ledger_events_receiver` broadcast channel and help accordingly.
mod sinks {
    use super::Clients;
    use super::State;
    use log::{debug, error, trace};
    use serde_json::json;
    use tokio::sync::broadcast::{error::RecvError, Receiver};
    use warp::ws::Message;

    use racers_ledger_datatypes::SalvageEvent;

    /// Handles actually telling our proxy clients about ledger event updates.
    #[tracing::instrument]
    pub async fn websocket_client_updater_sink(
        mut ledger_events_receiver: Receiver<SalvageEvent>,
        clients: Clients,
    ) {
        loop {
            let recv_result = ledger_events_receiver.recv().await;
            match recv_result {
                Ok(salvage_event) => {
                    for (client_id, tx) in clients.read().await.iter() {
                        debug!("attempted to send data to client {}", client_id);
                        let json = serde_json::to_string(&salvage_event).unwrap_or_else(|_| {
                            error!(
                                "somehow failed to serialize salvage event to string: {:#?}",
                                salvage_event
                            );
                            json!({
                                "type": "error",
                                "message": "could not serialize salvage event :("
                            })
                            .to_string()
                        });
                        if let Err(_disconnected) = tx.send(Ok(Message::text(json))) {
                            // the tx is disconnected.
                        }
                    }
                }
                Err(RecvError::Lagged(lagged_messages)) => {
                    error!(
                        "websocket client updater sink missed {} messages :(",
                        lagged_messages
                    )
                }
                Err(RecvError::Closed) => {
                    error!("somehow the websocket client updater sink got a RecvError::Closed, this is a problem if it happened when not shutting down the game, bug sariya about it");
                }
            }
        }
    }

    /// Log to the console!
    #[tracing::instrument]
    pub async fn console_sink(
        mut ledger_events_receiver: Receiver<SalvageEvent>,
        log_time_tick: bool,
    ) {
        loop {
            let recv_result = ledger_events_receiver.recv().await;
            match recv_result {
                Ok(salvage_event) => match salvage_event {
                    SalvageEvent::TimeTickEvent { .. } => {
                        trace!("received {:#?}", salvage_event);
                        if log_time_tick {
                            println!("{}", salvage_event)
                        }
                    }
                    salvage_event => {
                        trace!("received {:#?}", salvage_event);
                        println!("{}", salvage_event)
                    }
                },
                Err(RecvError::Lagged(lagged_messages)) => {
                    error!("console sink missed {} messages :(", lagged_messages)
                }
                Err(RecvError::Closed) => {
                    error!("somehow the console sink got a RecvError::Closed, this is a problem if it happened when not shutting down the game, bug sariya about it");
                }
            }
        }
    }

    /// Update the `State` struct so that clients asking for it later can have the most up-to-date state!
    #[tracing::instrument]
    pub async fn state_updater_sink(
        mut ledger_events_receiver: Receiver<SalvageEvent>,
        state: State,
    ) {
        loop {
            let recv_result = ledger_events_receiver.recv().await;
            match recv_result {
                Ok(salvage_event) => match salvage_event {
                    SalvageEvent::StartShiftEvent { .. } => {
                        debug!("startshift event received, updating state");
                        let mut state = state.write().await;
                        (*state).in_shift = true;
                        debug!("startshift event done updating state");
                    }
                    SalvageEvent::EndShiftEvent { .. } => {
                        debug!("endshift event received, updating state");
                        let mut state = state.write().await;
                        (*state).in_shift = false;
                        debug!("endshift event done updating state");
                    }
                    _ => {}
                },
                Err(RecvError::Lagged(lagged_messages)) => {
                    error!("status updater sink missed {} messages :(", lagged_messages)
                }
                Err(RecvError::Closed) => {
                    error!("somehow the status updater sink got a RecvError::Closed, this is a problem if it happened when not shutting down the game, bug sariya about it");
                }
            }
        }
    }
}

#[tokio::main]
pub async fn main() {
    let opts = Arc::new(Opts::parse());
    let max_level = match opts.verbose {
        0 => Level::ERROR,
        1 => Level::INFO,
        2 => Level::DEBUG,
        3 | _ => Level::TRACE,
    };
    // TODO(sariya) this could probably stand to be better but i don't really care right this second
    match opts.log_format.to_lowercase().as_str() {
        "full" => {
            tracing_subscriber::fmt()
                .with_max_level(max_level)
                .with_thread_names(true)
                .init();
        }
        "compact" => {
            tracing_subscriber::fmt()
                .with_max_level(max_level)
                .with_thread_names(true)
                .compact()
                .init();
        }
        "pretty" => {
            tracing_subscriber::fmt()
                .with_max_level(max_level)
                .with_thread_names(true)
                .pretty()
                .init();
        }
        "pretty_and_all_spans" => {
            tracing_subscriber::fmt()
                .with_max_level(max_level)
                .with_thread_ids(true)
                .with_thread_names(true)
                .with_span_events(FmtSpan::FULL)
                .pretty()
                .init();
        }
        _ => {
            panic!("you gave an invalid logging format and now i must die");
        }
    };

    if opts.nocolorize {
        colored::control::set_override(false);
    }
    info!("starting up server");
    info!(
        "connect port: {}, listen port: {}, listen address: {}",
        opts.connect_port,
        opts.listen_port,
        (if opts.expose { "0.0.0.0" } else { "127.0.0.1" })
    );

    // State we'll need to share with our components later.
    let clients = Clients::default();
    let state = State::default();

    // Single-use channel specifically for shutting down gracefully.
    let (shutdown_tx, shutdown_rx) = oneshot::channel();

    // Kick off the mod<->lamprey WS connection!
    // (when referring to this connection, we should call this "mod websocket" for consistency...)
    let (ledger_events_sender_original, _) = broadcast::channel(512);
    let opts_clone = Arc::clone(&opts);
    let ledger_events_sender = ledger_events_sender_original.clone();
    let clients_clone = clients.clone();
    tokio::spawn(async move {
        let connect_destination =
            format!("ws://localhost:{}/racers-ledger/", opts_clone.connect_port);
        let (websocketstream, response) =
            connect_async(Url::parse(connect_destination.as_str()).unwrap())
                .await
                .expect(format!("Can't connect to {}", connect_destination).as_str());
        info!("connected to server");
        info!("response code: {}", response.status());
        let (_, mut websocket_rx) = websocketstream.split();
        loop {
            let msg = websocket_rx.next().await;
            if msg.is_none() {
                continue;
            };
            let msg = msg
                .unwrap()
                .expect("ran into an error with the message somehow i guess");

            trace!("received message {}", msg);
            match msg {
                Message::Text(string) => {
                    trace!("trying to convert msg to object...");
                    let event: Result<SalvageEvent, serde_json::Error> =
                        serde_json::from_str(string.as_str());
                    if let Ok(salvage_event) = event {
                        // if we ever make ALL of the sinks optional this unwrap isn't guaranteed to work so we'll
                        // need to implement some kind of retry logic maybe
                        ledger_events_sender.send(salvage_event).unwrap();
                    }
                }
                Message::Ping(data) => {
                    trace!("received ping! (data: {:?})", data);
                }
                Message::Pong(data) => {
                    trace!("received pong! (data: {:?}", data);
                }
                Message::Binary(data) => {
                    trace!("received binary data: {:?}", data)
                }
                Message::Close(close_frame) => {
                    trace!("received close!");
                    // default code 1000 "normal closure"
                    // see https://developer.mozilla.org/en-US/docs/Web/API/CloseEvent for meanings of codes
                    let code = 1000_u16;
                    let reason = "game closed! (probably)";
                    if let Some(close_frame) = close_frame {
                        // TODO(sariya) pass down the code/reason to consumers?
                        trace!("close frame info: {:#?}", close_frame);
                    }

                    // server died, let's clean up and tell our clients and die too
                    // TODO(sariya) this should probably be in the updater sink, but it
                    // unfortunately needs info to data (the message::close frame)
                    for (_, tx) in clients_clone.read().await.iter() {
                        if let Err(_disconnected) =
                            tx.send(Ok(warp::ws::Message::close_with(code, reason)))
                        {
                            // the tx is disconnected and already gone
                        }
                    }
                    // let's get the webserver shut down too, now!
                    shutdown_tx
                        .send(())
                        .expect("somehow failed sending the shutdown signal lmao");
                    break;
                }
            }
        }
    });

    // Spawn a console sink to log when we get new ledger events
    let opts_clone = Arc::clone(&opts);
    let ledger_events_receiver = ledger_events_sender_original.subscribe();
    tokio::spawn(async move {
        sinks::console_sink(ledger_events_receiver, !opts_clone.notime_tick).await
    });

    // Spawn a state updater sink to keep abreast of when the game state changes
    let ledger_events_receiver = ledger_events_sender_original.subscribe();
    let state_clone = state.clone();
    tokio::spawn(
        async move { sinks::state_updater_sink(ledger_events_receiver, state_clone).await },
    );

    // Spawn a sink for sending all of our proxy clients the ledger events!
    let ledger_events_receiver = ledger_events_sender_original.subscribe();
    let clients_clone = clients.clone();
    tokio::spawn(async move {
        sinks::websocket_client_updater_sink(ledger_events_receiver, clients_clone).await
    });

    // let's actually serve our API to the world (or, at least localhost) now!
    let server = warp::serve(filters::api(state.clone(), clients.clone()));
    let bind_address = if opts.expose {
        [0, 0, 0, 0]
    } else {
        [127, 0, 0, 1]
    };
    let (_, server) =
        server.bind_with_graceful_shutdown((bind_address, opts.listen_port), async move {
            shutdown_rx.await.ok();
        });
    tokio::spawn(server)
        .await
        .expect("somehow failed spawning the server (oops)");
}
