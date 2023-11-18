namespace Node.Common;

public class TaskValidationException : Exception
{
    public TaskValidationException(string message) : base(message) { }
    public TaskValidationException(string message, Exception innerException) : base(message, innerException) { }
}
