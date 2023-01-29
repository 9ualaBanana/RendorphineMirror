using Microsoft.AspNetCore.Mvc;

namespace Telegram.Controllers;

[ApiController]
[Route($"telegram/{{token}}/{PathFragment}")]
public class ImageController : ControllerBase
{
    internal const string PathFragment = "image";

    [HttpPost]
    public async Task Handle()
    {

    }
}
