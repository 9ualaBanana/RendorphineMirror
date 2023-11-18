global using Node.Plugins;
global using Node.Plugins.Models;
global using Node.Tasks;
global using Node.Tasks.Exec;
global using Node.Tasks.Models;
global using Node.Tasks.Models.ExecInfo;
using Node.Common;
using Node.Tests;


var builder = Init.CreateContainer(new Init.InitConfig("renderfin_tests"));
builder.RegisterType<LocalTests>()
    .SingleInstance();

using var container = builder.Build();

await container.Resolve<LocalTests>()
    .Run();
