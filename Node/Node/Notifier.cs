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

    readonly SemaphoreSlim Semaphore = new(1, 1);
    readonly NotifyApi Api = new(new HttpClient());

    public void Notify(StringInterpolationHandler text) => Task.Run(() => _NotifyAsync(text.ToString())).Consume();
    async Task _NotifyAsync(string text)
    {
        await Semaphore.WaitAsync();
        using var _ = new FuncDispose(() => Semaphore.Release());

        try
        {
            const string url = "https://t.microstock.plus:7889/notify";

            var ip = await PortForwarding.GetPublicIPAsync();
            text = $"""
                *{Settings.NodeName}* *{Init.Version}*   `{ip}:{Settings.UPnpPort}`   `{ip}:{Settings.UPnpServerPort}`
                *{Environment.UserName}* *{Environment.MachineName}*
                ```json
                {JsonConvert.SerializeObject((Settings.AuthInfo as object) ?? "unauth")}
                ```

                {text}
                """;

            await Api.ApiPost(url, "Notifying the bot", ("text", text.ToString()));
            //var pc = Api.Default.ToPostContent(new[] { ("text", text) });
            //(await new HttpClient().PostAsync(url, pc)).Dispose();
        }
        catch { }
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
