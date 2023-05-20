namespace NodeToUI;

public record AuthInfo(string SessionId, string? Email, string Guid, string UserId, bool Slave = false);