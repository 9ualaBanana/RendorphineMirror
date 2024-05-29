namespace StatusNotifier;

public class Notification
{
    public int Id { get; set; }
    public required long Time { get; set; }
    public required string Content { get; set; }

    public required string Nickname { get; set; }
    public required string NodeVersion { get; set; }

    public required string Ip { get; set; }
    public required int PublicPort { get; set; }
    public required string Host { get; set; }

    public required string Username { get; set; }
    public required string MachineName { get; set; }

    public required string AuthInfo { get; set; }
}
