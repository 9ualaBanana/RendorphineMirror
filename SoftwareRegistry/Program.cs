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
using Autofac.Extensions.DependencyInjection;
using SoftwareRegistry;



var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory(builder =>
    Init.InitializeContainer(builder, new Init.InitConfig("renderfin_registry3"), typeof(Program).Assembly)
));

builder.Services.AddControllers().AddNewtonsoftJson();
builder.Services.AddSingleton(ctx => new TorrentClient(6231, 6232) { Logger = ctx.GetRequiredService<ILogger<TorrentClient>>() });
builder.Services.AddSingleton<SoftwareList>();

var app = builder.Build();

var client = app.Services.GetRequiredService<TorrentClient>();
app.Services.GetRequiredService<ILogger<Program>>()
    .LogInformation("Torrent listening at dht" + client.DhtPort + " and trt" + client.ListenPort);

await app.Services.GetRequiredService<SoftwareList>()
    .AddPluginsFromDirectory("plugins");

app.MapControllers();
app.Run();
