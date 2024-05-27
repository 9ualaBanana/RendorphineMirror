using System.Runtime.CompilerServices;
using System.Text;

namespace Node;

[AutoRegisteredService(true)]
public class Notifier
{
    record NotifyApi : Api
    {
        public NotifyApi(HttpClient client) : base(client) { }

        protected override bool NeedsToRetryRequest(OperationResult result) => false;
    }

    public required Init Init { get; init; }
    public required ILogger<Notifier> Logger { get; init; }

    readonly SemaphoreSlim Semaphore = new(1, 1);
    readonly NotifyApi Api = new(new HttpClient());

    public void Notify(StringInterpolationHandler text) => Task.Run(() => _NotifyAsync(text.ToString())).Consume();
    async Task _NotifyAsync(string text)
    {
        await Semaphore.WaitAsync();
        using var _ = new FuncDispose(() => Semaphore.Release());

        try
        {
            const string url = "https://t.microstock.plus:7889/send";
            //const string url = "http://127.0.0.1:5014/send";

            var ip = await PortForwarding.GetPublicIPAsync();
            var host = await PortForwarding.TryReadNginxHost(default);
            var hoststr = host is null ? "nohost" : $"{host.Value.host}:{host.Value.port}";

            var notif2 = new[]
            {
                ("Time", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToStringInvariant()),
                ("Content", text),

                ("Nickname", Settings.NodeName),
                ("NodeVersion", Init.Version),

                ("Ip", ip.ToString()),
                ("PublicPort", Settings.UPnpPort.ToStringInvariant()),
                ("Host", hoststr),

                ("Username", Environment.UserName),
                ("MachineName", Environment.MachineName),

                ("AuthInfo", JsonConvert.SerializeObject((Settings.AuthInfo as object) ?? "unauth")),
            };
            await Api.ApiPost(url, "Notifying the bot2", notif2).ThrowIfError();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, $"Error notifying");
        }
    }


    /// <summary> InterpolatedStringHandler that escapes all interpolated values </summary>
    [InterpolatedStringHandler]
    public class StringInterpolationHandler
    {
        readonly StringBuilder StringBuilder = new();

        public StringInterpolationHandler() { }
        public StringInterpolationHandler(int literalLength, int formattedCount) : this() { }

        public void AppendLiteral(string value) => StringBuilder.Append(value);

        public void AppendFormatted<T>(T value)
        {
            var str = (value?.ToString() ?? "")
                .Replace(@"`", @"\`")
                .Replace(@"\", @"\\")
                .Replace(@"_", @"\_")
                .Replace(@"*", @"\*")
                .Replace(@"[", @"\[")
                .Replace(@"]", @"\]");

            StringBuilder.Append(str);
        }
        public void AppendFormatted<T>(T value, string format)
        {
            if (format == "n") AppendLiteral(value?.ToString() ?? "");
            else AppendFormatted(value);
        }

        public override string ToString() => StringBuilder.ToString();

        public static implicit operator StringInterpolationHandler(string str)
        {
            var handler = new StringInterpolationHandler();
            handler.AppendLiteral(str);

            return handler;
        }
    }
}
