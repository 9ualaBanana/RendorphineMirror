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
    public bool IsSuccessStatusCode => Response.IsSuccessStatusCode;
    public HttpStatusCode StatusCode => Response.StatusCode;

    public HttpResponseMessage Response { get; }
    public int? ErrorCode { get; }
    public string? Error { get; }

    public HttpError(string? error, HttpResponseMessage response, int? errorCode)
    {
        Error = error;
        Response = response;
        ErrorCode = errorCode;
    }

    public override string ToString() => $"HTTP {StatusCode}; Code {ErrorCode}; {Error}";
}