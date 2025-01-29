<?php
namespace Flexon;

class FlexonConverter {
    private $config;
    private $encryptionKey;
    private $validationEnabled;
    private $inspectEnabled;

    // Magic number and version as per binary format spec
    const MAGIC_NUMBER = 0x464C584E; // "FLXN"
    const VERSION = 0x01;

    public function __construct($config = null) {
        $this->config = $config ?? require_once('config.php');
        $this->validationEnabled = false;
        $this->inspectEnabled = false;
    }

    public function setEncryptionKey($key) {
        $this->encryptionKey = $key;
    }

    public function enableValidation($enabled = true) {
        $this->validationEnabled = $enabled;
    }

    public function enableInspection($enabled = true) {
        $this->inspectEnabled = $enabled;
    }

    public function convertToFlexon($data, $sourceFormat) {
        $decoded = $this->decodeSource($data, $sourceFormat);
        return $this->encodeFlexon($decoded);
    }

    public function convertFromFlexon($flexonData, $targetFormat) {
        $decoded = $this->decodeFlexon($flexonData);
        return $this->encodeTarget($decoded, $targetFormat);
    }

    private function decodeSource($data, $format) {
        switch (strtolower($format)) {
            case 'json':
                return json_decode($data, true);
            case 'xml':
                return $this->xmlToArray($data);
            case 'bson':
                return $this->bsonToArray($data);
            case 'csv':
            case 'xsv':
                return $this->csvToArray($data);
            default:
                throw new \Exception("Unsupported source format: $format");
        }
    }

    private function encodeTarget($data, $format) {
        switch (strtolower($format)) {
            case 'json':
                return json_encode($data, JSON_PRETTY_PRINT);
            case 'xml':
                return $this->arrayToXml($data);
            case 'bson':
                return $this->arrayToBson($data);
            case 'csv':
            case 'xsv':
                return $this->arrayToCsv($data);
            default:
                throw new \Exception("Unsupported target format: $format");
        }
    }

    private function encodeFlexon($data) {
        $header = pack('N', self::MAGIC_NUMBER);  // 4 bytes magic
        $header .= pack('C', self::VERSION);      // 1 byte version
        $flags = 0;
        if ($this->validationEnabled) $flags |= 0x02;
        if ($this->encryptionKey) $flags |= 0x04;
        $header .= pack('C', $flags);             // 1 byte flags
        $header .= pack('n', 0);                  // 2 bytes reserved

        $content = serialize($data);
        if ($this->encryptionKey) {
            $content = $this->encrypt($content);
        }

        $size = strlen($content);
        $header .= pack('N', $size);              // 4 bytes content size

        return $header . $content;
    }

    private function decodeFlexon($flexonData) {
        // Read header (12 bytes total)
        $magic = unpack('N', substr($flexonData, 0, 4))[1];
        if ($magic !== self::MAGIC_NUMBER) {
            throw new \Exception("Invalid Flexon file format");
        }

        $version = ord($flexonData[4]);
        $flags = ord($flexonData[5]);
        // Skip 2 reserved bytes
        $size = unpack('N', substr($flexonData, 8, 4))[1];

        $content = substr($flexonData, 12);
        if ($flags & 0x04) { // Encryption flag
            if (!$this->encryptionKey) {
                throw new \Exception("Encryption key required");
            }
            $content = $this->decrypt($content);
        }

        return unserialize($content);
    }

    private function encrypt($data) {
        $iv = openssl_random_pseudo_bytes(16);
        $encrypted = openssl_encrypt(
            $data,
            'AES-256-CBC',
            $this->encryptionKey,
            OPENSSL_RAW_DATA,
            $iv
        );
        return $iv . $encrypted;
    }

    private function decrypt($data) {
        $iv = substr($data, 0, 16);
        $encrypted = substr($data, 16);
        return openssl_decrypt(
            $encrypted,
            'AES-256-CBC',
            $this->encryptionKey,
            OPENSSL_RAW_DATA,
            $iv
        );
    }

    // Format-specific conversion methods
    private function xmlToArray($xml) {
        $xml = simplexml_load_string($xml);
        return json_decode(json_encode($xml), true);
    }

    private function arrayToXml($array, $root = 'root') {
        $xml = new \SimpleXMLElement("<?xml version=\"1.0\"?><$root></$root>");
        $this->arrayToXmlRecursive($array, $xml);
        return $xml->asXML();
    }

    private function arrayToXmlRecursive($array, &$xml) {
        foreach ($array as $key => $value) {
            if (is_array($value)) {
                if (is_numeric($key)) {
                    $key = 'item' . $key;
                }
                $subnode = $xml->addChild($key);
                $this->arrayToXmlRecursive($value, $subnode);
            } else {
                if (is_numeric($key)) {
                    $key = 'item' . $key;
                }
                $xml->addChild($key, htmlspecialchars($value));
            }
        }
    }

    private function bsonToArray($bson) {
        if (!extension_loaded('mongodb')) {
            throw new \Exception("MongoDB extension required for BSON support");
        }
        return \MongoDB\BSON\toPHP($bson);
    }

    private function arrayToBson($array) {
        if (!extension_loaded('mongodb')) {
            throw new \Exception("MongoDB extension required for BSON support");
        }
        return \MongoDB\BSON\fromPHP($array);
    }

    private function csvToArray($csv) {
        $lines = str_getcsv($csv, "\n");
        $array = array();
        $headers = str_getcsv(array_shift($lines));
        foreach ($lines as $line) {
            $row = array();
            $values = str_getcsv($line);
            foreach ($headers as $i => $header) {
                $row[$header] = $values[$i] ?? '';
            }
            $array[] = $row;
        }
        return $array;
    }

    private function arrayToCsv($array) {
        if (empty($array)) return '';
        
        $output = fopen('php://temp', 'r+');
        // Write headers
        fputcsv($output, array_keys(reset($array)));
        // Write data
        foreach ($array as $row) {
            fputcsv($output, $row);
        }
        rewind($output);
        $csv = stream_get_contents($output);
        fclose($output);
        return $csv;
    }

    // Validation and inspection methods
    public function validate($data) {
        if (!$this->validationEnabled) return true;
        
        // Implement validation logic based on schema
        // This is a basic implementation
        if (is_array($data)) {
            foreach ($data as $key => $value) {
                if (!$this->validateValue($value)) {
                    return false;
                }
            }
        }
        return true;
    }

    private function validateValue($value) {
        if (is_array($value)) {
            return $this->validate($value);
        }
        return true;
    }

    public function inspect($data) {
        if (!$this->inspectEnabled) return null;
        
        return [
            'type' => gettype($data),
            'size' => is_string($data) ? strlen($data) : count($data),
            'structure' => $this->inspectStructure($data)
        ];
    }

    private function inspectStructure($data, $depth = 0) {
        if ($depth > 5) return '[Max depth reached]';
        
        if (is_array($data)) {
            $structure = [];
            foreach ($data as $key => $value) {
                $structure[$key] = $this->inspectStructure($value, $depth + 1);
            }
            return $structure;
        }
        
        return gettype($data);
    }
}
