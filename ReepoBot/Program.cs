using ReepoBot.Services;
using ReepoBot.Services.GitHub;
using ReepoBot.Services.Node;
using ReepoBot.Services.Telegram;
using ReepoBot.Services.Telegram.UpdateHandlers;
using Telegram.Bot;
using Telegram.Bot.Types;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddFile("log.txt");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

await InitializeBot();

builder.Services.AddScoped<TelegramUpdateHandler>();
builder.Services.AddScoped<GitHubWebhookEventForwarder>();
builder.Services.AddScoped<HardwareInfoForwarder>();
builder.Services.AddSingleton<NodeSupervisor>();

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