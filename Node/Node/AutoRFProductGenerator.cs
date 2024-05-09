namespace Node;

public class AutoRFProductGenerator
{
    public required IRFProductStorage RFProducts { get; init; }
    public required RFProduct.Factory RFProductFactory { get; init; }
    public required SettingsInstance Settings { get; init; }
    public required DataDirs Dirs { get; init; }
    public required ILogger<AutoRFProductGenerator> Logger { get; init; }

    public AutoRFProductGenerator() => StartThread(ProcessOnce);

    void StartThread(Func<Task> action)
    {
        new Thread(async () =>
        {
            while (true)
            {
                await Task.Delay(5000);

                try { await action(); }
                catch (Exception ex) { Logger.LogError(ex, ""); }
            }
        })
        {
            Name = "AutoRFProductGenerator",
            IsBackground = true,
        }.Start();
    }
    async Task ProcessOnce()
    {
        var dirs = Settings.RFProductSourceDirectories.Value;
        if (dirs.Length == 0)
            Settings.RFProductSourceDirectories.Value = [Dirs.DataDir("autorfp")];

        foreach (var dir in dirs)
            await GenerateRFProducts(dir, default);
    }

    async Task GenerateRFProducts(string directory, CancellationToken token)
    {
        if (!Directory.Exists(directory)) return;

        foreach (var productDir in Directory.GetDirectories(directory))
        {
            token.ThrowIfCancellationRequested();
            if (File.Exists(Path.Combine(productDir, ".rfproducted"))) continue;

            try
            {
                Logger.Info("Creating product " + productDir);
                RFProduct rfp;
                try
                {
                    rfp = await RFProductFactory.CreateAsync(productDir, productDir, token, false);
                }
                catch
                {
                    foreach (var assetsdir in Directory.GetDirectories(productDir, "rfp-assets", SearchOption.AllDirectories))
                        Directory.Delete(assetsdir, true);

                    foreach (var file in Directory.GetFiles(productDir, "idea.*", SearchOption.AllDirectories))
                        File.Delete(file);

                    rfp = await RFProductFactory.CreateAsync(productDir, productDir, token, false);
                }

                Logger.Info($"Auto-created rfproduct {rfp.ID} @ {rfp.Idea.Path}");
                File.Create(Path.Combine(productDir, ".rfproducted")).Dispose();
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Logger.Error(ex);
            }
        }
    }
}
