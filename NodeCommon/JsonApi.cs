namespace NodeCommon;

public static class JsonApi
{
    static readonly JObject SuccessObj = JsonFromOpResult((OperationResult) true);

    public static JObject JsonFromOpResult(in OperationResult result)
    {
        var json = new JObject() { ["ok"] = new JValue(result.Success ? 1 : 0), };
        if (!result) json["errormessage"] = result.Error.ToString();

        return json;
    }
    public static JObject JsonFromOpResult<T>(in OperationResult<T> result) => JsonFromOpResult(result, "value");
    public static JObject JsonFromOpResult<T>(in OperationResult<T> result, string propertyname)
    {
        var json = JsonFromOpResult(result.OpResult);
        if (result) json[propertyname] = JToken.FromObject(result.Value!);

        return json;
    }

    public static JObject JsonFromOpResult(JToken token) => JsonFromOpResult(token, "value");
    public static JObject JsonFromOpResult(JToken token, string propertyname)
    {
        var json = JsonFromOpResult((OperationResult) true);
        json[propertyname] = token;

        return json;
    }

    public static JObject Success() => SuccessObj;
    public static JObject Success<T>(T value) => JsonFromOpResult(value.AsOpResult());
    public static JObject Error(string text) => JsonFromOpResult(OperationResult.Err(text));
}
