<?php
require_once 'includes/FlexonConverter.php';
$config = require_once 'includes/config.php';

session_start();

$converter = new Flexon\FlexonConverter($config);
$message = '';
$result = '';

if ($_SERVER['REQUEST_METHOD'] === 'POST') {
    try {
        if (isset($_FILES['file']) && $_FILES['file']['error'] === UPLOAD_ERR_OK) {
            $sourceData = file_get_contents($_FILES['file']['tmp_name']);
        } else {
            $sourceData = $_POST['content'] ?? '';
        }

        $sourceFormat = $_POST['source_format'];
        $targetFormat = $_POST['target_format'];
        
        if (isset($_POST['encryption_key'])) {
            $converter->setEncryptionKey($_POST['encryption_key']);
        }
        
        $converter->enableValidation(isset($_POST['validate']));
        $converter->enableInspection(isset($_POST['inspect']));

        if ($sourceFormat === 'flexon') {
            $result = $converter->convertFromFlexon($sourceData, $targetFormat);
        } else {
            $result = $converter->convertToFlexon($sourceData, $sourceFormat);
            if ($targetFormat !== 'flexon') {
                $result = $converter->convertFromFlexon($result, $targetFormat);
            }
        }

        if (isset($_POST['inspect'])) {
            $inspection = $converter->inspect($result);
            $result = "Inspection Results:\n" . print_r($inspection, true) . "\n\nConverted Data:\n" . $result;
        }

        $message = 'Conversion successful!';
    } catch (Exception $e) {
        $message = 'Error: ' . $e->getMessage();
    }
}
?>
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Flexon Converter</title>
    <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.1.3/dist/css/bootstrap.min.css" rel="stylesheet">
    <style>
        body { padding: 20px; }
        .container { max-width: 900px; }
        .result { white-space: pre-wrap; }
    </style>
</head>
<body>
    <div class="container">
        <h1 class="mb-4">Flexon Converter</h1>
        
        <?php if ($message): ?>
        <div class="alert alert-<?= strpos($message, 'Error') === 0 ? 'danger' : 'success' ?>">
            <?= htmlspecialchars($message) ?>
        </div>
        <?php endif; ?>

        <form method="post" enctype="multipart/form-data" class="mb-4">
            <div class="row mb-3">
                <div class="col">
                    <label class="form-label">Source Format:</label>
                    <select name="source_format" class="form-select" required>
                        <?php foreach ($config['supported_formats'] as $format => $info): ?>
                        <option value="<?= $format ?>"><?= strtoupper($format) ?></option>
                        <?php endforeach; ?>
                    </select>
                </div>
                <div class="col">
                    <label class="form-label">Target Format:</label>
                    <select name="target_format" class="form-select" required>
                        <?php foreach ($config['supported_formats'] as $format => $info): ?>
                        <option value="<?= $format ?>"><?= strtoupper($format) ?></option>
                        <?php endforeach; ?>
                    </select>
                </div>
            </div>

            <div class="mb-3">
                <label class="form-label">Upload File:</label>
                <input type="file" name="file" class="form-control">
            </div>

            <div class="mb-3">
                <label class="form-label">Or Paste Content:</label>
                <textarea name="content" class="form-control" rows="10"></textarea>
            </div>

            <div class="mb-3">
                <label class="form-label">Encryption Key (optional):</label>
                <input type="password" name="encryption_key" class="form-control">
            </div>

            <div class="mb-3">
                <div class="form-check">
                    <input type="checkbox" name="validate" class="form-check-input" id="validate">
                    <label class="form-check-label" for="validate">Enable Validation</label>
                </div>
                <div class="form-check">
                    <input type="checkbox" name="inspect" class="form-check-input" id="inspect">
                    <label class="form-check-label" for="inspect">Enable Inspection</label>
                </div>
            </div>

            <button type="submit" class="btn btn-primary">Convert</button>
        </form>

        <?php if ($result): ?>
        <div class="card">
            <div class="card-header">
                <h5 class="card-title mb-0">Conversion Result</h5>
            </div>
            <div class="card-body">
                <pre class="result"><?= htmlspecialchars($result) ?></pre>
            </div>
        </div>
        <?php endif; ?>
    </div>

    <script src="https://cdn.jsdelivr.net/npm/bootstrap@5.1.3/dist/js/bootstrap.bundle.min.js"></script>
</body>
</html>
