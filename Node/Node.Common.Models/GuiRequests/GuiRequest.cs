namespace Node.Common.Models.GuiRequests;

public abstract record GuiRequest
{
    /// <summary> Being called when request is completed or cancelled </summary>
    [JsonIgnore] public Action OnRemoved = delegate { };
    [JsonIgnore] public TaskCompletionSource<JToken> Task = null!;
}
