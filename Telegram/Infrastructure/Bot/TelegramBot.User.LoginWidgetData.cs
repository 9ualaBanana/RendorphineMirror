using Microsoft.AspNetCore.Mvc;

namespace Telegram.Infrastructure.Bot;

public partial class TelegramBot
{
    public partial record User
    {
        public record LoginWidgetData(
            [ModelBinder(Name = LoginWidgetData.username)] string? UserName,
            [ModelBinder(Name = LoginWidgetData.first_name)] string? FirstName,
            [ModelBinder(Name = LoginWidgetData.last_name)] string? LastName,
            [ModelBinder(Name = LoginWidgetData.photo_url)] Uri? ProfilePicture,
            [ModelBinder(Name = LoginWidgetData.auth_date)] long AuthenticationDate,
            [ModelBinder(Name = LoginWidgetData.hash)] string Hash)
        {
            internal const string username = nameof(username);
            internal const string first_name = nameof(first_name);
            internal const string last_name = nameof(last_name);
            internal const string photo_url = nameof(photo_url);
            internal const string auth_date = nameof(auth_date);
            internal const string hash = nameof(hash);

            internal QueryString ToQueryString()
                => QueryString.Create(new Dictionary<string, string?>
                {
                    [username] = UserName,
                    [first_name] = FirstName,
                    [last_name] = LastName,
                    [photo_url] = ProfilePicture?.AbsoluteUri,
                    [auth_date] = AuthenticationDate.ToString(),
                    [hash] = Hash
                });
        }
    }
}
