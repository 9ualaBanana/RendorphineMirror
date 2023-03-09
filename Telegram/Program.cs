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
using Telegram.MediaFiles.Images;
using Telegram.MediaFiles.Videos;
using Telegram.Middleware.UpdateRouting;
using Telegram.Security.Authentication;
using Telegram.Security.Authorization;
using Telegram.Services.GitHub;
using Telegram.Services.Node;
using Telegram.Tasks.ResultPreview;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Host.UseNLog();

builder.Services.AddTelegramBotUsing(builder.Configuration).AddCommands();
builder.Services.AddUpdateRouting();
builder.Services.AddAuthentication(MPlusViaTelegramChatDefaults.AuthenticationScheme)
    .AddMPlusViaTelegramChat();
builder.Services.AddAuthorizationWithHandlers();

builder.Services
    .AddImageProcessing()
    .AddVideoProcessing();

builder.Services.AddScoped<TelegramPreviewTaskResultHandler>();

// Telegram.Bot works only with Newtonsoft.
builder.Services.AddControllers().AddNewtonsoftJson();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpClient();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<UserNodes>();
builder.Services.AddScoped<GitHubEventForwarder>();

var app = builder.Build();

await app.Services.GetRequiredService<TelegramBot>().InitializeAsync();

if (app.Environment.IsDevelopment())
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
