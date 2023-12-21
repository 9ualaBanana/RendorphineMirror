using Node.Common.Models.GuiRequests;

namespace Node.Common.Models;

public interface INodeGui
{
    Task<OperationResult<TResult>> Request<TResult>(GuiRequest request, CancellationToken token);

    async Task<OperationResult<string>> RequestCaptchaInputAsync(string base64image, CancellationToken token = default) => await Request<string>(new CaptchaRequest(base64image), token);
}
