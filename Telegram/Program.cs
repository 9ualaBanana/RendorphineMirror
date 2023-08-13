global using Common;
global using Node.Common.Models;
global using Node.Plugins.Models;
global using Node.Tasks.Models;
global using Node.Tasks.Models.ExecInfo;
global using NodeCommon;
global using NodeCommon.Tasks;
global using NodeCommon.Tasks.Model;
using NLog.Web;
using Telegram.Bot.Types.Enums;
using Telegram.Commands;
using Telegram.Infrastructure.Bot;
using Telegram.Localization;
using Telegram.MediaFiles.Images;
using Telegram.MediaFiles.Videos;
using Telegram.Security.Authentication;
using Telegram.Security.Authorization;
using Telegram.Services.GitHub;
using Telegram.Services.Node;
using Telegram.StableDiffusion;
using Telegram.TrialUsers;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost
    .ConfigureTelegramBot(_ => _
        .AddCommands()
        .AddImages()
        .AddVideos()
        .AddMPlusAuthentication()
        .AddMPlusAuthorization()
        .ConfigureExceptionHandlerOptions())

    .ConfigureServices(_ => _
        .AddTrialUsers()
        .AddStableDiffusion()
        .AddRequestLocalization_()
        .AddSwaggerGen()

        .AddSingleton<UserNodes>()
        .AddScoped<GitHubEventForwarder>())

    .UseNLog_();

var app = builder.Build();

app.UseTelegramBot()
    .UseAuthentication()
    .UseAuthorization()
    .UseRequestLocalization();
if (!app.Environment.IsDevelopment())
    app.UseTelegramBotExceptionHandler();
else app.UseDeveloperExceptionPage()
        .UseSwagger().UseSwaggerUI();

await app.RunAsync_(
    allowedUpdates: new UpdateType[] { UpdateType.Message, UpdateType.CallbackQuery },
    dropPendingUpdates: true);


static class Startup
{
    /// <inheritdoc cref="AspNetExtensions.UseNLog(IHostBuilder)"/>
    internal static IWebHostBuilder UseNLog_(this IWebHostBuilder builder)
        => builder.UseNLog(new() { ReplaceLoggerFactory = true });
}
