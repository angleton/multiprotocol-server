use std::net::SocketAddr;

#[tokio::main]
async fn main() {
    let _meter_provider = bdd_rust_multiprotocol_server::init_telemetry_provider()
        .expect("failed to initialize OpenTelemetry metrics");
    let addr: SocketAddr = "127.0.0.1:8080".parse().unwrap();

    bdd_rust_multiprotocol_server::run(addr).await;
}
