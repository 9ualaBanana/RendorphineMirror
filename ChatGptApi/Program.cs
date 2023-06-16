global using Microsoft.AspNetCore.Mvc;
global using ILogger = Microsoft.Extensions.Logging.ILogger;
using NLog.Web;


Initializer.AppName = "renderfin_chatgptapi";
Init.Initialize();


var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSystemd();

builder.Logging.ClearProviders();
builder.Host.UseNLog();

builder.Services.AddControllers().AddNewtonsoftJson();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();