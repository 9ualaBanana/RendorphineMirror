namespace Common;

public record RequestOptions
{
    public HttpClient HttpClient { get; set; }
    public int RetryAttempts { get; set; }
    public TimeSpan RetryInterval { get; set; }
    public CancellationToken CancellationToken { get; set; }

    public RequestOptions(
        HttpClient? httpClient = null,
        int retryAttempts = 5,
        TimeSpan? retryInterval = null,
        CancellationToken? cancellationToken = null)
    {
        HttpClient = httpClient ?? new();
        RetryAttempts = retryAttempts;
        RetryInterval = retryInterval ?? TimeSpan.FromSeconds(5);
        CancellationToken = cancellationToken ?? CancellationToken.None;
    }
}
