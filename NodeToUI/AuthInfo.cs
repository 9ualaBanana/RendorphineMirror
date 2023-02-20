namespace NodeToUI;

public readonly struct AuthInfo
{
    public readonly string SessionId, Guid, UserId;
    public readonly string? Email;
    public readonly bool Slave;

    public AuthInfo(string sessionId, string? email, string guid, string userid = null!, bool slave = false)
    {
        SessionId = sessionId;
        Email = email;
        Guid = guid;
        Slave = slave;
        UserId = userid;
    }
}