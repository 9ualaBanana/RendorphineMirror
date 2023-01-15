global using Common;
using NLog.Web;
using Telegram.Bot;
using Telegram.Services.GitHub;
using Telegram.Telegram;
using Telegram.Telegram.Authentication.Services;
using Telegram.Telegram.Updates;
using Telegram.Telegram.Updates.Tasks.ResultsPreview.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseDefaultServiceProvider(o => o.ValidateScopes = false);

builder.Logging.ClearProviders();
builder.Host.UseNLog();

builder.Services.AddTelegramBotUsing(builder.Configuration);

// Telegram.Bot works only with Newtonsoft.
builder.Services.AddControllers().AddNewtonsoftJson();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpClient();
builder.Services.AddSwaggerGen();
builder.Services.AddTelegramUpdateHandlers();

builder.Services.AddScoped<ChatAuthenticator>().AddDbContext<AuthenticatedUsersDbContext>();
builder.Services.AddScoped<TaskResultsPreviewer>();
builder.Services.AddScoped<GitHubEventForwarder>();

var app = builder.Build();

await app.Services.GetRequiredService<TelegramBot>().InitializeAsync();
    //.UseMessageChunkerFrom(app.Services);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();
