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

public class HttpErrorBase : IOperationError
{
    public bool IsSuccessStatusCode => (int) StatusCode is >= 200 and < 300;

    public string? Message { get; }
    public HttpStatusCode StatusCode { get; }

    public HttpErrorBase(string? message, HttpResponseMessage response) : this(message, response.StatusCode) { }
    public HttpErrorBase(string? message, HttpStatusCode statuscode)
    {
        Message = message;
        StatusCode = statuscode;
    }

    public override string ToString() => $"HTTP {(int) StatusCode}: {Message ?? "<no message>"}";
}
public class HttpError : HttpErrorBase
{
    public int? ErrorCode { get; }

    public HttpError(string? message, HttpResponseMessage response, int? errorcode) : this(message, response.StatusCode, errorcode) { }
    public HttpError(string? message, HttpStatusCode statuscode, int? errorcode) : base(message, statuscode) => ErrorCode = errorcode;

    public override string ToString() => $"HTTP {(int) StatusCode}, Code {ErrorCode?.ToStringInvariant() ?? "null"}: {Message ?? "<no message>"}";
}
