using System.Text.RegularExpressions;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Flexon;

public static class JsonSchemaValidator
{
    public static IReadOnlyList<string> Validate(JsonElement value, JsonElement schema)
    {
        var errors = new List<string>();
        ValidateValue(value, schema, "$", errors);
        return errors;
    }

    private static void ValidateValue(JsonElement value, JsonElement schema, string path, List<string> errors)
    {
        if (schema.TryGetProperty("type", out var typeElement) && !MatchesType(value, typeElement.GetString()))
        {
            errors.Add($"{path}: expected {typeElement.GetString()}, found {value.ValueKind.ToString().ToLowerInvariant()}.");
            return;
        }
        if (schema.TryGetProperty("enum", out var enumElement) && enumElement.ValueKind == JsonValueKind.Array &&
            !enumElement.EnumerateArray().Any(candidate => JsonNode.DeepEquals(JsonNode.Parse(candidate.GetRawText()), JsonNode.Parse(value.GetRawText()))))
            errors.Add($"{path}: value is not in the allowed enum.");

        switch (value.ValueKind)
        {
            case JsonValueKind.Object: ValidateObject(value, schema, path, errors); break;
            case JsonValueKind.Array: ValidateArray(value, schema, path, errors); break;
            case JsonValueKind.String: ValidateString(value.GetString() ?? string.Empty, schema, path, errors); break;
            case JsonValueKind.Number: ValidateNumber(value, schema, path, errors); break;
        }
    }

    private static void ValidateObject(JsonElement value, JsonElement schema, string path, List<string> errors)
    {
        if (schema.TryGetProperty("required", out var required) && required.ValueKind == JsonValueKind.Array)
            foreach (var property in required.EnumerateArray())
            {
                var name = property.GetString();
                if (name is not null && !value.TryGetProperty(name, out _)) errors.Add($"{path}.{name}: required property is missing.");
            }

        if (schema.TryGetProperty("properties", out var properties) && properties.ValueKind == JsonValueKind.Object)
            foreach (var propertySchema in properties.EnumerateObject())
                if (value.TryGetProperty(propertySchema.Name, out var propertyValue))
                    ValidateValue(propertyValue, propertySchema.Value, $"{path}.{propertySchema.Name}", errors);

        if (schema.TryGetProperty("additionalProperties", out var additional) && additional.ValueKind == JsonValueKind.False &&
            schema.TryGetProperty("properties", out properties) && properties.ValueKind == JsonValueKind.Object)
        {
            var allowed = properties.EnumerateObject().Select(property => property.Name).ToHashSet(StringComparer.Ordinal);
            foreach (var property in value.EnumerateObject())
                if (!allowed.Contains(property.Name)) errors.Add($"{path}.{property.Name}: additional properties are not allowed.");
        }
    }

    private static void ValidateArray(JsonElement value, JsonElement schema, string path, List<string> errors)
    {
        var items = value.EnumerateArray().ToArray();
        if (schema.TryGetProperty("minItems", out var minItems) && items.Length < minItems.GetInt32()) errors.Add($"{path}: expected at least {minItems.GetInt32()} items.");
        if (schema.TryGetProperty("maxItems", out var maxItems) && items.Length > maxItems.GetInt32()) errors.Add($"{path}: expected at most {maxItems.GetInt32()} items.");
        if (schema.TryGetProperty("items", out var itemSchema))
            for (var i = 0; i < items.Length; i++) ValidateValue(items[i], itemSchema, $"{path}[{i}]", errors);
    }

    private static void ValidateString(string value, JsonElement schema, string path, List<string> errors)
    {
        if (schema.TryGetProperty("minLength", out var minLength) && value.Length < minLength.GetInt32()) errors.Add($"{path}: string is shorter than {minLength.GetInt32()} characters.");
        if (schema.TryGetProperty("maxLength", out var maxLength) && value.Length > maxLength.GetInt32()) errors.Add($"{path}: string is longer than {maxLength.GetInt32()} characters.");
        if (!schema.TryGetProperty("pattern", out var pattern)) return;
        try
        {
            if (!Regex.IsMatch(value, pattern.GetString() ?? string.Empty, RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1)))
                errors.Add($"{path}: string does not match the required pattern.");
        }
        catch (ArgumentException) { errors.Add($"{path}: schema contains an invalid regular expression."); }
        catch (RegexMatchTimeoutException) { errors.Add($"{path}: schema regular expression timed out."); }
    }

    private static void ValidateNumber(JsonElement value, JsonElement schema, string path, List<string> errors)
    {
        var number = value.GetDouble();
        if (schema.TryGetProperty("minimum", out var minimum) && number < minimum.GetDouble()) errors.Add($"{path}: value is below the minimum {minimum.GetDouble()}.");
        if (schema.TryGetProperty("maximum", out var maximum) && number > maximum.GetDouble()) errors.Add($"{path}: value exceeds the maximum {maximum.GetDouble()}.");
    }

    private static bool MatchesType(JsonElement value, string? type) => type switch
    {
        "null" => value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined,
        "boolean" => value.ValueKind is JsonValueKind.True or JsonValueKind.False,
        "object" => value.ValueKind == JsonValueKind.Object,
        "array" => value.ValueKind == JsonValueKind.Array,
        "string" => value.ValueKind == JsonValueKind.String,
        "number" => value.ValueKind == JsonValueKind.Number,
        "integer" => value.ValueKind == JsonValueKind.Number && (value.TryGetInt64(out _) || value.TryGetUInt64(out _)),
        null => true,
        _ => false
    };
}
