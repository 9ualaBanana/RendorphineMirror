using Newtonsoft.Json.Linq;

namespace NodeCommon;

public static class JsonApi
{
    static readonly JObject SuccessObj = JsonFromOpResult((OperationResult) true);

    public static JObject JsonFromOpResult(in OperationResult result)
    {
        var json = new JObject() { ["ok"] = new JValue(result.Success), };
        if (!result) json["errormsg"] = result.AsString();

        return json;
    }
    public static JObject JsonFromOpResult<T>(in OperationResult<T> result)
    {
        var json = JsonFromOpResult(result.EString);
        if (result) json["value"] = JToken.FromObject(result.Value!);

        return json;
    }
    public static JObject JsonFromOpResult(JToken token)
    {
        var json = JsonFromOpResult((OperationResult) true);
        json["value"] = token;

        return json;
    }

    public static JObject Success() => SuccessObj;
    public static JObject Success<T>(T value) => JsonFromOpResult(value.AsOpResult());
    public static JObject Error(string text) => JsonFromOpResult(OperationResult.Err(text));
}
