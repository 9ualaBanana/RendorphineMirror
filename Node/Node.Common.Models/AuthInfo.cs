namespace Node.Common.Models;

public record AuthInfo(string SessionId, string? Email, string Guid, string UserId, bool Slave = false);
