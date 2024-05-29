using Microsoft.EntityFrameworkCore;
using StatusNotifier;
using Telegram.Bot;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(ctx => new TelegramBotClient(builder.Configuration.Get<Config>().ThrowIfNull().TelegramApiKey));
builder.Services.AddControllers();
builder.Services.AddRazorPages();
builder.Services.AddDbContext<NotificationDbContext>(opt =>
   opt.UseSqlite("Data Source=notifications.db")
);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

app.MapRazorPages();
app.MapControllers();
await app.StartAsync();

using (var scope = app.Services.CreateScope())
using (var context = scope.ServiceProvider.GetRequiredService<NotificationDbContext>())
    context.Database.Migrate();

await app.WaitForShutdownAsync();

record Config(string TelegramApiKey);
