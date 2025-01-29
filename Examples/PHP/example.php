<?php
require_once 'includes/FlexonConverter.php';

// Create a new converter instance
$converter = new Flexon\FlexonConverter();

// Example 1: Convert JSON to Flexon
$jsonData = json_encode([
    'name' => 'John Doe',
    'age' => 30,
    'email' => 'john@example.com'
]);
$flexonData = $converter->convertToFlexon($jsonData, 'json');
file_put_contents('example.flexon', $flexonData);

// Example 2: Convert Flexon to XML with encryption
$converter->setEncryptionKey('mysecretkey');
$xmlData = $converter->convertFromFlexon($flexonData, 'xml');
file_put_contents('example.xml', $xmlData);

// Example 3: Convert CSV to Flexon with validation and inspection
$csvData = "name,age,email\nJohn Doe,30,john@example.com";
$converter->enableValidation(true);
$converter->enableInspection(true);
$flexonData = $converter->convertToFlexon($csvData, 'csv');
$inspection = $converter->inspect($flexonData);
print_r($inspection);

// Example 4: Convert between multiple formats
$jsonData = '{"items":[{"id":1,"name":"Item 1"},{"id":2,"name":"Item 2"}]}';
$flexonData = $converter->convertToFlexon($jsonData, 'json');
$xmlData = $converter->convertFromFlexon($flexonData, 'xml');
$csvData = $converter->convertFromFlexon($flexonData, 'csv');

echo "Conversion chain complete!\n";
echo "XML output:\n$xmlData\n";
echo "CSV output:\n$csvData\n";
