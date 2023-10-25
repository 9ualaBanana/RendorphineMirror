global using Node.Common;
global using Node.Plugins;
global using Node.Plugins.Models;
global using Node.Tasks;
global using Node.Tasks.Exec;
global using Node.Tasks.Exec.Actions;
global using Node.Tasks.Exec.FFmpeg;
global using Node.Tasks.Exec.FFmpeg.Codecs;
global using Node.Tasks.Models;
global using Node.Tasks.Models.ExecInfo;
global using NodeCommon;
using Node.Tests;


var builder = Init.CreateContainer(new Init.InitConfig("renderfin_tests"));
builder.RegisterType<LocalTests>()
    .SingleInstance();

using var container = builder.Build();

await container.Resolve<LocalTests>()
    .Run();
