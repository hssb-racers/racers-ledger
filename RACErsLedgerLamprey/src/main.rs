mod datatypes;
use std::sync::Arc;

pub use crate::datatypes::*;

use log::{debug, error, info, LevelFilter};
use serde_json::Result;
use simple_logger::SimpleLogger;
use tokio::net::{TcpListener, TcpStream};
use tokio::sync::broadcast;
use tokio::sync::broadcast::error::RecvError;
use tokio::sync::broadcast::*;
//use std::thread::spawn;
use async_tungstenite::{tokio::connect_async, tungstenite::Message};
use futures::prelude::*;
use url::Url;
extern crate colored;

use clap::{AppSettings, Clap};

#[derive(Clap)]
#[clap(version = "0.1", author = "Sariya Melody <sariya@sariya.garden>")]
#[clap(setting = AppSettings::ColoredHelp)]
struct Opts {
    /// Port for lamprey to connect to and echo events from
    connect_port: i32,
    /// Port for lamprey to listen on for subclients (i.e. visualizers, other plugins, etc)
    listen_port: i32,
    /// Level of logging verbosity. No -v = Error only, -v = Info, -vv = Debug, -vvv = Trace.
    #[clap(short, long, parse(from_occurrences))]
    verbose: i32,
    /// Disable colored output.
    #[clap(long)]
    nocolorize: bool,
    /// Suppress TimeTickEvent printing to console
    #[clap(long)]
    notime_tick: bool,
}

#[tokio::main]
pub async fn main() {
    let opts = Arc::new(Opts::parse());
    SimpleLogger::new()
        .with_level(match opts.verbose {
            0 => LevelFilter::Error,
            1 => LevelFilter::Info,
            2 => LevelFilter::Debug,
            3 | _ => LevelFilter::Trace,
        })
        .init()
        .unwrap();
    if opts.nocolorize {
        colored::control::set_override(false);
    }
    info!("starting up server");
    info!(
        "connect port: {}, listen port: {}",
        opts.connect_port, opts.listen_port
    );

    // Kick off the mod<->lamprey WS connection! We should call this "mod websocket" for consistency...
    let (ledger_event_sender_original, _) = broadcast::channel(512);
    let opts_inner = Arc::clone(&opts);
    let ledger_event_sender = ledger_event_sender_original.clone();
    tokio::spawn(async move {
        let connect_destination =
            format!("ws://localhost:{}/racers-ledger/", opts_inner.connect_port);
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

            debug!("received message {}", msg);
            match msg {
                Message::Text(string) => {
                    debug!("trying to convert msg to object...");
                    let event: Result<SalvageEvent> = serde_json::from_str(string.as_str());
                    if let Ok(salvage_event) = event {
                        // if we ever make the console sink optional this unwrap isn't guaranteed to work so we'll
                        // need to implement some kind of retry logic maybe
                        ledger_event_sender.send(salvage_event).unwrap();
                    }
                }
                Message::Ping(data) => {
                    debug!("received ping! (data: {:?})", data);
                }
                Message::Pong(data) => {
                    debug!("received pong! (data: {:?}", data);
                }
                Message::Binary(data) => {
                    debug!("received binary data: {:?}", data)
                }
                Message::Close(close_frame) => {
                    debug!("received close!");
                    if let Some(close_frame) = close_frame {
                        debug!("close frame info: {:#?}", close_frame)
                    }
                }
            }
        }
    });
    // Spawn a console sink to log when we get new ledger events
    let opts_inner = Arc::clone(&opts);
    let mut ledger_event_receiver = ledger_event_sender_original.subscribe();
    tokio::spawn(async move {
        loop {
            let recv_result = ledger_event_receiver.recv().await;
            match recv_result {
                Ok(salvage_event) => match salvage_event {
                    SalvageEvent::TimeTickEvent { .. } => {
                        debug!("decoded {:#?}", salvage_event);
                        if !opts_inner.notime_tick {
                            println!("{}", salvage_event)
                        }
                    }
                    salvage_event => {
                        debug!("decoded {:#?}", salvage_event);
                        println!("{}", salvage_event)
                    }
                },
                Err(RecvError::Lagged(lagged_messages)) => {
                    error!("console sink missed {} messages :(", lagged_messages)
                }
                Err(RecvError::Closed) => {
                    panic!("somehow the console sink got a RecvError::Closed, this is a problem, bug sariya about it");
                }
            }
        }
    });
    let listener = TcpListener::bind(format!("127.0.0.1:{}", opts.listen_port))
        .await
        .expect("failed to start listener, dying");
    loop {
        let ledger_event_receiver = ledger_event_sender_original.subscribe();
        let (socket, _) = listener.accept().await.unwrap();
        // TODO: handle other routes instead of just websocket listen
        tokio::spawn(async move {
            handle_client_websocket(socket, ledger_event_receiver).await;
        });
    }
}

pub async fn handle_client_websocket(
    socket: TcpStream,
    ledger_event_receiver: Receiver<SalvageEvent>,
) {
}
