global using Common;
global using NodeCommon;
global using NodeCommon.NodeUserSettings;
global using NodeCommon.Plugins;
global using NodeCommon.Plugins.Deployment;
global using NodeCommon.Tasks;
global using NodeCommon.Tasks.Model;
using NLog.Web;
using Telegram.Commands;
using Telegram.Infrastructure.Bot;
using Telegram.Infrastructure.Middleware.UpdateRouting;
using Telegram.Localization;
using Telegram.MediaFiles.Images;
using Telegram.MediaFiles.Videos;
using Telegram.Security.Authentication;
using Telegram.Security.Authorization;
using Telegram.Services.GitHub;
using Telegram.Services.Node;
using Telegram.StableDiffusion;

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
        .AddLocalization_())

    .UseNLog_();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<UserNodes>();
builder.Services.AddScoped<GitHubEventForwarder>();
builder.Services.AddStableDiffusion();

var app = builder.Build();

await app.Services.GetRequiredService<TelegramBot>().InitializeAsync();

if (!app.Environment.IsDevelopment())
    app.UseTelegramBotExceptionHandler();
else
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseUpdateRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseRequestLocalization();

app.Run();


static class Startup
{
    internal static IWebHostBuilder UseNLog_(this IWebHostBuilder builder)
        => builder.UseNLog(new() { ReplaceLoggerFactory = true });
}
