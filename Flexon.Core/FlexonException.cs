namespace Flexon;

public class FlexonException : Exception
{
    public FlexonException(string message) : base(message) { }
    public FlexonException(string message, Exception innerException) : base(message, innerException) { }
}

public sealed class FlexonFormatException : FlexonException
{
    public FlexonFormatException(string message) : base(message) { }
    public FlexonFormatException(string message, Exception innerException) : base(message, innerException) { }
}

public sealed class FlexonAuthenticationException : FlexonException
{
    public FlexonAuthenticationException(string message) : base(message) { }
    public FlexonAuthenticationException(string message, Exception innerException) : base(message, innerException) { }
}
