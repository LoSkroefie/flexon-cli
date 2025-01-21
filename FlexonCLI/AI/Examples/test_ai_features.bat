@echo off
echo Testing AI Features with Flexon

REM Test 1: Serialize AI prompt with schema validation
echo Test 1: Serializing AI prompt...
flexon-cli serialize -i prompt_example.json -o prompt.flexon -s ../Schemas/prompt_schema.json

REM Test 2: Serialize with encryption
echo Test 2: Serializing with encryption...
flexon-cli serialize -i prompt_example.json -o prompt_encrypted.flexon -s ../Schemas/prompt_schema.json -e mysecretkey ChaCha20

REM Test 3: Serialize training data
echo Test 3: Serializing training data...
flexon-cli serialize -i training_example.json -o training.flexon -s ../Schemas/training_schema.json

REM Test 4: Deserialize and verify
echo Test 4: Deserializing data...
flexon-cli deserialize -i prompt.flexon -o prompt_decoded.json
flexon-cli deserialize -i prompt_encrypted.flexon -o prompt_decrypted.json -e mysecretkey

REM Test 5: Run benchmarks
echo Test 5: Running benchmarks...
flexon-cli benchmark -i training_example.json -o benchmark.flexon -b

echo.
echo All tests completed. Check the output files for results.
