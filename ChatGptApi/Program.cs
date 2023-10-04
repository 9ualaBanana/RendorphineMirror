global using Microsoft.AspNetCore.Mvc;
global using Node.Common;
global using Node.Tasks.Models;
global using NodeCommon;
global using NodeCommon.Tasks;
global using ILogger = Microsoft.Extensions.Logging.ILogger;
using Autofac.Extensions.DependencyInjection;
using ChatGptApi;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Vision.V1;


var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSystemd();
builder.Logging.ClearProviders();
builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory(builder =>
    Init.InitializeContainer(builder, new Init.InitConfig("renderfin_chatgptapi"), typeof(Program).Assembly)
));

builder.Services.AddControllers().AddNewtonsoftJson();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<HttpClient>();
builder.Services.AddSingleton<OpenAICompleter>();
builder.Services.AddSingleton<ElevenLabsApi>();
builder.Services.AddSingleton(
    ctx => new ElevenLabsApis(ctx.GetRequiredService<IConfiguration>().GetValue<string>("elevenlabs_apikey").ThrowIfNull(), ctx.GetRequiredService<ElevenLabsApi>())
);

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
