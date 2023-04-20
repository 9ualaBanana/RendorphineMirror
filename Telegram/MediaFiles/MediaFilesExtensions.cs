using Telegram.Infrastructure.CallbackQueries;
using Telegram.Infrastructure.MediaFiles;

namespace Telegram.MediaFiles;

static class MediaFilesExtensions
{
    internal static IServiceCollection AddMediaFiles(this IServiceCollection services)
        => Infrastructure.MediaFiles.MediaFilesExtensions.AddMediaFiles(services
)
        .AddCallbackQueries();
}
