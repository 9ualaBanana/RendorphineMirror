using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Telegram.MediaFiles.Images;

namespace Telegram.Infrastructure.MediaFiles.Images;

[ApiController]
[Route($"telegram/{{token}}/{PathFragment}")]
public class ImagesController : ControllerBase
{
    internal const string PathFragment = "image";

    [HttpPost]
    [Authorize]
    public async Task Handle([FromServices] ProcessingMethodSelectorImageHandler imageHandler)
      => await imageHandler.HandleAsync();
}
