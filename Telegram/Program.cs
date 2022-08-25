using NLog.Web;
using Telegram.Services.GitHub;
using Telegram.Services.Tasks;
using Telegram.Services.Telegram;
using Telegram.Services.Telegram.Authentication;
using Telegram.Services.Telegram.Updates;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Host.UseNLog();

// Telegram.Bot works only with Newtonsoft.
builder.Services.AddControllers().AddNewtonsoftJson();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpClient();
builder.Services.AddSwaggerGen();
builder.Services.AddTelegramUpdateHandlers();

Task<TelegramBot> botInitialization = TelegramBot.Initialize(
    builder.Configuration["BotToken"], builder.Configuration["Host"]);

builder.Services.AddSingleton<TelegramChatIdAuthentication>();
builder.Services.AddScoped<TaskResultsPreviewer>();
builder.Services.AddScoped<GitHubEventForwarder>();
builder.Services.AddSingleton(await botInitialization);

var app = builder.Build();

app.Services.GetRequiredService<TelegramBot>().UseLoggerFrom(app.Services);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run(builder.Configuration["HostListener"]);