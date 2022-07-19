using NLog.Web;
using ReepoBot.Services.GitHub;
using ReepoBot.Services.Node;
using ReepoBot.Services.Telegram;
using Telegram.Bot;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Host.UseNLog();

builder.Services.AddControllers().AddNewtonsoftJson();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

Task<TelegramBot> botInitialization = InitializeBot();

builder.Services.AddScoped<TelegramUpdateHandler>();
builder.Services.AddScoped<GitHubEventForwarder>();
builder.Services.AddSingleton<NodeSupervisor>();
builder.Services.AddSingleton(await botInitialization);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run(builder.Configuration["Host"]);

async Task<TelegramBot> InitializeBot()
{
    var token = builder.Configuration["BotToken"];
    var bot = new TelegramBot(token);
    await bot.SetWebhookAsync($"{builder.Configuration["Host"]}/telegram");
    return bot;
}