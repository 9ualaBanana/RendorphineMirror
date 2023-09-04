namespace Node.Tasks.Exec;

public record ImageDetectorRect(int X, int Y, int W, int H);
public class ImageDetectorLauncher
{
    public required PluginList Plugins { get; init; }
    public required ILogger<ImageDetectorLauncher> Logger { get; init; }

    public async Task<ImageDetectorRect> GenerateRectAsync(string file)
    {
        var script = $"python main.py -i \"{file}\"";
        var result = null as ImageDetectorRect;
        await CondaInvoker.ExecutePowerShellAtWithCondaEnvAsync(Plugins, PluginType.ImageDetector, script, onread, Logger);

        return result.ThrowIfNull("Rect could not be calculated");


        void onread(bool err, object data)
        {
            if (err) throw new Exception(data?.ToString());

            var str = data.ToString();
            if (str is null || !str.StartsWith("Result: ", StringComparison.Ordinal)) return;

            result = JsonConvert.DeserializeObject<ImageDetectorRect>(str.Substring("Result: ".Length).Replace("'", "\""));
        }
    }
}
