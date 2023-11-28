namespace NodeCommon;

public static class JsonApi
{
    static readonly JObject SuccessObj = JsonFromOpResult((OperationResult) true);

    public static JObject JsonFromOpResult(in OperationResult result)
    {
        var json = new JObject();
        AppendOpResult(json, result);

        return json;
    }
    public static void AppendOpResult(JObject json, in OperationResult result)
    {
        json["ok"] = new JValue(result.Success ? 1 : 0);
        if (!result) json["errormessage"] = result.Error.ToString();
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

    public static JObject JsonFromOpResultInline<T>(in OperationResult<T> result) where T : class
    {
        var json =
            result.Success
            ? JObject.FromObject(result.Value)
            : new JObject();

        AppendOpResult(json, result.GetResult());
        return json;
    }

    public static JObject Success() => SuccessObj;
    public static JObject Success<T>(T value) => JsonFromOpResult(value.AsOpResult());
    public static JObject Error(string text) => JsonFromOpResult(OperationResult.Err(text));
}
