@echo off
echo Running Go Flexon Examples
echo ========================

echo Building all examples...
cd basicusage
go build
cd ..

cd gamestate
go build
cd ..

cd aidata
go build
cd ..

cd secureconfig
go build
cd ..

cd benchmarking
go build
cd ..

echo.
echo Running Basic Usage Example
cd basicusage
basicusage.exe
cd ..

echo.
echo Running Game State Example
cd gamestate
gamestate.exe
cd ..

echo.
echo Running AI Data Example
cd aidata
aidata.exe
cd ..

echo.
echo Running Secure Config Example
cd secureconfig
secureconfig.exe
cd ..

echo.
echo Running Benchmarking Example
cd benchmarking
benchmarking.exe
cd ..

echo.
echo All examples completed!
