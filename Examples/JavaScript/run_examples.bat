@echo off
echo Running JavaScript Flexon Examples
echo ================================

echo Installing dependencies...
npm install

echo.
echo Running Basic Usage Example
cd BasicUsage
node basic_usage.js
cd ..

echo.
echo Running Game State Example
cd GameState
node game_state.js
cd ..

echo.
echo Running AI Data Example
cd AIData
node ai_data.js
cd ..

echo.
echo Running Secure Config Example
cd SecureConfig
node secure_config.js
cd ..

echo.
echo Running Benchmarking Example
cd Benchmarking
node benchmark.js
cd ..

echo.
echo All examples completed!
