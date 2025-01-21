@echo off
echo Running Rust Flexon Examples
echo ==========================

echo Building all examples...
cargo build --release

echo.
echo Running Basic Usage Example
cd basicusage
cargo run --release
cd ..

echo.
echo Running Game State Example
cd gamestate
cargo run --release
cd ..

echo.
echo Running AI Data Example
cd aidata
cargo run --release
cd ..

echo.
echo Running Secure Config Example
cd secureconfig
cargo run --release
cd ..

echo.
echo Running Benchmarking Example
cd benchmarking
cargo run --release
cd ..

echo.
echo Running Criterion Benchmarks
cd benchmarking
cargo bench
cd ..

echo.
echo All examples completed!
