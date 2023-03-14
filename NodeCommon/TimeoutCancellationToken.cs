namespace NodeCommon;

/// <summary> CancellationToken with a TimeSpan delay for when the operation is not progressing </summary>
public struct TimeoutCancellationToken
{
    public readonly CancellationToken Token;
    public readonly TimeSpan StuckTimeout;
    DateTimeOffset LastStuckCheckTime = default;

    public TimeoutCancellationToken(CancellationToken token) : this(token, TimeSpan.FromDays(365)) { }
    public TimeoutCancellationToken(TimeSpan stuckTimeout) : this(default, stuckTimeout) { }
    public TimeoutCancellationToken(CancellationToken token, TimeSpan stuckTimeout)
    {
        Token = token;
        StuckTimeout = stuckTimeout;

        ResetStuck();
    }



    public bool IsCancellationRequested => Token.IsCancellationRequested;
    public void ThrowIfCancellationRequested() => Token.ThrowIfCancellationRequested();

    public void CheckStuck(bool test, string text)
    {
        if (test) ThrowIfStuck(text);
        else ResetStuck();
    }
    public void CheckStuck<T>(ref T variable, T check, string text)
    {
        CheckStuck(EqualityComparer<T>.Default.Equals(variable, check), text);
        variable = check;
    }
    public void ResetStuck() => LastStuckCheckTime = DateTimeOffset.Now + StuckTimeout;
    public void ThrowIfStuck(string text)
    {
        var now = DateTimeOffset.Now;

        if (now > LastStuckCheckTime)
            throw new Exception($"{text} for at least {StuckTimeout}");
    }


    public static implicit operator TimeoutCancellationToken(CancellationToken token) => new(token);
    public static implicit operator TimeoutCancellationToken(TimeSpan stuckDelay) => new(stuckDelay);
}
