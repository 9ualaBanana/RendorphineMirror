using Microsoft.AspNetCore.Mvc;

namespace Telegram.OneClick;

[ApiController]
[Route("oneclick")]
public class OneClickController
{
	readonly TelegramBot _bot;
	readonly ChatId[] _subscribers;

	static readonly string[] _imageExtensions = new string[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff", ".ico" };

	public OneClickController(TelegramBot bot)
    {
		_bot = bot;
		_subscribers = new ChatId[]
		{
            624598614,	// GIBSON
			223960353,	// Kos
			466253221	// 13ym
        };
    }

    [HttpPost("display_renders")]
	public async Task DisplayRenders(IFormFileCollection renders, CancellationToken cancellationToken)
	{
		var renders_ = renders.Select(_ => new { Stream = _.OpenReadStream(), Name = _.FileName });
		var album = renders_.Select(_ => ConvertToAlbumInputMedia(_.Stream, _.Name));
        try
		{
            foreach (var subscriber in _subscribers)
				await _bot.SendAlbumAsync_(subscriber, album, cancellationToken: cancellationToken);
		}
		finally
		{
			foreach (var render in renders_.Select(_ => _.Stream))
				await render.DisposeAsync();
        }


		static IAlbumInputMedia ConvertToAlbumInputMedia(Stream content, string name)
		{
            bool isImage = _imageExtensions.Contains(Path.GetExtension(name));
			var inputMedia = new InputMedia(content, name);
            return isImage ? new InputMediaPhoto(inputMedia) : new InputMediaVideo(inputMedia);
		}
	}

	[HttpPost("display_render_error")]
    public async Task DisplayRenderError(string error, CancellationToken cancellationToken)
    {
        foreach (var subscriber in _subscribers)
            await _bot.SendMessageAsync_(subscriber, error, cancellationToken: cancellationToken);
    }
}
