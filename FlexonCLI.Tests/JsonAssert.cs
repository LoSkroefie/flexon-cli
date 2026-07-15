using System.Text.Json;

namespace FlexonCLI.Tests;

internal static class JsonAssert
{
    public static bool DeepEquals(JsonElement left, JsonElement right)
    {
        if (left.ValueKind != right.ValueKind) return false;
        return left.ValueKind switch
        {
            JsonValueKind.Object => ObjectsEqual(left, right),
            JsonValueKind.Array => ArraysEqual(left, right),
            JsonValueKind.String => left.GetString() == right.GetString(),
            JsonValueKind.Number => NumbersEqual(left, right),
            JsonValueKind.True or JsonValueKind.False or JsonValueKind.Null or JsonValueKind.Undefined => true,
            _ => false
        };
    }

    private static bool ObjectsEqual(JsonElement left, JsonElement right)
    {
        var leftProperties = left.EnumerateObject().ToDictionary(item => item.Name, item => item.Value, StringComparer.Ordinal);
        var rightProperties = right.EnumerateObject().ToDictionary(item => item.Name, item => item.Value, StringComparer.Ordinal);
        return leftProperties.Count == rightProperties.Count && leftProperties.All(item => rightProperties.TryGetValue(item.Key, out var value) && DeepEquals(item.Value, value));
    }

    private static bool ArraysEqual(JsonElement left, JsonElement right)
    {
        var leftItems = left.EnumerateArray().ToArray();
        var rightItems = right.EnumerateArray().ToArray();
        return leftItems.Length == rightItems.Length && leftItems.Zip(rightItems).All(pair => DeepEquals(pair.First, pair.Second));
    }

    private static bool NumbersEqual(JsonElement left, JsonElement right)
    {
        if (left.TryGetDecimal(out var leftDecimal) && right.TryGetDecimal(out var rightDecimal)) return leftDecimal == rightDecimal;
        return left.GetDouble().Equals(right.GetDouble());
    }
}
