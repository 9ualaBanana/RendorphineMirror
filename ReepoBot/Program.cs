using NLog.Web;
using ReepoBot.Services.GitHub;
using ReepoBot.Services.Tasks;
using ReepoBot.Services.Telegram;
using ReepoBot.Services.Telegram.Authentication;
using ReepoBot.Services.Telegram.Updates;
using ReepoBot.Services.Telegram.Updates.Commands;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Host.UseNLog();

// Telegram.Bot works only with Newtonsoft.
builder.Services.AddControllers().AddNewtonsoftJson();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpClient();
builder.Services.AddSwaggerGen();
builder.Services.AddTelegramUpdateHandlers().AddTelegramBotCommands();

Task<TelegramBot> botInitialization = TelegramBot.Initialize(
    builder.Configuration["BotToken"], builder.Configuration["Host"]);

builder.Services.AddSingleton<TelegramChatIdAuthentication>();
builder.Services.AddScoped<TaskResultsPreviewer>();
builder.Services.AddScoped<GitHubEventForwarder>();
builder.Services.AddSingleton(await botInitialization);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run(builder.Configuration["HostListener"]);
