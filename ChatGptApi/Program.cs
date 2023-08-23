global using Microsoft.AspNetCore.Mvc;
global using Node.Tasks.Models;
global using NodeCommon;
global using NodeCommon.Tasks;
global using ILogger = Microsoft.Extensions.Logging.ILogger;
using ChatGptApi;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Vision.V1;
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

builder.Services.AddSingleton<OpenAICompleter>();
builder.Services.AddSingleton(new ImageAnnotatorClientBuilder()
{
    GoogleCredential = GoogleCredential.FromFile("gcredentials.json"),
    // GrpcAdapter = RestGrpcAdapter.Default
}.Build());

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else app.UseHttpsRedirection();

app.MapControllers();
app.Run();