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


await Init.For(new Init.InitConfig("renderfin")).ExecuteAsync();
await Node.Tests.LocalTests.RunAsync();