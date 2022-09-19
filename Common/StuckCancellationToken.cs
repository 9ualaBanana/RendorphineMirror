namespace Common;

/// <summary> CancellationToken with a TimeSpan delay for when the operation is not progressing </summary>
public struct StuckCancellationToken
{
    public readonly CancellationToken Token;
    public readonly TimeSpan StuckTimeout;
    DateTime LastStuckCheckTime = default;

    public StuckCancellationToken(CancellationToken token) : this(token, TimeSpan.FromDays(365)) { }
    public StuckCancellationToken(TimeSpan stuckTimeout) : this(default, stuckTimeout) { }
    public StuckCancellationToken(CancellationToken token, TimeSpan stuckTimeout)
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
    public void ResetStuck() => LastStuckCheckTime = DateTime.Now + StuckTimeout;
    public void ThrowIfStuck(string text)
    {
        var now = DateTime.Now;

        if (now > LastStuckCheckTime)
            throw new Exception($"{text} for at least {StuckTimeout}");
    }


    public static implicit operator StuckCancellationToken(CancellationToken token) => new(token);
    public static implicit operator StuckCancellationToken(TimeSpan stuckDelay) => new(stuckDelay);
}
