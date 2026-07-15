using System.Text.Json;
using Flexon;

using var document = JsonDocument.Parse("""{"name":"FLEXON","nullable":null,"items":[1,null,2]}""");
var options = new FlexonOptions
{
    Compression = CompressionMethod.Brotli,
    Encryption = EncryptionAlgorithm.Aes256Gcm,
    Password = "example-only-password"
};

var encoded = FlexonSerializer.Serialize(document.RootElement, options);
var decoded = FlexonSerializer.Deserialize(encoded, new FlexonOptions { Password = options.Password });
Console.WriteLine(JsonSerializer.Serialize(decoded, new JsonSerializerOptions { WriteIndented = true }));
