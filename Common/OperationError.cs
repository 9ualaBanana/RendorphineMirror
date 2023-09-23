using System.Net;

namespace Common;

public interface IOperationError
{
    string ToString();
}

public class StringError : IOperationError
{
    public string Message { get; }

    public StringError(string message) => Message = message;

    public override string ToString() => Message;
}
public class ExceptionError : IOperationError
{
    public Exception Exception { get; }

    public ExceptionError(Exception exception) => Exception = exception;

    public override string ToString() => Exception.ToString();
}
public class HttpError : IOperationError
{
    public bool IsSuccessStatusCode => (int) StatusCode is >= 200 and < 300;

    public string? Message { get; }
    public HttpStatusCode StatusCode { get; }
    public int? ErrorCode { get; }

    public HttpError(string? message, HttpResponseMessage response, int? errorcode) : this(message, response.StatusCode, errorcode) { }
    public HttpError(string? message, HttpStatusCode statuscode, int? errorcode)
    {
        Message = message;
        StatusCode = statuscode;
        ErrorCode = errorcode;
    }

    public override string ToString() => $"HTTP {(int) StatusCode}, Code {ErrorCode?.ToStringInvariant() ?? "null"}: {Message ?? "<no message>"}";
}
