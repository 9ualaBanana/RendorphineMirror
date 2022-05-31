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

app.Run($"{builder.Configuration["HostServer"]}");

async Task InitializeBot()
{
    var token = builder.Configuration["BotToken"];
    var bot = new TelegramBot(token);
    await bot.SetWebhookAsync($"{builder.Configuration["HostServer"]}/telegram");
    builder.Services.AddSingleton(bot);
}