global using Microsoft.AspNetCore.Mvc;
global using Node.Common;
global using Node.Tasks.Models;
global using NodeCommon;
global using NodeCommon.Tasks;
global using ILogger = Microsoft.Extensions.Logging.ILogger;
using ChatGptApi;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Vision.V1;
using NLog.Web;


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

builder.Services.AddSingleton(new Init.InitConfig("renderfin_chatgptapi"));
builder.Services.AddSingleton<Init>();

var app = builder.Build();
app.Services.GetRequiredService<Init>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else app.UseHttpsRedirection();

app.MapControllers();
app.Run();