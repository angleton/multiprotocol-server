# Multiprotocol Server

## Overview
The Multiprotocol Server is designed to handle multiple communication protocols, with an initial implementation of the FIX protocol using asynchronous Rust. This server can accept TCP connections, process incoming messages, and respond appropriately based on the message type.

## Rust Implementation
The Rust implementation is located in the `Rust/src/protocols/fix/mod.rs` file. It includes the following main functions:
- `serve`: Binds to a specified address and listens for incoming TCP connections.
- `handle_connection`: Manages the communication with a connected client, processing messages and sending responses.
- `is_heartbeat`: Checks if the incoming message is a heartbeat message.
- `heartbeat_message`: Constructs a response for a heartbeat message.
- `reject_message`: Constructs a response for unsupported messages.
- `build_message`: Builds a complete FIX message including a checksum.

## Setup
To set up the Rust server, ensure you have Rust and Cargo installed. Clone the repository and navigate to the `Rust` directory. Use the following command to run the server:

```bash
cargo run
```

## Usage
Once the server is running, it will listen for incoming FIX protocol messages. Clients can connect to the server and send messages according to the FIX protocol specifications. The server will respond to heartbeat messages and reject unsupported messages.

## Contributing
Contributions are welcome! Please feel free to submit issues or pull requests for enhancements or bug fixes.