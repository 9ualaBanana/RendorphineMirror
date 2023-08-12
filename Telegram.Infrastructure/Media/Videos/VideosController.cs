using Microsoft.AspNetCore.Mvc;

namespace Telegram.Infrastructure.Media.Videos;

[ApiController]
[Route($"/{PathFragment}")]
public class VideosController : ControllerBase
{
    internal const string PathFragment = "video";

    [HttpPost]
    public virtual async Task Handle([FromServices] VideoHandler_ videoHandler)
        => await videoHandler.HandleAsync();
}
