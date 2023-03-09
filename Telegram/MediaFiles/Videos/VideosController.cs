using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Telegram.MediaFiles.Videos;

[ApiController]
[Route($"telegram/{{token}}/{PathFragment}")]
public class VideosController : ControllerBase
{
    internal const string PathFragment = "video";

    [HttpPost]
    [Authorize]
    public async Task Handle([FromServices] ProcessingMethodSelectorVideoHandler videoHandler)
      => await videoHandler.HandleAsync(HttpContext);
}
