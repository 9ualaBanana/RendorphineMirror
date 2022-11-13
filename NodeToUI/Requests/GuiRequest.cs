using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NodeToUI.Requests;

public abstract record GuiRequest
{
    [JsonIgnore] public Action OnRemoved = delegate { };
    [JsonIgnore] public TaskCompletionSource<JToken> Task = null!;
}