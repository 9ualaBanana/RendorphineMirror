using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Telegram.MediaFiles.Videos;

namespace Telegram.Infrastructure.MediaFiles.Videos;

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
