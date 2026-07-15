using System.Diagnostics;
using System.Text.Json;
using Flexon;

namespace FlexonCLI;

public static class CliApplication
{
    public static string Version => typeof(CliApplication).Assembly.GetName().Version?.ToString(3) ?? "unknown";
    private static readonly JsonSerializerOptions PrettyJson = new() { WriteIndented = true };

    public static int Run(string[] args, TextWriter output, TextWriter error)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);

        try
        {
            if (args.Length == 0 || IsHelp(args[0]))
            {
                WriteUsage(output);
                return 0;
            }
            if (args[0] is "--version" or "-V" or "version")
            {
                output.WriteLine(Version);
                return 0;
            }

            return args[0].ToLowerInvariant() switch
            {
                "serialize" => Serialize(args[1..], output, error),
                "deserialize" => Deserialize(args[1..], output, error),
                "encode" => Encode(args[1..], output, error),
                "decode" => Decode(args[1..], output, error),
                "inspect" => Inspect(args[1..], output, error),
                "validate" => Validate(args[1..], output, error),
                "encrypt" => Encrypt(args[1..], output, error),
                "decrypt" => Decrypt(args[1..], output, error),
                "keygen" => Keygen(args[1..], output),
                "sign" => Sign(args[1..], output),
                "verify-signature" => VerifySignature(args[1..], output, error),
                "benchmark" => Benchmark(args[1..], output, error),
                "help" => ShowCommandHelp(args[1..], output),
                _ => throw new CliUsageException($"Unknown command '{args[0]}'.")
            };
        }
        catch (CliUsageException ex)
        {
            error.WriteLine($"Usage error: {ex.Message}");
            error.WriteLine("Run 'flexon-cli help' for usage.");
            return 2;
        }
        catch (FlexonAuthenticationException ex)
        {
            error.WriteLine($"Authentication error: {ex.Message}");
            return 4;
        }
        catch (Exception ex) when (ex is FlexonException or InvalidDataException or JsonException)
        {
            error.WriteLine($"Data error: {ex.Message}");
            return 3;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            error.WriteLine($"File error: {ex.Message}");
            return 5;
        }
        catch (Exception ex)
        {
            error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    private static int Serialize(string[] args, TextWriter output, TextWriter error)
    {
        var parsed = CommandOptions.Parse(args, allowMultipleInputs: true);
        parsed.RequireInputs();
        parsed.RequireOutput();
        var package = new Dictionary<string, object?>(StringComparer.Ordinal);
        JsonElement? singleJson = null;

        foreach (var input in parsed.Inputs)
        {
            if (!File.Exists(input)) throw new CliUsageException($"Input file '{input}' does not exist.");
            var name = Path.GetFileName(input);
            if (!package.TryAdd(name, ReadInput(input, out var json)))
                throw new CliUsageException($"Multiple inputs use the same filename '{name}'.");
            if (parsed.Inputs.Count == 1) singleJson = json;
        }

        if (parsed.Schema is not null)
        {
            if (singleJson is null) throw new CliUsageException("Schema validation requires exactly one JSON input file.");
            ValidateAgainstSchema(singleJson.Value, parsed.Schema);
        }

        WarnIfPasswordOnCommandLine(parsed, error);
        var options = parsed.ToFlexonOptions(forWrite: true);
        AtomicFile.Write(parsed.Output!, stream => FlexonSerializer.Serialize(package, stream, options));
        output.WriteLine($"Serialized {package.Count} file(s) to '{parsed.Output}'.");
        return 0;
    }

    private static int Deserialize(string[] args, TextWriter output, TextWriter error)
    {
        var parsed = CommandOptions.Parse(args);
        parsed.RequireSingleInput();
        parsed.RequireOutput();
        WarnIfPasswordOnCommandLine(parsed, error);
        using var stream = File.OpenRead(parsed.Inputs[0]);
        var value = FlexonSerializer.Deserialize(stream, parsed.ToFlexonOptions(forWrite: false));

        if (value is Dictionary<string, object?> package)
        {
            var root = Path.GetFullPath(parsed.Output!);
            var destinations = package.Keys.ToDictionary(name => name, name => GetSafeChildPath(root, name), StringComparer.Ordinal);
            Directory.CreateDirectory(root);
            foreach (var (name, fileValue) in package)
            {
                var destination = destinations[name];
                if (fileValue is byte[] bytes) AtomicFile.WriteAllBytes(destination, bytes);
                else AtomicFile.WriteAllText(destination, JsonSerializer.Serialize(fileValue, PrettyJson));
            }
            output.WriteLine($"Deserialized {package.Count} file(s) to '{parsed.Output}'.");
        }
        else
        {
            AtomicFile.WriteAllText(parsed.Output!, JsonSerializer.Serialize(value, PrettyJson));
            output.WriteLine($"Deserialized value to '{parsed.Output}'.");
        }
        return 0;
    }

    private static int Encode(string[] args, TextWriter output, TextWriter error)
    {
        if (args.Length < 2) throw new CliUsageException("encode requires <input.json> <output.flexon> [password] [algorithm].");
        var input = args[0];
        var destination = args[1];
        var password = args.Length > 2 ? args[2] : null;
        var encryption = password is null ? EncryptionAlgorithm.None : ParseEncryption(args.Length > 3 ? args[3] : "AES256");
        if (args.Length > 4) throw new CliUsageException("Too many encode arguments.");
        if (password is not null) error.WriteLine("Warning: command-line passwords may be visible to other processes; prefer serialize --password-env.");
        using var document = JsonDocument.Parse(File.ReadAllText(input));
        var options = new FlexonOptions { Encryption = encryption, Password = password };
        AtomicFile.Write(destination, stream => FlexonSerializer.Serialize(document.RootElement, stream, options));
        output.WriteLine($"Encoded '{input}' to '{destination}'.");
        return 0;
    }

    private static int Decode(string[] args, TextWriter output, TextWriter error)
    {
        if (args.Length is < 2 or > 3) throw new CliUsageException("decode requires <input.flexon> <output.json> [password].");
        var password = args.Length == 3 ? args[2] : null;
        if (password is not null) error.WriteLine("Warning: command-line passwords may be visible to other processes; prefer deserialize --password-env.");
        using var stream = File.OpenRead(args[0]);
        var value = FlexonSerializer.Deserialize(stream, new FlexonOptions { Password = password });
        AtomicFile.WriteAllText(args[1], JsonSerializer.Serialize(value, PrettyJson));
        output.WriteLine($"Decoded '{args[0]}' to '{args[1]}'.");
        return 0;
    }

    private static int Inspect(string[] args, TextWriter output, TextWriter error)
    {
        if (args.Length is < 1 or > 2) throw new CliUsageException("inspect requires <input.flexon> [password].");
        if (args.Length == 2) error.WriteLine("Warning: command-line passwords may be visible to other processes.");
        using var stream = File.OpenRead(args[0]);
        var value = FlexonSerializer.Deserialize(stream, new FlexonOptions { Password = args.Length == 2 ? args[1] : null });
        output.WriteLine(JsonSerializer.Serialize(value, PrettyJson));
        return 0;
    }

    private static int Validate(string[] args, TextWriter output, TextWriter error)
    {
        if (args.Length is < 2 or > 3) throw new CliUsageException("validate requires <input.flexon> <schema.json> [password].");
        using var input = File.OpenRead(args[0]);
        var value = FlexonSerializer.Deserialize(input, new FlexonOptions { Password = args.Length == 3 ? args[2] : null });
        var json = JsonSerializer.SerializeToElement(value);
        try
        {
            ValidateAgainstSchema(json, args[1]);
            output.WriteLine("Validation passed.");
            return 0;
        }
        catch (FlexonFormatException ex)
        {
            error.WriteLine(ex.Message);
            return 3;
        }
    }

    private static int Keygen(string[] args, TextWriter output)
    {
        if (args.Length != 2) throw new CliUsageException("keygen requires <private-key.pem> <public-key.pem>.");
        var pair = FlexonSignature.GenerateKeyPair();
        AtomicFile.WriteAllText(args[0], pair.PrivateKeyPem);
        AtomicFile.WriteAllText(args[1], pair.PublicKeyPem);
        output.WriteLine($"Generated an ECDSA P-256 key pair at '{args[0]}' and '{args[1]}'. Protect the private key.");
        return 0;
    }

    private static int Sign(string[] args, TextWriter output)
    {
        if (args.Length != 3) throw new CliUsageException("sign requires <input> <private-key.pem> <signature>.");
        var signature = FlexonSignature.Sign(File.ReadAllBytes(args[0]), File.ReadAllText(args[1]));
        AtomicFile.WriteAllBytes(args[2], signature);
        output.WriteLine($"Signed '{args[0]}' to '{args[2]}' using ECDSA P-256/SHA-256.");
        return 0;
    }

    private static int VerifySignature(string[] args, TextWriter output, TextWriter error)
    {
        if (args.Length != 3) throw new CliUsageException("verify-signature requires <input> <public-key.pem> <signature>.");
        var valid = FlexonSignature.Verify(File.ReadAllBytes(args[0]), File.ReadAllBytes(args[2]), File.ReadAllText(args[1]));
        if (!valid)
        {
            error.WriteLine("Signature verification failed.");
            return 6;
        }
        output.WriteLine("Signature is valid.");
        return 0;
    }

    private static int Encrypt(string[] args, TextWriter output, TextWriter error)
    {
        if (args.Length is < 3 or > 4) throw new CliUsageException("encrypt requires <input.flexon> <output.flexon> <password> [algorithm].");
        error.WriteLine("Warning: command-line passwords may be visible to other processes; prefer deserialize/serialize with --password-env.");
        using var input = File.OpenRead(args[0]);
        var value = FlexonSerializer.Deserialize(input);
        var options = new FlexonOptions { Encryption = ParseEncryption(args.Length == 4 ? args[3] : "AES256"), Password = args[2] };
        AtomicFile.Write(args[1], stream => FlexonSerializer.Serialize(value, stream, options));
        output.WriteLine($"Encrypted '{args[0]}' to '{args[1]}'.");
        return 0;
    }

    private static int Decrypt(string[] args, TextWriter output, TextWriter error)
    {
        if (args.Length != 3) throw new CliUsageException("decrypt requires <input.flexon> <output.flexon> <password>.");
        error.WriteLine("Warning: command-line passwords may be visible to other processes; prefer deserialize/serialize with --password-env.");
        using var input = File.OpenRead(args[0]);
        var value = FlexonSerializer.Deserialize(input, new FlexonOptions { Password = args[2] });
        AtomicFile.Write(args[1], stream => FlexonSerializer.Serialize(value, stream));
        output.WriteLine($"Decrypted '{args[0]}' to '{args[1]}'.");
        return 0;
    }

    private static int Benchmark(string[] args, TextWriter output, TextWriter error)
    {
        var parsed = CommandOptions.Parse(args);
        parsed.RequireSingleInput();
        var value = ReadInput(parsed.Inputs[0], out _);
        WarnIfPasswordOnCommandLine(parsed, error);
        var options = parsed.ToFlexonOptions(forWrite: true);

        var serializeWatch = Stopwatch.StartNew();
        var bytes = FlexonSerializer.Serialize(value, options);
        serializeWatch.Stop();
        var deserializeWatch = Stopwatch.StartNew();
        _ = FlexonSerializer.Deserialize(bytes, new FlexonOptions { Password = options.Password });
        deserializeWatch.Stop();
        if (parsed.Output is not null) AtomicFile.WriteAllBytes(parsed.Output, bytes);

        var inputLength = new FileInfo(parsed.Inputs[0]).Length;
        output.WriteLine($"Input bytes: {inputLength:N0}");
        output.WriteLine($"FLEXON bytes: {bytes.Length:N0}");
        output.WriteLine($"Serialize: {serializeWatch.Elapsed.TotalMilliseconds:F3} ms");
        output.WriteLine($"Deserialize: {deserializeWatch.Elapsed.TotalMilliseconds:F3} ms");
        if (inputLength > 0) output.WriteLine($"Size change: {(bytes.Length - inputLength) * 100.0 / inputLength:+0.00;-0.00;0.00}%");
        return 0;
    }

    private static object? ReadInput(string path, out JsonElement? json)
    {
        if (!File.Exists(path)) throw new CliUsageException($"Input file '{path}' does not exist.");
        if (Path.GetExtension(path).Equals(".json", StringComparison.OrdinalIgnoreCase))
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            json = document.RootElement.Clone();
            return json.Value;
        }
        json = null;
        return File.ReadAllBytes(path);
    }

    private static void ValidateAgainstSchema(JsonElement value, string schemaPath)
    {
        using var schema = JsonDocument.Parse(File.ReadAllText(schemaPath));
        var errors = JsonSchemaValidator.Validate(value, schema.RootElement);
        if (errors.Count > 0) throw new FlexonFormatException("Schema validation failed:" + Environment.NewLine + string.Join(Environment.NewLine, errors.Select(item => "  - " + item)));
    }

    private static string GetSafeChildPath(string root, string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name is "." or ".." || Path.IsPathRooted(name) ||
            name.IndexOfAny(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }) >= 0 || Path.GetFileName(name) != name)
            throw new FlexonFormatException($"Unsafe package filename '{name}'.");
        var candidate = Path.GetFullPath(Path.Combine(root, name));
        var rootWithSeparator = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!candidate.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase))
            throw new FlexonFormatException($"Package filename '{name}' escapes the output directory.");
        return candidate;
    }

    private static EncryptionAlgorithm ParseEncryption(string value) => value.ToLowerInvariant() switch
    {
        "aes" or "aes256" or "aes256gcm" or "aes-256-gcm" => EncryptionAlgorithm.Aes256Gcm,
        "chacha20" or "chacha20poly1305" or "chacha20-poly1305" => EncryptionAlgorithm.ChaCha20Poly1305,
        "none" => EncryptionAlgorithm.None,
        "tripledes" or "3des" => throw new CliUsageException("TripleDES is not supported for new files because it is obsolete. Use AES256 or ChaCha20."),
        _ => throw new CliUsageException($"Unknown encryption algorithm '{value}'.")
    };

    private static bool IsHelp(string value) => value is "help" or "--help" or "-h";

    private static int ShowCommandHelp(string[] args, TextWriter output)
    {
        WriteUsage(output);
        return 0;
    }

    private static void WarnIfPasswordOnCommandLine(CommandOptions parsed, TextWriter error)
    {
        if (parsed.PasswordWasOnCommandLine)
            error.WriteLine("Warning: command-line passwords may be visible to other processes; prefer --password-env or --password-file.");
    }

    private static void WriteUsage(TextWriter output)
    {
        output.WriteLine($"FLEXON CLI {Version}");
        output.WriteLine("Usage: flexon-cli <command> [options]");
        output.WriteLine();
        output.WriteLine("Commands:");
        output.WriteLine("  serialize   Package one or more files into FLEXON v2");
        output.WriteLine("  deserialize Safely extract a FLEXON package");
        output.WriteLine("  encode      Convert one JSON value to FLEXON v2");
        output.WriteLine("  decode      Convert one FLEXON value to JSON");
        output.WriteLine("  inspect     Print FLEXON contents as JSON");
        output.WriteLine("  validate    Validate FLEXON contents against a JSON schema subset");
        output.WriteLine("  encrypt     Rewrap a FLEXON file with authenticated encryption");
        output.WriteLine("  decrypt     Remove authenticated encryption from a FLEXON file");
        output.WriteLine("  keygen      Generate an ECDSA P-256 signing key pair");
        output.WriteLine("  sign        Create a detached signature for a file");
        output.WriteLine("  verify-signature Verify a detached file signature");
        output.WriteLine("  benchmark   Measure a local round trip");
        output.WriteLine();
        output.WriteLine("Common options:");
        output.WriteLine("  -i, --input <path>           Input path (repeatable for serialize)");
        output.WriteLine("  -o, --output <path>          Output file or directory");
        output.WriteLine("  -c, --compression <method>   none, gzip, deflate, brotli");
        output.WriteLine("  --encryption <algorithm>     none, AES256, ChaCha20");
        output.WriteLine("  --password-env <name>        Read password from an environment variable");
        output.WriteLine("  --password-file <path>       Read password from a protected local file");
        output.WriteLine("  -e, --encrypt <password> [algorithm] (compatibility; less secure)");
    }

    private sealed class CommandOptions
    {
        public List<string> Inputs { get; } = new();
        public string? Output { get; private set; }
        public string? Schema { get; private set; }
        public CompressionMethod Compression { get; private set; } = CompressionMethod.GZip;
        public EncryptionAlgorithm Encryption { get; private set; } = EncryptionAlgorithm.None;
        public string? Password { get; private set; }
        public bool PasswordWasOnCommandLine { get; private set; }

        public static CommandOptions Parse(string[] args, bool allowMultipleInputs = false)
        {
            var result = new CommandOptions();
            for (var index = 0; index < args.Length; index++)
            {
                var argument = args[index];
                switch (argument)
                {
                    case "-i": case "--input":
                        result.Inputs.Add(RequireValue(args, ref index, argument));
                        break;
                    case "-o": case "--output":
                        result.Output = RequireValue(args, ref index, argument);
                        break;
                    case "-s": case "--schema":
                        result.Schema = RequireValue(args, ref index, argument);
                        break;
                    case "-c": case "--compression":
                        result.Compression = ParseCompression(RequireValue(args, ref index, argument));
                        break;
                    case "--encryption":
                        result.Encryption = ParseEncryption(RequireValue(args, ref index, argument));
                        break;
                    case "--password":
                        result.Password = RequireValue(args, ref index, argument);
                        result.PasswordWasOnCommandLine = true;
                        break;
                    case "--password-env":
                        var variable = RequireValue(args, ref index, argument);
                        result.Password = Environment.GetEnvironmentVariable(variable)
                            ?? throw new CliUsageException($"Environment variable '{variable}' is not set.");
                        break;
                    case "--password-file":
                        result.Password = File.ReadAllText(RequireValue(args, ref index, argument)).TrimEnd('\r', '\n');
                        break;
                    case "-e": case "--encrypt":
                        result.Password = RequireValue(args, ref index, argument);
                        result.PasswordWasOnCommandLine = true;
                        result.Encryption = EncryptionAlgorithm.Aes256Gcm;
                        if (index + 1 < args.Length && !args[index + 1].StartsWith('-')) result.Encryption = ParseEncryption(args[++index]);
                        break;
                    default:
                        throw new CliUsageException($"Unknown option '{argument}'.");
                }
            }
            if (!allowMultipleInputs && result.Inputs.Count > 1) throw new CliUsageException("This command accepts only one input file.");
            if (result.Encryption != EncryptionAlgorithm.None && string.IsNullOrEmpty(result.Password))
                throw new CliUsageException("Encryption requires --password-env, --password-file, --password, or -e.");
            return result;
        }

        public FlexonOptions ToFlexonOptions(bool forWrite) => new()
        {
            Compression = forWrite ? Compression : CompressionMethod.GZip,
            Encryption = forWrite ? Encryption : EncryptionAlgorithm.None,
            Password = Password
        };

        public void RequireInputs()
        {
            if (Inputs.Count == 0) throw new CliUsageException("At least one input file is required.");
        }

        public void RequireSingleInput()
        {
            if (Inputs.Count != 1) throw new CliUsageException("Exactly one input file is required.");
        }

        public void RequireOutput()
        {
            if (string.IsNullOrWhiteSpace(Output)) throw new CliUsageException("An output path is required.");
        }

        private static string RequireValue(string[] args, ref int index, string option)
        {
            if (++index >= args.Length) throw new CliUsageException($"Option '{option}' requires a value.");
            return args[index];
        }

        private static CompressionMethod ParseCompression(string value) => value.ToLowerInvariant() switch
        {
            "none" => CompressionMethod.None,
            "gzip" => CompressionMethod.GZip,
            "deflate" => CompressionMethod.Deflate,
            "brotli" => CompressionMethod.Brotli,
            _ => throw new CliUsageException($"Unknown compression method '{value}'.")
        };
    }

    private sealed class CliUsageException : Exception
    {
        public CliUsageException(string message) : base(message) { }
    }
}
