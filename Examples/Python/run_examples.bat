@echo off
echo Running Python Flexon Examples
echo ============================

echo Installing dependencies...
pip install -r requirements.txt

echo.
echo Running Basic Usage Example
cd BasicUsage
python basic_usage.py
cd ..

echo.
echo Running Game State Example
cd GameState
python game_state.py
cd ..

echo.
echo Running AI Data Example
cd AIData
python ai_data.py
cd ..

echo.
echo Running Secure Config Example
cd SecureConfig
python secure_config.py
cd ..

echo.
echo Running Benchmarking Example
cd Benchmarking
python benchmark.py
cd ..

echo.
echo All examples completed!
