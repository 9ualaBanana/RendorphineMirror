using ReepoBot.Services;
using ReepoBot.Services.GitHub;
using ReepoBot.Services.Hardware;
using ReepoBot.Services.Telegram;
using Telegram.Bot;
using Telegram.Bot.Types;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddFile("log.txt");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

await InitializeBot();

builder.Services.AddScoped<WebhookEventHandlerFactory<TelegramUpdateHandler, Update>, TelegramUpdateHandlerFactory>();
builder.Services.AddScoped<WebhookEventHandlerFactory<GitHubWebhookEventForwarder, string>, GitHubWebhookEventForwarderFactory>();
builder.Services.AddScoped<HardwareInfoForwarder>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

async Task InitializeBot()
{
    var token = Environment.GetEnvironmentVariable("BOT_TOKEN", EnvironmentVariableTarget.User)!;
    var bot = new TelegramBot(token);
    await bot.SetWebhookAsync($"{Environment.GetEnvironmentVariable("SERVER_HOST", EnvironmentVariableTarget.Machine)}/telegram");
    builder.Services.AddSingleton(bot);
}