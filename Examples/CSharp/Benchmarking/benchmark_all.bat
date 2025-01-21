@echo off
setlocal

:: Set paths
set FLEXON_CLI=..\..\..\..\FlexonCLI\bin\Release\net8.0\FlexonCLI.exe
set OUTPUT_DIR=results

:: Create output directory
if not exist %OUTPUT_DIR% mkdir %OUTPUT_DIR%

:: Test data sizes
set SIZES=1000 10000 100000

:: Compression methods
set COMPRESSION=None GZip Deflate Brotli

:: Test cases
echo Running benchmarks...
for %%s in (%SIZES%) do (
    echo Testing with %%s records...
    
    :: Generate test data
    dotnet run --no-build -c Release -- %%s %OUTPUT_DIR%\data_%%s.json
    
    :: Test each compression method
    for %%c in (%COMPRESSION%) do (
        echo Testing %%c compression...
        
        :: Standard
        %FLEXON_CLI% benchmark -i %OUTPUT_DIR%\data_%%s.json -o %OUTPUT_DIR%\data_%%s_%%c.flexon -c %%c
        
        :: Encrypted
        %FLEXON_CLI% benchmark -i %OUTPUT_DIR%\data_%%s.json -o %OUTPUT_DIR%\data_%%s_%%c_encrypted.flexon -c %%c -e benchmark_key AES256
    )
)

echo Benchmarks complete. Results in %OUTPUT_DIR%
