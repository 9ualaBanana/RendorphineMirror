using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Common
{
    public class JsonConfig
    {
        readonly string Path;
        readonly JsonObject JObject;

        public JsonConfig(string path)
        {
            Path = path;

            if (!File.Exists(path)) JObject = new();
            else
            {
                using (var stream = File.OpenRead(path))
                    JObject = (JsonNode.Parse(stream) as JsonObject) ?? new();
            }
        }


        public bool Exists(string path) => JObject.ContainsKey(path);

        [return: NotNullIfNotNull("def")]
        public T? TryGet<T>(string path, T? def = default)
        {
            if (Exists(path)) return Get<T>(path);
            return def;
        }
        public T GetOrThrow<T>(string path)
        {
            if (!Exists(path)) throw new InvalidOperationException(@$"`{path}` field was not found in config");
            return Get<T>(path);
        }

        public T Get<T>(string path)
        {
            var node = JObject[path]!;

            try { return node.GetValue<T>(); }
            catch { return node.Deserialize<T>()!; }
        }
        public IEnumerable<T> Array<T>(string path) => JObject[path]!.AsArray().Select(x => x!.GetValue<T>());

        public void Set<T>(string path, T value)
        {
            JObject[path] = JsonValue.Create(value);
            Save();
        }

        void Save()
        {
            using var file = File.OpenWrite(Path);
            using var utf = new Utf8JsonWriter(file);
            JObject.WriteTo(utf);
        }
    }
}