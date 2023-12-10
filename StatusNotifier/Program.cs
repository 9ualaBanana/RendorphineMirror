using Telegram.Bot;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(ctx => new TelegramBotClient(builder.Configuration.Get<Config>().ThrowIfNull().TelegramApiKey));
builder.Services.AddControllers();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

app.MapControllers();
app.Run();


record Config(string TelegramApiKey);
