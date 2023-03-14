﻿using Telegram.Bot;

namespace Telegram.Infrastructure.MediaFiles;

public class MediaFileDownloader
{
    readonly TelegramBot _bot;
    readonly HttpClient _httpClient;

    public MediaFileDownloader(TelegramBot bot, IHttpClientFactory httpClientFactory)
    {
        _bot = bot;
        _httpClient = httpClientFactory.CreateClient();
    }

    internal async Task UseAsyncToDownload(MediaFile mediaFile, string destinationPath, CancellationToken cancellationToken)
    {
        using var downloadedMediaFile = File.Create(destinationPath);

        if (mediaFile.FileId is not null)
            await _bot.GetInfoAndDownloadFileAsync(mediaFile.FileId, downloadedMediaFile, cancellationToken);
        else await
            (await _httpClient.GetStreamAsync(mediaFile.Location, cancellationToken))
                .CopyToAsync(downloadedMediaFile, cancellationToken);
    }
}
