using Microsoft.AspNetCore.Mvc;

namespace Telegram.Infrastructure.Media.Images;

[ApiController]
[Route($"/{PathFragment}")]
public class ImagesController : ControllerBase
{
    internal const string PathFragment = "image";

    [HttpPost]
    public virtual async Task Handle([FromServices] ImageHandler_ imageHandler)
        => await imageHandler.HandleAsync();
}
