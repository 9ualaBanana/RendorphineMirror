using System.IO.Compression;
using System.Net.Http.Json;
using System.Text.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Node.Tasks.Models;

public static class NodeTask
{
    public static async Task<string> RegisterAsync(JObject data, TaskObject taskobj, JObject input, JObject output, RequestOptions? requestOptions = null)
    {
        requestOptions ??= new();

        var httpContent = new MultipartFormDataContent()
        {
            { new StringContent(Settings.SessionId!), "sessionid" },
            { JsonContent.Create(taskobj, options: new(JsonSerializerDefaults.Web) { PropertyNamingPolicy = LowercaseNamingPolicy.Instance }), "object" },
            // Cast to object is necessary to allow serialization of properties of derived classes.
            { new StringContent(input.ToString(Formatting.None)), "input" },
            { new StringContent(output.ToString(Formatting.None)), "output" },
            { new StringContent(data.ToString(Formatting.None)), "data" },
            { new StringContent(string.Empty), "origin" }
        };

        Log.Information($"Registering task: {JsonConvert.SerializeObject(info)}");
        var response = await Api.TryPostAsync(
            $"{Api.TaskManagerEndpoint}/registermytask", httpContent, requestOptions)
            .ConfigureAwait(false);
        var jsonResponse = await response.Content.ReadAsStringAsync(requestOptions.CancellationToken).ConfigureAwait(false);

        var id = JsonDocument.Parse(jsonResponse).RootElement.GetProperty("taskid").GetString()!;
        Log.Information($"Task registered with ID {id}");

        return id;
    }

    public static string ZipFiles(IEnumerable<string> files)
    {
        var directoryName = Path.Combine(Path.GetTempPath(), "renderphine_temp");
        Directory.CreateDirectory(directoryName);
        var archiveName = Path.Combine(directoryName, Guid.NewGuid().ToString() + ".zip");


        using var archivefile = File.OpenWrite(archiveName);
        using var archive = new ZipArchive(archivefile, ZipArchiveMode.Create);

        foreach (var file in files)
            archive.CreateEntryFromFile(file, Path.GetFileName(file));

        return archiveName;
    }
    public static IEnumerable<string> UnzipFiles(string zipfile)
    {
        var directoryName = Path.Combine(Path.GetTempPath(), "renderphine_temp", Guid.NewGuid().ToString());
        Directory.CreateDirectory(directoryName);

        using var archivefile = File.OpenRead(zipfile);
        using var archive = new ZipArchive(archivefile, ZipArchiveMode.Read);

        foreach (var entry in archive.Entries)
        {
            var path = Path.Combine(directoryName, entry.FullName);
            entry.ExtractToFile(path, true);

            yield return path;
        }
    }


    class LowercaseNamingPolicy : JsonNamingPolicy
    {
        public static LowercaseNamingPolicy Instance = new();

        private LowercaseNamingPolicy() { }

        public override string ConvertName(string name) => name.ToLowerInvariant();
    }
}