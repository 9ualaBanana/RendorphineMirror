global using Common;
global using Microsoft.AspNetCore.Authorization;
global using Microsoft.AspNetCore.Mvc;
global using MonoTorrent;
global using Node.Common;
global using Node.Plugins.Models;
global using NodeCommon;
global using System.Collections.Immutable;
global using System.Diagnostics.CodeAnalysis;
global using ILogger = Microsoft.Extensions.Logging.ILogger;
using NLog.Web;
using SoftwareRegistry;

Initializer.AppName = "renderfin_registry2";
Init.Initialize();


var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Host.UseNLog();
builder.Services.AddControllers().AddNewtonsoftJson();
builder.Services.AddSingleton(new TorrentClient(6229, 6230));
builder.Services.AddSingleton<SoftList>();
builder.Services.AddSingleton<TorrentManager>();

var app = builder.Build();

var client = app.Services.GetRequiredService<TorrentClient>();
app.Services.GetRequiredService<ILogger<Program>>()
    .LogInformation("Torrent listening at dht" + client.DhtPort + " and trt" + client.ListenPort);

await app.Services.GetRequiredService<TorrentManager>()
    .AddFromMainDirectoryAsync();


app.MapControllers();
app.Run();