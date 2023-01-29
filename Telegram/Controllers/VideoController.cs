using Microsoft.AspNetCore.Mvc;

namespace Telegram.Controllers;

[ApiController]
[Route($"telegram/{{token}}/{PathFragment}")]
public class VideoController : ControllerBase
{
    internal const string PathFragment = "video";

    [HttpPost]
    public async Task Handle()
    {

    }
}
