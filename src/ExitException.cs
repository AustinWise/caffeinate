namespace caffeinate;

internal class ExitException : Exception
{
    public ExitException(string? message) : base(message)
    {
    }
}
