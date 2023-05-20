global using Common;
global using NodeCommon;
global using NodeCommon.NodeUserSettings;
global using NodeCommon.Plugins;
global using NodeCommon.Plugins.Deployment;
global using NodeCommon.Tasks;
global using NodeCommon.Tasks.Model;
using NLog.Web;
using Telegram.Bot;
using Telegram.Commands;
using Telegram.Infrastructure.Middleware.UpdateRouting;
using Telegram.MediaFiles.Images;
using Telegram.MediaFiles.Videos;
using Telegram.Security.Authentication;
using Telegram.Security.Authorization;
using Telegram.Services.GitHub;
using Telegram.Services.Node;
using Telegram.StableDiffusion;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost
    .UseNLog_()
    .AddTelegramBot().ConfigureServices(_ => _
        .AddCommands()
        .AddUpdateRouting()
        .AddImages()
        .AddVideos()
        .AddMPlusAuthorization()
        .AddAuthentication(MPlusAuthenticationDefaults.AuthenticationScheme).AddMPlus()
            .Services.AddScoped<AuthenticationManager>());

// Telegram.Bot works only with Newtonsoft.
builder.Services.AddControllers().AddNewtonsoftJson();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpClient();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<UserNodes>();
builder.Services.AddScoped<GitHubEventForwarder>();
builder.Services.AddStableDiffusion();

var app = builder.Build();

await app.Services.GetRequiredService<TelegramBot>().InitializeAsync();

if (!app.Environment.IsDevelopment())
    app.UseExceptionHandler_();
else
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.MapControllers();
app.UseUpdateRouting();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.Run();


static class Startup
{
    internal static IWebHostBuilder UseNLog_(this IWebHostBuilder builder)
        => builder.UseNLog(new() { ReplaceLoggerFactory = true });
}
