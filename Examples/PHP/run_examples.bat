@echo off
echo Running PHP Flexon Examples
echo =========================

echo Installing dependencies...
cd basicusage
composer install
cd ..

cd gamestate
composer install
cd ..

cd aidata
composer install
cd ..

cd secureconfig
composer install
cd ..

cd benchmarking
composer install
cd ..

echo.
echo Running Basic Usage Example
cd basicusage
php basic_usage.php
cd ..

echo.
echo Running Game State Example
cd gamestate
php game_state.php
cd ..

echo.
echo Running AI Data Example
cd aidata
php ai_data.php
cd ..

echo.
echo Running Secure Config Example
cd secureconfig
php secure_config.php
cd ..

echo.
echo Running Benchmarking Example
cd benchmarking
php benchmarking.php
cd ..

echo.
echo All examples completed!
