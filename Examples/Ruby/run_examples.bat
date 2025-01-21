@echo off
echo Running Ruby Flexon Examples
echo ==========================

echo Installing dependencies...
cd basicusage
bundle install
cd ..

cd gamestate
bundle install
cd ..

cd aidata
bundle install
cd ..

cd secureconfig
bundle install
cd ..

cd benchmarking
bundle install
cd ..

echo.
echo Running Basic Usage Example
cd basicusage
ruby basic_usage.rb
cd ..

echo.
echo Running Game State Example
cd gamestate
ruby game_state.rb
cd ..

echo.
echo Running AI Data Example
cd aidata
ruby ai_data.rb
cd ..

echo.
echo Running Secure Config Example
cd secureconfig
ruby secure_config.rb
cd ..

echo.
echo Running Benchmarking Example
cd benchmarking
ruby benchmarking.rb
cd ..

echo.
echo All examples completed!
