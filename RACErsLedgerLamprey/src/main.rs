mod datatypes;
pub use crate::datatypes::*;

use log::{debug, error, info, LevelFilter};
use serde_json::{Result, Value};
use simple_logger::SimpleLogger;
use std::net::TcpListener;
use std::thread::spawn;
use tungstenite::{connect, server::accept, Message};
use url::Url;

use clap::{AppSettings, Clap};

#[derive(Clap)]
#[clap(version = "0.1", author = "Sariya Melody <sariya@sariya.garden>")]
#[clap(setting = AppSettings::ColoredHelp)]
struct Opts {
    /// Port for lamprey to connect to and echo events from
    connect_port: i32,
    /// Port for lamprey to listen on for subclients (i.e. visualizers, other plugins, etc)
    listen_port: i32,
    /// A level of verbosity, and can be used multiple times.
    #[clap(short, long, parse(from_occurrences))]
    verbose: i32,
}

fn main() {
    let opts = Opts::parse();
    SimpleLogger::new()
        .with_level(match opts.verbose {
            0 => LevelFilter::Error,
            1 => LevelFilter::Info,
            2 => LevelFilter::Debug,
            3 | _ => LevelFilter::Trace,
        })
        .init()
        .unwrap();
    info!("starting up server");
    info!(
        "connect port: {}, listen port: {}",
        opts.connect_port, opts.listen_port
    );

    let connect_destination = format!("ws://localhost:{}/racers-ledger/", opts.connect_port);
    let (mut socket, response) = connect(Url::parse(connect_destination.as_str()).unwrap())
        .expect(format!("Can't connect to {}", connect_destination).as_str());
    info!("connected to server");
    info!("response code: {}", response.status());
    loop {
        let msg = socket.read_message().expect("error reading message");
        debug!("received message {}", msg);
        match msg {
            Message::Text(string) => {
                debug!("trying to convert msg to object...");
                let event: Result<SalvageEvent> = serde_json::from_str(string.as_str());
                match event {
                    Ok(salvage_event) => {
                        info!("decoded {:#?}", salvage_event)
                    }
                    Err(e) => {
                        error!("{}", e)
                    }
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
}
