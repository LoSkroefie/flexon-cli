@echo off
echo Running Java Flexon Examples
echo ==========================

echo Building all examples...
mvn clean package

echo.
echo Running Basic Usage Example
cd BasicUsage
java -jar target/basic-usage-1.0-SNAPSHOT-jar-with-dependencies.jar
cd ..

echo.
echo Running Game State Example
cd GameState
java -jar target/game-state-1.0-SNAPSHOT-jar-with-dependencies.jar
cd ..

echo.
echo Running AI Data Example
cd AIData
java -jar target/ai-data-1.0-SNAPSHOT-jar-with-dependencies.jar
cd ..

echo.
echo Running Secure Config Example
cd SecureConfig
java -jar target/secure-config-1.0-SNAPSHOT-jar-with-dependencies.jar
cd ..

echo.
echo Running Benchmarking Example
cd Benchmarking
java -jar target/benchmarking-1.0-SNAPSHOT-jar-with-dependencies.jar
cd ..

echo.
echo All examples completed!
