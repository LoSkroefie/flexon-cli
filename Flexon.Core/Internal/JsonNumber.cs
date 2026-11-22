using System.Globalization;
using System.Numerics;

namespace Flexon.Internal;

/// <summary>Compares JSON decimal values exactly, independently of notation and binary floating point.</summary>
internal static class JsonNumber
{
    public static bool HasSameValue(string left, string right) => Normalize(left) == Normalize(right);

    private static (string Coefficient, BigInteger Exponent) Normalize(string text)
    {
        var exponentIndex = text.IndexOfAny(new[] { 'e', 'E' });
        var significand = exponentIndex < 0 ? text : text[..exponentIndex];
        var exponent = exponentIndex < 0
            ? BigInteger.Zero
            : BigInteger.Parse(text.AsSpan(exponentIndex + 1), CultureInfo.InvariantCulture);
        var negative = significand[0] == '-';
        if (negative) significand = significand[1..];
        var point = significand.IndexOf('.');
        if (point >= 0)
        {
            exponent -= significand.Length - point - 1;
            significand = significand.Remove(point, 1);
        }
        var digits = significand.TrimStart('0');
        if (digits.Length == 0) return ("0", BigInteger.Zero);
        var coefficient = digits.TrimEnd('0');
        exponent += digits.Length - coefficient.Length;
        return (negative ? "-" + coefficient : coefficient, exponent);
    }
}
