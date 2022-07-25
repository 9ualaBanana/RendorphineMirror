using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Common.NodeToUI;

public static class LocalPipe
{
    public static Task<Stream> SendLocalAsync(string prefix) =>
        SendAsync(new HttpRequestMessage(HttpMethod.Get, $"http://127.0.0.1:{Settings.LocalListenPort}/{prefix}"));
    public static async Task<Stream> SendAsync(HttpRequestMessage msg)
    {
        var data = await new HttpClient().SendAsync(msg, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
        return await data.Content.ReadAsStreamAsync().ConfigureAwait(false);
    }
    public static Task StartReadingAsync<T>(Stream stream, Action<T> onReceive, CancellationToken token) =>
        StartReadingAsync(stream, (JToken token) => onReceive(token.ToObject<T>(LocalApi.JsonSerializerWithType)!), token);
    public static async Task StartReadingAsync(Stream stream, Action<JToken> onReceive, CancellationToken token)
    {
        try
        {
            var reader = new JsonTextReader(new StreamReader(stream)) { SupportMultipleContent = true };

            while (true)
            {
                if (token.IsCancellationRequested) return;

                var read = await reader.ReadAsync().ConfigureAwait(false);
                if (!read) return;
                var tk = await JToken.LoadAsync(reader).ConfigureAwait(false);

                onReceive(tk);
            }
        }
        catch (Exception ex) { Log.Error($"LocalPipe read stream died: {ex.Message}"); }
    }
    public static JsonTextReader CreateReader(Stream stream) => new JsonTextReader(new StreamReader(stream)) { SupportMultipleContent = true };


    public class Writer
    {
        readonly JsonTextWriter JWriter;
        readonly Stream Stream;

        public Writer(Stream stream)
        {
            Stream = stream;
            JWriter = new JsonTextWriter(new StreamWriter(stream) { AutoFlush = true });
        }

        public Task<bool> WriteAsync<T>(T value) where T : notnull => WriteAsync(JToken.FromObject(value, LocalApi.JsonSerializerWithType));
        public async Task<bool> WriteAsync(JToken token)
        {
            try
            {
                await token.WriteToAsync(JWriter).ConfigureAwait(false);
                await JWriter.FlushAsync().ConfigureAwait(false);

                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"LocalPipe write stream died: {ex.Message}");
                return false;
            }
        }
    }
}