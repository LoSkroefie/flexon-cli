<?php
return [
    'supported_formats' => [
        'json' => ['mime' => 'application/json', 'extension' => '.json'],
        'xml'  => ['mime' => 'application/xml', 'extension' => '.xml'],
        'bson' => ['mime' => 'application/bson', 'extension' => '.bson'],
        'csv'  => ['mime' => 'text/csv', 'extension' => '.csv'],
        'xsv'  => ['mime' => 'text/csv', 'extension' => '.xsv'],
        'flexon' => ['mime' => 'application/flexon', 'extension' => '.flexon']
    ],
    'upload_dir' => __DIR__ . '/../uploads/',
    'max_file_size' => 10 * 1024 * 1024, // 10MB
    'temp_dir' => sys_get_temp_dir(),
    'encryption' => [
        'method' => 'AES-256-CBC',
        'key_length' => 32
    ],
    'validation' => [
        'enabled' => true,
        'schema_dir' => __DIR__ . '/../schemas/'
    ]
];
