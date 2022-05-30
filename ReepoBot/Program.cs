using ReepoBot.Services;
using ReepoBot.Services.GitHub;
using ReepoBot.Services.Telegram;
using Telegram.Bot;
using Telegram.Bot.Types;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddFile("log.txt");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var token = Environment.GetEnvironmentVariable("BOT_TOKEN", EnvironmentVariableTarget.User)!;
var bot = new TelegramBot(token);
bot.SetWebhookAsync("https://1903-213-87-161-72.eu.ngrok.io/telegram", dropPendingUpdates: true);
builder.Services.AddSingleton(bot);

builder.Services.AddScoped<WebhookEventHandlerFactory<TelegramUpdateHandler, Update>, TelegramUpdateHandlerFactory>();
builder.Services.AddScoped<WebhookEventHandlerFactory<GitHubWebhookEventForwarder, string>, GitHubWebhookEventForwarderFactory>();

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
