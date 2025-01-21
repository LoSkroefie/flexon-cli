using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Linq;
using System.Drawing;
using System.Diagnostics;

public enum EncryptionAlgorithm
{
    AES256,
    ChaCha20,
    TripleDES
}

public enum CompressionMethod
{
    None,
    GZip,
    Deflate,
    Brotli
}

public class EncryptionOptions
{
    public EncryptionAlgorithm Algorithm { get; set; }
    public string Key { get; set; }

    public EncryptionOptions(string key, EncryptionAlgorithm algorithm = EncryptionAlgorithm.AES256)
    {
        Key = key;
        Algorithm = algorithm;
    }

    public static EncryptionOptions Parse(string[] args, int startIndex)
    {
        if (args.Length <= startIndex) return null;
        
        var key = args[startIndex];
        var algorithm = EncryptionAlgorithm.AES256;

        if (args.Length > startIndex + 1)
        {
            if (Enum.TryParse(args[startIndex + 1], true, out EncryptionAlgorithm parsedAlgorithm))
            {
                algorithm = parsedAlgorithm;
            }
        }

        return new EncryptionOptions(key, algorithm);
    }
}

public class SerializationOptions
{
    public List<string> InputFiles { get; set; } = new();
    public string OutputFile { get; set; }
    public string SchemaFile { get; set; }
    public EncryptionOptions Encryption { get; set; }
    public CompressionMethod Compression { get; set; } = CompressionMethod.GZip;
    public bool Benchmark { get; set; }

    public static SerializationOptions ParseFromArgs(string[] args)
    {
        var options = new SerializationOptions();
        for (int i = 1; i < args.Length; i++)
        {
            switch (args[i].ToLower())
            {
                case "-i":
                case "--input":
                    if (++i < args.Length) options.InputFiles.Add(args[i]);
                    break;
                case "-o":
                case "--output":
                    if (++i < args.Length) options.OutputFile = args[i];
                    break;
                case "-s":
                case "--schema":
                    if (++i < args.Length) options.SchemaFile = args[i];
                    break;
                case "-c":
                case "--compression":
                    if (++i < args.Length && Enum.TryParse(args[i], true, out CompressionMethod method))
                    {
                        options.Compression = method;
                    }
                    break;
                case "-e":
                case "--encrypt":
                    if (++i < args.Length)
                    {
                        string key = args[i];
                        EncryptionAlgorithm algo = EncryptionAlgorithm.AES256;
                        if (i + 1 < args.Length && Enum.TryParse(args[i + 1], true, out EncryptionAlgorithm parsedAlgo))
                        {
                            algo = parsedAlgo;
                            i++;
                        }
                        options.Encryption = new EncryptionOptions(key, algo);
                    }
                    break;
                case "-b":
                case "--benchmark":
                    options.Benchmark = true;
                    break;
            }
        }
        return options;
    }
}

public class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("=====================================");
        Console.WriteLine(" FLEXON CLI Utility v1.2.0");
        Console.WriteLine(" Developed by JVR Software");
        Console.WriteLine("=====================================\n");

        if (args.Length < 1)
        {
            DisplayUsage();
            return;
        }

        try
        {
            var command = args[0].ToLower();
            switch (command)
            {
                case "help":
                case "--help":
                case "-h":
                    if (args.Length > 1)
                    {
                        DisplayCommandHelp(args[1].ToLower());
                    }
                    else
                    {
                        DisplayUsage();
                    }
                    break;

                case "serialize":
                    var serializeOptions = SerializationOptions.ParseFromArgs(args);
                    if (serializeOptions.InputFiles.Count == 0 || string.IsNullOrEmpty(serializeOptions.OutputFile))
                    {
                        throw new ArgumentException("Input and output files are required for serialization.");
                    }
                    SerializeData(serializeOptions);
                    break;

                case "deserialize":
                    var deserializeOptions = SerializationOptions.ParseFromArgs(args);
                    if (string.IsNullOrEmpty(deserializeOptions.InputFiles.FirstOrDefault()) || 
                        string.IsNullOrEmpty(deserializeOptions.OutputFile))
                    {
                        throw new ArgumentException("Input and output files are required for deserialization.");
                    }
                    DeserializeData(deserializeOptions);
                    break;

                case "benchmark":
                    var benchmarkOptions = SerializationOptions.ParseFromArgs(args);
                    if (string.IsNullOrEmpty(benchmarkOptions.InputFiles.FirstOrDefault()))
                    {
                        throw new ArgumentException("Input file is required for benchmark.");
                    }
                    RunBenchmark(benchmarkOptions);
                    break;

                // Keep existing commands for backward compatibility
                case "encode":
                case "decode":
                case "inspect":
                case "validate":
                case "encrypt":
                case "decrypt":
                    HandleLegacyCommands(command, args);
                    break;

                default:
                    throw new ArgumentException("Invalid command. Use 'serialize', 'deserialize', or 'benchmark'.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    private static void SerializeData(SerializationOptions options)
    {
        var stopwatch = options.Benchmark ? Stopwatch.StartNew() : null;
        var data = new Dictionary<string, object>();

        foreach (var inputFile in options.InputFiles)
        {
            var fileInfo = new FileInfo(inputFile);
            var fileExtension = fileInfo.Extension.ToLower();

            object fileData;
            switch (fileExtension)
            {
                case ".json":
                    var jsonContent = File.ReadAllText(inputFile);
                    fileData = JsonSerializer.Deserialize<object>(jsonContent);
                    break;

                case ".png":
                case ".jpg":
                case ".jpeg":
                case ".bmp":
                case ".gif":
                    fileData = File.ReadAllBytes(inputFile);
                    break;

                default:
                    // For unknown types, store as binary
                    fileData = File.ReadAllBytes(inputFile);
                    break;
            }

            data[fileInfo.Name] = fileData;
        }

        // Validate against schema if provided
        if (!string.IsNullOrEmpty(options.SchemaFile))
        {
            var schemaJson = File.ReadAllText(options.SchemaFile);
            var schema = JsonSerializer.Deserialize<JsonElement>(schemaJson);
            var errors = new List<string>();
            
            if (!FlexonBinary.Validate(data, schema, errors: errors))
            {
                throw new Exception($"Schema validation failed:\n{string.Join("\n", errors)}");
            }
        }

        using var outputStream = new FileStream(options.OutputFile, FileMode.Create);
        using var targetStream = options.Encryption != null ? 
            GetEncryptionStream(outputStream, options.Encryption) : outputStream;
        
        // Write compression method as first byte
        targetStream.WriteByte((byte)options.Compression);
        
        using var compressedStream = GetCompressionStream(targetStream, options.Compression, true);
        using var writer = new BinaryWriter(compressedStream);
        
        FlexonBinary.Encode(data, writer);

        if (options.Benchmark && stopwatch != null)
        {
            stopwatch.Stop();
            Console.WriteLine($"Serialization completed in {stopwatch.ElapsedMilliseconds}ms");
            Console.WriteLine($"Input size: {options.InputFiles.Sum(f => new FileInfo(f).Length)} bytes");
            Console.WriteLine($"Output size: {new FileInfo(options.OutputFile).Length} bytes");
        }
    }

    private static void DeserializeData(SerializationOptions options)
    {
        var stopwatch = options.Benchmark ? Stopwatch.StartNew() : null;

        using var inputStream = new FileStream(options.InputFiles[0], FileMode.Open);
        using var sourceStream = options.Encryption != null ? 
            GetDecryptionStream(inputStream, options.Encryption) : inputStream;
        
        // Read compression method from first byte
        var compressionMethod = (CompressionMethod)sourceStream.ReadByte();
        
        using var compressedStream = GetCompressionStream(sourceStream, compressionMethod, false);
        using var reader = new BinaryReader(compressedStream);
        
        var data = FlexonBinary.Decode(reader);

        // Handle the deserialized data
        if (data is Dictionary<string, object> dict)
        {
            var outputDir = Path.GetDirectoryName(options.OutputFile);
            foreach (var kvp in dict)
            {
                var outputPath = Path.Combine(outputDir, kvp.Key);
                if (kvp.Value is byte[] bytes)
                {
                    File.WriteAllBytes(outputPath, bytes);
                }
                else
                {
                    var json = JsonSerializer.Serialize(kvp.Value, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(outputPath, json);
                }
            }
        }
        else
        {
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(options.OutputFile, json);
        }

        if (options.Benchmark && stopwatch != null)
        {
            stopwatch.Stop();
            Console.WriteLine($"Deserialization completed in {stopwatch.ElapsedMilliseconds}ms");
        }
    }

    private static void RunBenchmark(SerializationOptions options)
    {
        Console.WriteLine("Running Flexon Benchmark...");
        Console.WriteLine("1. Testing Serialization Performance...");
        
        var serializeWatch = Stopwatch.StartNew();
        SerializeData(options);
        serializeWatch.Stop();

        Console.WriteLine("2. Testing Deserialization Performance...");
        var deserializeWatch = Stopwatch.StartNew();
        DeserializeData(options);
        deserializeWatch.Stop();

        // Calculate and display metrics
        var inputSize = options.InputFiles.Sum(f => new FileInfo(f).Length);
        var outputSize = new FileInfo(options.OutputFile).Length;
        var compressionRatio = (1 - ((double)outputSize / inputSize)) * 100;

        Console.WriteLine("\nBenchmark Results:");
        Console.WriteLine($"Serialization Time: {serializeWatch.ElapsedMilliseconds}ms");
        Console.WriteLine($"Deserialization Time: {deserializeWatch.ElapsedMilliseconds}ms");
        Console.WriteLine($"Input Size: {inputSize:N0} bytes");
        Console.WriteLine($"Output Size: {outputSize:N0} bytes");
        Console.WriteLine($"Compression Ratio: {compressionRatio:F2}%");
        Console.WriteLine($"Throughput: {(inputSize / (1024.0 * 1024.0)) / (serializeWatch.ElapsedMilliseconds / 1000.0):F2} MB/s");
    }

    private static void HandleLegacyCommands(string command, string[] args)
    {
        // Display CLI header with updated version
        Console.WriteLine("=====================================");
        Console.WriteLine(" FLEXON CLI Utility v1.2.0");
        Console.WriteLine(" Developed by JVR Software");
        Console.WriteLine("=====================================\n");

        if (args.Length < 2)
        {
            DisplayUsage();
            return;
        }

        var inputPath = args[1];
        var outputPath = args.Length > 2 ? args[2] : null;

        try
        {
            if (!File.Exists(inputPath))
            {
                throw new FileNotFoundException($"Input file '{inputPath}' not found.");
            }

            switch (command)
            {
                case "encode":
                    if (outputPath == null) throw new ArgumentException("Output path is required for encoding.");
                    var encodeOptions = EncryptionOptions.Parse(args, 3);
                    EncodeJsonToFlexon(inputPath, outputPath, encodeOptions);
                    Console.WriteLine($"Successfully encoded {inputPath} to {outputPath}");
                    break;

                case "decode":
                    if (outputPath == null) throw new ArgumentException("Output path is required for decoding.");
                    var decodeOptions = EncryptionOptions.Parse(args, 3);
                    DecodeFlexonToJson(inputPath, outputPath, decodeOptions);
                    Console.WriteLine($"Successfully decoded {inputPath} to {outputPath}");
                    break;

                case "inspect":
                    InspectFlexon(inputPath, outputPath);
                    Console.WriteLine($"Successfully inspected {inputPath}" + (outputPath != null ? $" and exported to {outputPath}" : ""));
                    break;

                case "validate":
                    if (outputPath == null) throw new ArgumentException("Schema path is required for validation.");
                    ValidateFlexon(inputPath, outputPath);
                    Console.WriteLine($"Validation completed for {inputPath} against schema {outputPath}");
                    break;

                case "encrypt":
                    if (outputPath == null) throw new ArgumentException("Output path is required for encryption.");
                    if (args.Length <= 3) throw new ArgumentException("Encryption key is required.");
                    var encryptOptions = EncryptionOptions.Parse(args, 3);
                    EncryptFlexon(inputPath, outputPath, encryptOptions);
                    Console.WriteLine($"Successfully encrypted {inputPath} to {outputPath}");
                    break;

                case "decrypt":
                    if (outputPath == null) throw new ArgumentException("Output path is required for decryption.");
                    if (args.Length <= 3) throw new ArgumentException("Encryption key is required.");
                    var decryptOptions = EncryptionOptions.Parse(args, 3);
                    DecryptFlexon(inputPath, outputPath, decryptOptions);
                    Console.WriteLine($"Successfully decrypted {inputPath} to {outputPath}");
                    break;

                default:
                    throw new ArgumentException("Invalid command. Use 'encode', 'decode', 'inspect', 'validate', 'encrypt', or 'decrypt'.");
            }
        }
        catch (FileNotFoundException ex)
        {
            Console.WriteLine($"File Error: {ex.Message}");
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"Argument Error: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected Error: {ex.Message}");
        }
    }

    static void ValidateFlexon(string inputPath, string schemaPath)
    {
        using var inputStream = new FileStream(inputPath, FileMode.Open);
        using var compressedStream = new GZipStream(inputStream, CompressionMode.Decompress);
        using var reader = new BinaryReader(compressedStream);

        var data = FlexonBinary.Decode(reader);
        var schemaJson = File.ReadAllText(schemaPath);
        var schema = JsonSerializer.Deserialize<JsonElement>(schemaJson);

        var errors = new List<string>();
        if (!FlexonBinary.Validate(data, schema, errors: errors))
        {
            Console.WriteLine("Validation failed:");
            foreach (var error in errors)
            {
                Console.WriteLine($"  - {error}");
            }
            return;
        }

        Console.WriteLine("Validation passed: FLEXON data matches the schema.");
    }

    static void EncodeJsonToFlexon(string inputPath, string outputPath, EncryptionOptions options = null, CompressionMethod compression = CompressionMethod.GZip)
    {
        var json = File.ReadAllText(inputPath);
        var data = JsonSerializer.Deserialize<JsonElement>(json);

        using var outputStream = new FileStream(outputPath, FileMode.Create);
        using var targetStream = options != null ? 
            GetEncryptionStream(outputStream, options) : outputStream;
        
        // Write compression method as first byte
        targetStream.WriteByte((byte)compression);
        
        using var compressedStream = GetCompressionStream(targetStream, compression, true);
        using var writer = new BinaryWriter(compressedStream);
        FlexonBinary.Encode(data, writer);
    }

    static void DecodeFlexonToJson(string inputPath, string outputPath, EncryptionOptions options = null)
    {
        using var inputStream = new FileStream(inputPath, FileMode.Open);
        using var sourceStream = options != null ? 
            GetDecryptionStream(inputStream, options) : inputStream;
        
        // Read compression method from first byte
        var compressionMethod = (CompressionMethod)sourceStream.ReadByte();
        
        using var compressedStream = GetCompressionStream(sourceStream, compressionMethod, false);
        using var reader = new BinaryReader(compressedStream);
        var data = FlexonBinary.Decode(reader);

        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(outputPath, json);
    }

    static void InspectFlexon(string inputPath, string outputPath)
    {
        using var inputStream = new FileStream(inputPath, FileMode.Open);
        using var compressedStream = new GZipStream(inputStream, CompressionMode.Decompress);
        using var reader = new BinaryReader(compressedStream);
        var data = FlexonBinary.Decode(reader);

        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        Console.WriteLine("FLEXON Data Inspection:");
        Console.WriteLine(json);

        if (outputPath != null)
        {
            File.WriteAllText(outputPath, json);
        }
    }

    static void EncryptFlexon(string inputPath, string outputPath, EncryptionOptions options)
    {
        using var inputStream = new FileStream(inputPath, FileMode.Open);
        using var outputStream = new FileStream(outputPath, FileMode.Create);
        using var cryptoStream = GetEncryptionStream(outputStream, options);
        inputStream.CopyTo(cryptoStream);
    }

    static void DecryptFlexon(string inputPath, string outputPath, EncryptionOptions options)
    {
        using var inputStream = new FileStream(inputPath, FileMode.Open);
        using var outputStream = new FileStream(outputPath, FileMode.Create);
        using var cryptoStream = GetDecryptionStream(inputStream, options);
        cryptoStream.CopyTo(outputStream);
    }

    private static Stream GetEncryptionStream(Stream outputStream, EncryptionOptions options)
    {
        // Write algorithm identifier
        outputStream.WriteByte((byte)options.Algorithm);

        switch (options.Algorithm)
        {
            case EncryptionAlgorithm.AES256:
                return GetAesEncryptionStream(outputStream, options.Key);
            case EncryptionAlgorithm.ChaCha20:
                return GetChaCha20EncryptionStream(outputStream, options.Key);
            case EncryptionAlgorithm.TripleDES:
                return GetTripleDesEncryptionStream(outputStream, options.Key);
            default:
                throw new ArgumentException($"Unsupported encryption algorithm: {options.Algorithm}");
        }
    }

    private static Stream GetDecryptionStream(Stream inputStream, EncryptionOptions options)
    {
        // Read algorithm identifier
        var algorithm = (EncryptionAlgorithm)inputStream.ReadByte();
        
        // Override the provided algorithm with the one from the file
        options.Algorithm = algorithm;

        switch (algorithm)
        {
            case EncryptionAlgorithm.AES256:
                return GetAesDecryptionStream(inputStream, options.Key);
            case EncryptionAlgorithm.ChaCha20:
                return GetChaCha20DecryptionStream(inputStream, options.Key);
            case EncryptionAlgorithm.TripleDES:
                return GetTripleDesDecryptionStream(inputStream, options.Key);
            default:
                throw new ArgumentException($"Unsupported encryption algorithm: {algorithm}");
        }
    }

    private static Stream GetCompressionStream(Stream baseStream, CompressionMethod method, bool compress)
    {
        switch (method)
        {
            case CompressionMethod.None:
                return baseStream;
            case CompressionMethod.GZip:
                return compress 
                    ? new GZipStream(baseStream, CompressionLevel.Optimal) 
                    : new GZipStream(baseStream, CompressionMode.Decompress);
            case CompressionMethod.Deflate:
                return compress 
                    ? new DeflateStream(baseStream, CompressionLevel.Optimal) 
                    : new DeflateStream(baseStream, CompressionMode.Decompress);
            case CompressionMethod.Brotli:
                return compress 
                    ? new BrotliStream(baseStream, CompressionLevel.Optimal) 
                    : new BrotliStream(baseStream, CompressionMode.Decompress);
            default:
                throw new ArgumentException($"Unsupported compression method: {method}");
        }
    }

    private static Stream GetAesEncryptionStream(Stream outputStream, string key)
    {
        using var aes = Aes.Create();
        var keyBytes = DeriveKeyAndIV(key, out byte[] iv, 32);
        aes.Key = keyBytes;
        aes.IV = iv;

        outputStream.Write(iv, 0, iv.Length);
        var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        return new CryptoStream(outputStream, encryptor, CryptoStreamMode.Write);
    }

    private static Stream GetAesDecryptionStream(Stream inputStream, string key)
    {
        using var aes = Aes.Create();
        var iv = new byte[16];
        inputStream.Read(iv, 0, iv.Length);
        
        var keyBytes = DeriveKeyAndIV(key, out _, 32);
        aes.Key = keyBytes;
        aes.IV = iv;

        var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        return new CryptoStream(inputStream, decryptor, CryptoStreamMode.Read);
    }

    private static Stream GetChaCha20EncryptionStream(Stream outputStream, string key)
    {
        using var memoryStream = new MemoryStream();
        outputStream.CopyTo(memoryStream);
        var data = memoryStream.ToArray();
        var encryptedData = EncryptChaCha20(data, DeriveKeyAndIV(key, out _, 32));
        return new MemoryStream(encryptedData);
    }

    private static Stream GetChaCha20DecryptionStream(Stream inputStream, string key)
    {
        using var memoryStream = new MemoryStream();
        inputStream.CopyTo(memoryStream);
        var encryptedData = memoryStream.ToArray();
        var decryptedData = DecryptChaCha20(encryptedData, DeriveKeyAndIV(key, out _, 32));
        return new MemoryStream(decryptedData);
    }

    private static Stream GetTripleDesEncryptionStream(Stream outputStream, string key)
    {
        using var des = TripleDES.Create();
        var keyBytes = DeriveKeyAndIV(key, out byte[] iv, 24); // TripleDES uses 24-byte key
        des.Key = keyBytes;
        des.IV = iv;

        outputStream.Write(iv, 0, iv.Length);
        var encryptor = des.CreateEncryptor(des.Key, des.IV);
        return new CryptoStream(outputStream, encryptor, CryptoStreamMode.Write);
    }

    private static Stream GetTripleDesDecryptionStream(Stream inputStream, string key)
    {
        using var des = TripleDES.Create();
        var iv = new byte[8]; // TripleDES uses 8-byte IV
        inputStream.Read(iv, 0, iv.Length);
        
        var keyBytes = DeriveKeyAndIV(key, out _, 24);
        des.Key = keyBytes;
        des.IV = iv;

        var decryptor = des.CreateDecryptor(des.Key, des.IV);
        return new CryptoStream(inputStream, decryptor, CryptoStreamMode.Read);
    }

    private static byte[] DeriveKeyAndIV(string password, out byte[] iv, int keySize)
    {
        using var deriveBytes = new Rfc2898DeriveBytes(password, 16, 10000, HashAlgorithmName.SHA256);
        iv = deriveBytes.GetBytes(16);
        return deriveBytes.GetBytes(keySize);
    }

    private static byte[] EncryptChaCha20(byte[] data, byte[] key)
    {
        using var algorithm = new ChaCha20Poly1305(key);
        var nonce = new byte[12];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(nonce);

        byte[] ciphertext = new byte[data.Length];
        byte[] tag = new byte[16];
        algorithm.Encrypt(nonce, data, ciphertext, tag);

        // Combine nonce + ciphertext + tag
        byte[] result = new byte[nonce.Length + ciphertext.Length + tag.Length];
        Buffer.BlockCopy(nonce, 0, result, 0, nonce.Length);
        Buffer.BlockCopy(ciphertext, 0, result, nonce.Length, ciphertext.Length);
        Buffer.BlockCopy(tag, 0, result, nonce.Length + ciphertext.Length, tag.Length);

        return result;
    }

    private static byte[] DecryptChaCha20(byte[] encryptedData, byte[] key)
    {
        using var algorithm = new ChaCha20Poly1305(key);

        // Extract nonce, ciphertext, and tag
        byte[] nonce = new byte[12];
        Buffer.BlockCopy(encryptedData, 0, nonce, 0, nonce.Length);

        byte[] ciphertext = new byte[encryptedData.Length - nonce.Length - 16];
        Buffer.BlockCopy(encryptedData, nonce.Length, ciphertext, 0, ciphertext.Length);

        byte[] tag = new byte[16];
        Buffer.BlockCopy(encryptedData, nonce.Length + ciphertext.Length, tag, 0, tag.Length);

        byte[] plaintext = new byte[ciphertext.Length];
        algorithm.Decrypt(nonce, ciphertext, tag, plaintext);

        return plaintext;
    }

    private static void DisplayUsage()
    {
        Console.WriteLine("Flexon CLI - The Next Generation Data Format");
        Console.WriteLine("Version: 1.2.0");
        Console.WriteLine("\nUsage: flexon-cli <command> [options]");
        Console.WriteLine("\nCommands:");
        Console.WriteLine("  serialize     Convert files to Flexon format");
        Console.WriteLine("  deserialize   Convert Flexon files back to original format");
        Console.WriteLine("  benchmark     Run performance tests");
        Console.WriteLine("  help          Display help information");
        Console.WriteLine("\nLegacy Commands:");
        Console.WriteLine("  encode        Convert JSON to Flexon (legacy)");
        Console.WriteLine("  decode        Convert Flexon to JSON (legacy)");
        Console.WriteLine("  inspect       View Flexon file contents");
        Console.WriteLine("  validate      Validate Flexon against schema");
        Console.WriteLine("  encrypt       Encrypt existing Flexon file");
        Console.WriteLine("  decrypt       Decrypt encrypted Flexon file");
        Console.WriteLine("\nGet detailed help for a command:");
        Console.WriteLine("  flexon-cli help <command>");
        Console.WriteLine("\nExamples:");
        Console.WriteLine("  flexon-cli help serialize");
        Console.WriteLine("  flexon-cli serialize -i input.json -o output.flexon");
        Console.WriteLine("  flexon-cli benchmark -i large_file.json -o benchmark.flexon");
    }

    private static void DisplayCommandHelp(string command)
    {
        switch (command)
        {
            case "serialize":
                Console.WriteLine("Convert files to Flexon format");
                Console.WriteLine("\nUsage:");
                Console.WriteLine("  flexon-cli serialize [options]");
                Console.WriteLine("\nOptions:");
                Console.WriteLine("  -i, --input     Input file(s) to process. Can be specified multiple times");
                Console.WriteLine("  -o, --output    Output Flexon file");
                Console.WriteLine("  -s, --schema    JSON schema file for validation");
                Console.WriteLine("  -e, --encrypt   Encryption key and optional algorithm");
                Console.WriteLine("  -c, --compression  Compression method (GZip, Deflate, Brotli, None)");
                Console.WriteLine("  -b, --benchmark Run performance benchmarks");
                Console.WriteLine("\nSupported Input Formats:");
                Console.WriteLine("  - JSON files (.json)");
                Console.WriteLine("  - Images (.png, .jpg, .jpeg, .gif, .bmp)");
                Console.WriteLine("  - Any binary file");
                Console.WriteLine("\nExamples:");
                Console.WriteLine("  flexon-cli serialize -i config.json -o config.flexon");
                Console.WriteLine("  flexon-cli serialize -i data.json -i image.png -o package.flexon");
                Console.WriteLine("  flexon-cli serialize -i input.json -o secure.flexon -e myKey ChaCha20");
                break;

            case "deserialize":
                Console.WriteLine("Convert Flexon files back to original format");
                Console.WriteLine("\nUsage:");
                Console.WriteLine("  flexon-cli deserialize [options]");
                Console.WriteLine("\nOptions:");
                Console.WriteLine("  -i, --input     Input Flexon file");
                Console.WriteLine("  -o, --output    Output directory or file");
                Console.WriteLine("  -e, --encrypt   Encryption key (if file is encrypted)");
                Console.WriteLine("  -b, --benchmark Run performance benchmarks");
                Console.WriteLine("\nExamples:");
                Console.WriteLine("  flexon-cli deserialize -i package.flexon -o ./output_dir");
                Console.WriteLine("  flexon-cli deserialize -i encrypted.flexon -o data.json -e myKey");
                break;

            case "benchmark":
                Console.WriteLine("Run performance tests");
                Console.WriteLine("\nUsage:");
                Console.WriteLine("  flexon-cli benchmark [options]");
                Console.WriteLine("\nOptions:");
                Console.WriteLine("  -i, --input     Input file to benchmark");
                Console.WriteLine("  -o, --output    Output file for benchmark results");
                Console.WriteLine("  -e, --encrypt   Include encryption in benchmark");
                Console.WriteLine("  -c, --compression  Compression method (GZip, Deflate, Brotli, None)");
                Console.WriteLine("  -b, --benchmark Show detailed metrics");
                Console.WriteLine("\nMetrics Reported:");
                Console.WriteLine("  - Serialization time");
                Console.WriteLine("  - Deserialization time");
                Console.WriteLine("  - Compression ratio");
                Console.WriteLine("  - Memory usage");
                Console.WriteLine("  - Throughput (MB/s)");
                Console.WriteLine("\nExamples:");
                Console.WriteLine("  flexon-cli benchmark -i large_dataset.json -o benchmark.flexon -b");
                Console.WriteLine("  flexon-cli benchmark -i data.json -o secure.flexon -e myKey -b");
                break;

            case "encrypt":
                Console.WriteLine("Encrypt an existing Flexon file");
                Console.WriteLine("\nUsage:");
                Console.WriteLine("  flexon-cli encrypt <input> <output> <key> [algorithm]");
                Console.WriteLine("\nSupported Algorithms:");
                Console.WriteLine("  - AES256 (default)");
                Console.WriteLine("  - ChaCha20");
                Console.WriteLine("  - TripleDES");
                Console.WriteLine("\nExamples:");
                Console.WriteLine("  flexon-cli encrypt data.flexon secure.flexon myKey");
                Console.WriteLine("  flexon-cli encrypt data.flexon secure.flexon myKey ChaCha20");
                break;

            case "decrypt":
                Console.WriteLine("Decrypt an encrypted Flexon file");
                Console.WriteLine("\nUsage:");
                Console.WriteLine("  flexon-cli decrypt <input> <output> <key>");
                Console.WriteLine("\nNote: The encryption algorithm is automatically detected");
                Console.WriteLine("\nExamples:");
                Console.WriteLine("  flexon-cli decrypt secure.flexon data.flexon myKey");
                break;

            case "validate":
                Console.WriteLine("Validate Flexon file against JSON schema");
                Console.WriteLine("\nUsage:");
                Console.WriteLine("  flexon-cli validate <input> <schema>");
                Console.WriteLine("\nExamples:");
                Console.WriteLine("  flexon-cli validate data.flexon schema.json");
                break;

            case "inspect":
                Console.WriteLine("View contents of a Flexon file");
                Console.WriteLine("\nUsage:");
                Console.WriteLine("  flexon-cli inspect <input> [output]");
                Console.WriteLine("\nExamples:");
                Console.WriteLine("  flexon-cli inspect data.flexon");
                Console.WriteLine("  flexon-cli inspect data.flexon output.json");
                break;

            case "encode":
            case "decode":
                Console.WriteLine("Legacy commands - Consider using serialize/deserialize instead");
                Console.WriteLine("\nUsage:");
                Console.WriteLine("  flexon-cli encode <input.json> <output.flexon> [key] [algorithm]");
                Console.WriteLine("  flexon-cli decode <input.flexon> <output.json> [key]");
                Console.WriteLine("\nExamples:");
                Console.WriteLine("  flexon-cli encode input.json output.flexon");
                Console.WriteLine("  flexon-cli encode input.json secure.flexon myKey ChaCha20");
                Console.WriteLine("  flexon-cli decode input.flexon output.json");
                break;

            default:
                Console.WriteLine($"Unknown command: {command}");
                Console.WriteLine("Use 'flexon-cli help' to see all available commands");
                break;
        }
    }
}

public static class FlexonBinary
{
    public static void Encode(object data, BinaryWriter writer)
    {
        if (data == null)
        {
            writer.Write((byte)0x00); // Null
        }
        else if (data is bool boolean)
        {
            writer.Write((byte)(boolean ? 0x01 : 0x02)); // Boolean
        }
        else if (data is int integer)
        {
            writer.Write((byte)0x03); // Integer
            writer.Write(integer);
        }
        else if (data is double dbl)
        {
            writer.Write((byte)0x04); // Float
            writer.Write(dbl);
        }
        else if (data is string str)
        {
            writer.Write((byte)0x05); // String
            var bytes = Encoding.UTF8.GetBytes(str);
            writer.Write(bytes.Length);
            writer.Write(bytes);
        }
        else if (data is byte[] binary)
        {
            writer.Write((byte)0x06); // Binary
            writer.Write(binary.Length);
            writer.Write(binary);
        }
        else if (data is DateTime dt)
        {
            writer.Write((byte)0x07); // Date
            writer.Write(dt.ToBinary());
        }
        else if (data is Guid guid)
        {
            writer.Write((byte)0x08); // UUID
            writer.Write(guid.ToByteArray());
        }
        else if (data is System.Collections.IList list)
        {
            writer.Write((byte)0x09); // List
            foreach (var item in list)
            {
                Encode(item, writer);
            }
            writer.Write((byte)0x00); // End of list marker
        }
        else if (data is System.Collections.IDictionary dict)
        {
            writer.Write((byte)0x0A); // Object
            foreach (var key in dict.Keys)
            {
                Encode(key, writer); // Key
                Encode(dict[key], writer); // Value
            }
            writer.Write((byte)0x00); // End of object marker
        }
        else if (data is JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Null:
                    writer.Write((byte)0x00);
                    break;
                case JsonValueKind.True:
                    writer.Write((byte)0x01);
                    break;
                case JsonValueKind.False:
                    writer.Write((byte)0x02);
                    break;
                case JsonValueKind.Number:
                    if (element.TryGetInt32(out int intValue))
                    {
                        writer.Write((byte)0x03);
                        writer.Write(intValue);
                    }
                    else
                    {
                        writer.Write((byte)0x04);
                        writer.Write(element.GetDouble());
                    }
                    break;
                case JsonValueKind.String:
                    writer.Write((byte)0x05);
                    var stringValue = element.GetString();
                    var stringBytes = Encoding.UTF8.GetBytes(stringValue ?? "");
                    writer.Write(stringBytes.Length);
                    writer.Write(stringBytes);
                    break;
                case JsonValueKind.Array:
                    writer.Write((byte)0x09);
                    foreach (var item in element.EnumerateArray())
                    {
                        Encode(item, writer);
                    }
                    writer.Write((byte)0x00);
                    break;
                case JsonValueKind.Object:
                    writer.Write((byte)0x0A);
                    foreach (var prop in element.EnumerateObject())
                    {
                        Encode(prop.Name, writer);
                        Encode(prop.Value, writer);
                    }
                    writer.Write((byte)0x00);
                    break;
                default:
                    throw new NotSupportedException($"Unsupported JsonValueKind: {element.ValueKind}");
            }
        }
        else
        {
            throw new NotSupportedException($"Unsupported type: {data.GetType()}");
        }
    }

    public static object Decode(BinaryReader reader)
    {
        byte typeIndicator = reader.ReadByte();

        return typeIndicator switch
        {
            0x00 => null,
            0x01 => true,
            0x02 => false,
            0x03 => reader.ReadInt32(),
            0x04 => reader.ReadDouble(),
            0x05 => Encoding.UTF8.GetString(reader.ReadBytes(reader.ReadInt32())),
            0x06 => reader.ReadBytes(reader.ReadInt32()),
            0x07 => DateTime.FromBinary(reader.ReadInt64()),
            0x08 => new Guid(reader.ReadBytes(16)),
            0x09 => DecodeList(reader),
            0x0A => DecodeDictionary(reader),
            _ => throw new InvalidOperationException($"Unknown type indicator: {typeIndicator}")
        };
    }

    private static List<object> DecodeList(BinaryReader reader)
    {
        var list = new List<object>();
        while (reader.PeekChar() != 0x00)
        {
            list.Add(Decode(reader));
        }
        reader.ReadByte();
        return list;
    }

    private static Dictionary<string, object> DecodeDictionary(BinaryReader reader)
    {
        var dict = new Dictionary<string, object>();
        while (reader.PeekChar() != 0x00)
        {
            var key = (string)Decode(reader);
            var value = Decode(reader);
            dict[key] = value;
        }
        reader.ReadByte();
        return dict;
    }

    public static bool Validate(object data, JsonElement schema, string propertyPath = "", List<string> errors = null)
    {
        if (errors == null) errors = new List<string>();
        string type = schema.GetProperty("type").GetString();
        propertyPath = string.IsNullOrEmpty(propertyPath) ? "(root)" : propertyPath;

        if (type == "string")
        {
            if (data is not string str)
            {
                errors.Add($"Property '{propertyPath}': Expected string, got {data?.GetType().Name ?? "null"}.");
                return false;
            }

            if (schema.TryGetProperty("minLength", out var minLength) && str.Length < minLength.GetInt32())
            {
                errors.Add($"Property '{propertyPath}': Length {str.Length} is less than minimum {minLength.GetInt32()}.");
                return false;
            }

            if (schema.TryGetProperty("maxLength", out var maxLength) && str.Length > maxLength.GetInt32())
            {
                errors.Add($"Property '{propertyPath}': Length {str.Length} exceeds maximum {maxLength.GetInt32()}.");
                return false;
            }
        }
        else if (type == "integer")
        {
            if (data is not int integer)
            {
                errors.Add($"Property '{propertyPath}': Expected integer, got {data?.GetType().Name ?? "null"}.");
                return false;
            }

            if (schema.TryGetProperty("minimum", out var minimum) && integer < minimum.GetInt32())
            {
                errors.Add($"Property '{propertyPath}': Value {integer} is less than minimum {minimum.GetInt32()}.");
                return false;
            }

            if (schema.TryGetProperty("maximum", out var maximum) && integer > maximum.GetInt32())
            {
                errors.Add($"Property '{propertyPath}': Value {integer} exceeds maximum {maximum.GetInt32()}.");
                return false;
            }
        }
        else if (type == "object")
        {
            if (data is not Dictionary<string, object> dict)
            {
                errors.Add($"Property '{propertyPath}': Expected object, got {data?.GetType().Name ?? "null"}.");
                return false;
            }

            var properties = schema.GetProperty("properties");
            foreach (var property in properties.EnumerateObject())
            {
                string key = property.Name;
                var subSchema = property.Value;

                if (!dict.ContainsKey(key))
                {
                    if (schema.TryGetProperty("required", out var requiredFields) &&
                        requiredFields.EnumerateArray().Any(r => r.GetString() == key))
                    {
                        errors.Add($"Property '{propertyPath}.{key}': Missing required property.");
                        return false;
                    }
                }
                else
                {
                    Validate(dict[key], subSchema, $"{propertyPath}.{key}", errors);
                }
            }
        }
        else if (type == "array")
        {
            if (data is not List<object> list)
            {
                errors.Add($"Property '{propertyPath}': Expected array, got {data?.GetType().Name ?? "null"}.");
                return false;
            }

            var itemsSchema = schema.GetProperty("items");
            for (int i = 0; i < list.Count; i++)
            {
                Validate(list[i], itemsSchema, $"{propertyPath}[{i}]", errors);
            }
        }
        else
        {
            errors.Add($"Property '{propertyPath}': Unsupported type '{type}' in schema.");
            return false;
        }

        return !errors.Any();
    }
}
