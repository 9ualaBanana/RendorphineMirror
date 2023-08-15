global using Node.Plugins;
global using Node.Plugins.Models;
global using Node.Tasks;
global using Node.Tasks.Exec;
global using Node.Tasks.Models;


Initializer.AppName = "renderfin";
Init.Initialize();

await Node.Tests.LocalTests.RunAsync();