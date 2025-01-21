@echo off
echo Running C# Flexon Examples
echo =========================

echo Building examples...
dotnet build BasicUsage
dotnet build GameState
dotnet build AIData
dotnet build SecureConfig
dotnet build Benchmarking

echo.
echo Running Basic Usage Example
cd BasicUsage
dotnet run
cd ..

echo.
echo Running Game State Example
cd GameState
dotnet run
cd ..

echo.
echo Running AI Data Example
cd AIData
dotnet run
cd ..

echo.
echo Running Secure Config Example
cd SecureConfig
dotnet run
cd ..

echo.
echo Running Benchmarking Example
cd Benchmarking
dotnet run --filter *
cd ..

echo.
echo All examples completed!
