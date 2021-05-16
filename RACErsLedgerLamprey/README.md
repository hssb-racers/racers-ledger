# RACErs Ledger Lamprey

Super simple websockets-based event bus proxy written in Rust. Also kinda a learning project for me, but this should be fully "production" usable for any RACEing needs.

Goals:
 - enough performance/throughput to deliver at least 100 events per second per client with 5 clients (spitballed worst case for performance)
 - just enough flexibility to not need to rewrite this every time I want to add a new feature to the ledger or companion

 Non-goals:
- enough flexibility to work with anything that isn't the ledger as a host


## API
API is currently at version 0. This implies that it is totally unstable and can change on any release with no warning.

Currently provided:

| route | description |
| ----- | ----------- |
| `/api/v0/status` | JSON document containing current game state. Currently this is `{in_shift: bool}`. |
| `/api/v0/racers-ledger-proxy` | Websocket endpoint. Connect to it and the lamprey server will stream every salvage event it hears about from the mod directly to you. |


## What's a lamprey?

from a conversation with a friend:

>so a lamprey is a little seacreature that gets its nutrition by latching onto another, larger fish
>the software term lamprey is a program that handles heavier processing externally to a bigger business logic binary
>kinda like instead of linking in the sqlite library to some tool you're building, you could ship a program that connects to sqlite and translates it into a REST API or something
>which prevents you from needing to interface with SQLite directly yourself
>in this case it's all about externalizing multiplexing the event stream -- the mod itself doesnt want to deal with a bunch of clients (graph visualizer, maybe a bingo auto-checker, maybe some other stuff) connecting to it and having to write the same data to multiple clients, that's slow and we're on a time budget (since events are delayed from being released until all of their handlers have returned)