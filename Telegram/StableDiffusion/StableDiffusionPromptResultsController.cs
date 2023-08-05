using Microsoft.AspNetCore.Mvc;

namespace Telegram.StableDiffusion;

[ApiController]
[Route($"midjourney/results")]
public class StableDiffusionPromptResultsController : ControllerBase
{
    [HttpPost("{promptId}")]
    public async Task Handle(
        Guid promptId,
        IFormFileCollection generatedImagesForms,
        [FromServices] StableDiffusionPrompt.CachedMessages cache,
        [FromServices] GeneratedStableDiffusionImages generatedImages)
    {
        if (cache.TryRetrieveBy(promptId) is StableDiffusionPromptMessage promptMessage)
        {
            var downloadedGeneratedImages = generatedImages
                .DownloadAsyncFrom(generatedImagesForms, HttpContext.RequestAborted)
                .ToEnumerable(); // Copy results to IEnumerable to prevent redownloading.

            await generatedImages.SendAsync(
                promptMessage.Message.Chat.Id,
                promptMessage.Message.MessageId,
                promptId,
                downloadedGeneratedImages,
                HttpContext.RequestAborted);
        }
    }
}
