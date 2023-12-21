namespace Node.Common.Models.GuiRequests;

public record InputTurboSquidModelInfoRequest(ImmutableArray<InputTurboSquidModelInfoRequest.ModelInfo> Infos) : GuiRequest
{
    public record ModelInfo(string Name, string Format, ImmutableArray<string>? Renderers);

    public record Response(ImmutableArray<Response.ResponseModelInfo?> Infos)
    {
        public record ResponseModelInfo(bool IsNative, string Renderer, double FormatVersion, double? RendererVersion);
    }
}
