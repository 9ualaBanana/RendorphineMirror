using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NodeToUI;

public static class LocalPipe
{
    readonly static NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

    public static Task<Stream> SendAsync(string uri) => SendAsync(new HttpRequestMessage(HttpMethod.Get, uri));
    public static async Task<Stream> SendAsync(HttpRequestMessage msg)
    {
        var data = await new HttpClient().SendAsync(msg, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
        return await data.Content.ReadAsStreamAsync().ConfigureAwait(false);
    }
    public static Task StartReadingAsync<T>(Stream stream, Action<T> onReceive, CancellationToken token) =>
        StartReadingAsync(stream, (JToken token) => onReceive(token.ToObject<T>(JsonSettings.TypedS)!), token);
    public static async Task StartReadingAsync(Stream stream, Action<JToken> onReceive, CancellationToken token)
    {
        try
        {
            var reader = new JsonTextReader(new StreamReader(stream)) { SupportMultipleContent = true };

            while (true)
            {
                if (token.IsCancellationRequested) return;

                var read = await reader.ReadAsync(token).ConfigureAwait(false);
                if (!read) return;
                var tk = await JToken.LoadAsync(reader, token).ConfigureAwait(false);

                onReceive(tk);
            }
        }
        catch (Exception ex) { _logger.Error(ex, "LocalPipe read stream died"); }
    }
    public static JsonTextReader CreateReader(Stream stream) => new JsonTextReader(new StreamReader(stream)) { SupportMultipleContent = true };


    public class Writer : IDisposable
    {
        readonly Stream Stream;

        public Writer(Stream stream) => Stream = stream;

        public Task WriteAsync<T>(T value) where T : notnull => WriteAsync(JToken.FromObject(value, JsonSettings.TypedS));
        public async Task WriteAsync(JToken token)
        {
            try
            {
                await Stream.WriteAsync(Encoding.UTF8.GetBytes(token.ToString(Formatting.None)));
                await Stream.FlushAsync();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "LocalPipe write stream died");
                Dispose();
                throw;
            }
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Stream.Dispose();
        }
    }
}
