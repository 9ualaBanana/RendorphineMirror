namespace NodeToUI.Requests;

public record InputTurboSquidModelInfoRequest(ImmutableArray<InputTurboSquidModelInfoRequest.ModelInfo> Infos, ImmutableDictionary<string, ImmutableArray<string>> FormatRenderers) : GuiRequest
{
    public record ModelInfo(string Name);

    public record Response(ImmutableArray<Response.ResponseModelInfo> Infos)
    {
        public record ResponseModelInfo(bool IsNative, string Format, string Renderer, string FormatVersion, string RendererVersion);
    }
}