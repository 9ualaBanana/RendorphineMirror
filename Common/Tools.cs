using System.Text.Json.Nodes;

namespace Common
{
    public static class Tools
    {
        public static OperationResult<T> CheckToken<T>(JsonNode? json, string path, string info) =>
            OperationResult.WrapException(() =>
            {
                if (json is null) return OperationResult.Err(@$"Input json node was null in {info}");

                var node = json[path];
                if (node is null) return OperationResult.Err(@$"Json token {path} was null in {info}");

                if (node is JsonValue value && value.TryGetValue(out T? output)) return output.AsOpResult();
                return node.GetValue<T>().AsOpResult();
            }, info);
    }
}