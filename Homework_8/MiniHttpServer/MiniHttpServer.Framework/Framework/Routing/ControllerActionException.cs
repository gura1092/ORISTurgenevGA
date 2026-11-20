namespace MiniHttpServer.Framework.Routing;

public sealed class ControllerActionException : Exception
{
    public int StatusCode { get; }

    public ControllerActionException(int statusCode, string message) : base(message)
    {
        if (statusCode is < 400 or > 599)
            throw new ArgumentOutOfRangeException(nameof(statusCode));

        StatusCode = statusCode;
    }
}
