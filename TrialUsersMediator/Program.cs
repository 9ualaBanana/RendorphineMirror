using Microsoft.Extensions.DependencyInjection.Extensions;
using Telegram.MPlus;
using TrialUsersMediator;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<TrialUsersDbContext>();
builder.Services.AddMPlusClient();
builder.Services.TryAddSingleton<TrialUser.Identity>();
builder.Services.AddScoped<TrialUser.MediatorClient>();

builder.Services.AddControllers();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.UseSwagger().UseSwaggerUI();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

using (var scope = app.Services.CreateScope())
    await scope.ServiceProvider.GetRequiredService<TrialUser.MediatorClient>().InitializeAsync();

app.Run("https://localhost:7000");
