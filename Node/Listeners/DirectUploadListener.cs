using System.Net;
using Microsoft.AspNetCore.WebUtilities;
using Node.Tasks;

namespace Node.Listeners;

public class DirectUploadListener : MultipartListenerBase
{
    protected override ListenTypes ListenType => ListenTypes.Public;
    protected override string? Prefix => "rphtaskexec/uploadinput";

    protected override async ValueTask<HttpStatusCode> Execute(HttpListenerContext context, CachedMultipartReader reader)
    {
        var sections = await reader.GetSectionsAsync();
        var taskid = await sections["taskid"].ReadAsStringAsync();
        NodeSettings.QueuedTasks.TryGetValue(taskid, out var task);

        if (task is null || task.Input is not DirectDownloadTaskInputInfo dinput)
            return await WriteErr(context.Response, "No task found with such id");

        if (dinput.Downloaded)
            return await WriteErr(context.Response, "Input files were already uploaded");

        var file = sections["file"];
        var format = FileFormatExtensions.FromMime(file.Headers["Content-Type"]);
        var last = (await (sections.GetValueOrDefault("last")?.ReadAsStringAsync() ?? Task.FromResult("0"))) == "1";

        // if there is only one input file, check length
        if (task.InputFiles.Count == 0 && last)
        {
            var length = long.Parse(file.Headers["Content-Length"]);
            if (Math.Abs(task.Info.Object.Size - length) > 1024 * 1024)
                return await WriteErr(context.Response, "Invalid input file length");
        }

        using var _ = file.Body;

        var filename = task.FSNewInputFile(format, GetQueryPart(file.Headers["Content-Disposition"], "filename"));
        using (var resultfile = File.OpenWrite(filename))
            await file.Body.CopyToAsync(resultfile);

        if (last)
        {
            var check = task.GetAction().InputRequirements.Check(task);
            if (!check) return await WriteJson(context.Response, check);

            dinput.Downloaded = true;
        }

        return await WriteSuccess(context.Response);
    }
}
