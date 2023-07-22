using System.Text;
using Telegram.Bot.Types;
using Telegram.Infrastructure.Middleware.UpdateRouting;

namespace Telegram.Infrastructure.Bot;

static class TelegramBotExtensions
{
    internal static IWebHostBuilder ConfigureTelegramBot(
        this IWebHostBuilder builder,
        Action<ITelegramBotBuilder> configure)
    {
        builder
            .ConfigureAppConfiguration(_ => _
                .AddJsonFile("botsettings.json", optional: false, reloadOnChange: false)
                .AddJsonFile($"botsettings.{Environments.Development}.json", optional: true, reloadOnChange: false));

        configure(TelegramBot.Builder.Default(builder.Services()));

        return builder;
    }

    internal static WebApplication UseTelegramBot(this WebApplication app)
        => app.UseUpdateRouting();

    internal static async Task RunAsync_(this WebApplication app)
    { await app.Services.GetRequiredService<TelegramBot>().InitializeAsync(); app.Run(); }

    #region Properties

    internal static ChatId ChatId(this Update update) =>
        update.Message?.Chat.Id ??
        update.CallbackQuery?.Message?.Chat.Id ??
        update.InlineQuery?.From.Id ??
        update.ChosenInlineResult?.From.Id ??
        update.ChannelPost?.Chat.Id ??
        update.EditedChannelPost?.Chat.Id ??
        update.ShippingQuery?.From.Id ??
        update.PreCheckoutQuery?.From.Id!;

    internal static User From(this Update update) =>
        update.Message?.From ??
        update.CallbackQuery?.From ??
        update.InlineQuery?.From ??
        update.ChosenInlineResult?.From ??
        update.ChannelPost?.From ??
        update.EditedChannelPost?.From ??
        update.ShippingQuery?.From ??
        update.PreCheckoutQuery?.From!;

    /// <summary>
    /// Sets <paramref name="caption"/> of the <paramref name="album"/>
    /// also removing captions from individual media files constituting it.
    /// </summary>
    /// <remarks>
    /// <paramref name="album"/> caption is set via the first media file constituting it.
    /// If any other constituent media files have their <see cref="InputMediaBase.Caption"/> set,
    /// then they are treated as captions of individual media files and not the <paramref name="album"/>.
    /// </remarks>
    internal static IEnumerable<IAlbumInputMedia> Caption(this IEnumerable<IAlbumInputMedia> album, string caption)
    {
        if (album.FirstOrDefault() is InputMediaBase album_)
        {
            album_.Caption = caption;
            foreach (var mediaFile in album.Skip(1))
                (mediaFile as InputMediaBase)!.Caption = null;
        }

        return album;
    }

    #endregion

    internal static string Sanitize(this string unsanitizedString)
    {
        return unsanitizedString
            .Replace("|", @"\|")
            .Replace("[", @"\[")
            .Replace("]", @"\]")
            .Replace("{", @"\{")
            .Replace("}", @"\}")
            .Replace(".", @"\.")
            .Replace("-", @"\-")
            .Replace("+", @"\+")
            .Replace("_", @"\_")
            .Replace(">", @"\>")
            .Replace("(", @"\(")
            .Replace(")", @"\)")
            .Replace("=", @"\=")
            .Replace("!", @"\!")
            .Replace("#", @"\#");
    }

    public static StringBuilder AppendHeader(this StringBuilder builder, string header)
        => builder
        .AppendLine(header)
        .AppendLine(HorizontalDelimeter);

    public const string HorizontalDelimeter = "------------------------------------------------------------------------------------------------";
}
