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

        foreach (var product in Directory.GetDirectories(directory).Concat(Directory.GetFiles(directory)).Select(Path.GetFullPath))
        {
            var isdir = Directory.Exists(product);

            token.ThrowIfCancellationRequested();
            if (File.Exists(Path.Combine(product, ".rfproducted"))) continue;
            if (RFProducts.RFProducts.Any(p => p.Value.Idea.Path == product)) continue;

            var container = isdir ? product : Path.ChangeExtension(product, null);
            {
                var origc = container;
                var num = 0;

                while (Directory.Exists(container))
                    container = origc + num;
            }

            async Task<RFProduct> create()
            {
                Logger.Info($"Auto-creating rfp {product} @ {container}");
                var rfp = await RFProductFactory.CreateAsync(product, container, token, false);

                if (!isdir)
                {
                    Logger.Info($"Deleting original file {product}");
                    File.Delete(product);
                }

                return rfp;
            }

            try
            {
                Logger.Info("Creating product " + product);
                RFProduct rfp;
                try
                {
                    rfp = await create();
                }
                catch
                {
                    if (isdir)
                    {
                        foreach (var assetsdir in Directory.GetDirectories(product, "rfp-assets", SearchOption.AllDirectories))
                            Directory.Delete(assetsdir, true);

                        foreach (var file in Directory.GetFiles(product, "idea.*", SearchOption.AllDirectories))
                            File.Delete(file);
                    }

                    rfp = await create();
                }

                Logger.Info($"Auto-created rfproduct {rfp.ID} @ {rfp.Idea.Path}");
                File.Create(Path.Combine(container, ".rfproducted")).Dispose();
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Logger.Error(ex);
            }
        }
    }
}
