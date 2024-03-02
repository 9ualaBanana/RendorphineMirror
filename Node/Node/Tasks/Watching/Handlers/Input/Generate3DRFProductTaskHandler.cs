using System.Net;
using System.Text.RegularExpressions;
using _3DProductsPublish.Turbosquid;
using _3DProductsPublish.Turbosquid.Upload;

namespace Node.Tasks.Watching.Handlers.Input;

public class Generate3DRFProductTaskHandler : WatchingTaskInputHandler<Generate3DRFProductTaskInputInfo>, ITypedTaskWatchingInput
{
    public static WatchingTaskInputType Type => WatchingTaskInputType.Generate3DRFProduct;

    public required RFProduct.Factory RFProductFactory { get; init; }
    public required IRFProductStorage RFProducts { get; init; }
    public required INodeGui NodeGui { get; init; }
    public required TurboSquidContainer TurboSquidContainer { get; init; }
    public required NodeGlobalState GlobalState { get; init; }
    public required INodeSettings Settings { get; init; }

    public override void StartListening() => StartThreadRepeated(10_000, RunOnce);

    void SetState(Func<AutoRFProductPublishInfo, AutoRFProductPublishInfo> editfunc)
    {
        if (!GlobalState.AutoRFProductPublishInfos.Value.TryGetValue(Task.Id, out var state))
        {
            state = new AutoRFProductPublishInfo()
            {
                IsPaused = Task.IsPaused,
                InputDirectory = Input.InputDirectory,
            };
        }

        state = editfunc(state);
        GlobalState.AutoRFProductPublishInfos[Task.Id] = state;
    }

    public async Task RunOnce()
    {
        SetState(state =>
        {
            var products = RFProducts.RFProducts.Values
                .Where(p => p.Type == nameof(RFProduct._3D) && p.Path.StartsWith(Path.GetFullPath(Input.InputDirectory)))
                .ToArray();

            return state with
            {
                FileCount = Directory.GetDirectories(Input.InputDirectory).Length,
                CurrentPublishing = null,
                CurrentRFProducting = null,
                RFProductedCount = products.Length,
            };
        });

        try
        {
            await GenerateQSProducts();
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
        }
        finally
        {
            SetState(state => state with { CurrentPublishing = null, CurrentRFProducting = null });
        }

        try
        {
            await PublishRFProducts(default);
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
        }
        finally
        {
            SetState(state => state with { CurrentPublishing = null, CurrentRFProducting = null });
        }
    }

    async Task GenerateQSProducts()
    {
        foreach (var productDir in Directory.GetDirectories(Input.InputDirectory))
        {
            if (File.Exists(Path.Combine(productDir, ".rfproducted"))) continue;

            try
            {
                SetState(state => state with { CurrentRFProducting = productDir });

                Logger.Info("Creating product " + productDir);
                var rfp = await RFProductFactory.CreateAsync(productDir, productDir, default, false);
                Logger.Info($"Auto-created rfproduct {rfp.ID} @ {rfp.Idea.Path}");
                File.Create(Path.Combine(productDir, ".rfproducted")).Dispose();

                SetState(state => state with { RFProductedCount = state.RFProductedCount + 1 });

                try { File.Delete(Path.Combine(productDir, "publish_exception.txt")); }
                catch { }
            }
            catch (Exception ex)
            {
                SetState(state => state with { Error = $"Error RFPRODUCTING product {productDir}:\n{ex}", });

                Logger.Error(ex);
                File.WriteAllText(Path.Combine(productDir, "publish_exception.txt"), ex.ToString());
            }
        }
    }

    static JObject ReadSubmitJson(string dir) => JObject.Parse(File.ReadAllText(Directory.GetFiles(dir).Single(f => f.EndsWith("_Submit.json"))));
    bool IsSubmittedTurboSquid(string dir)
    {
        if (!File.Exists(Path.Combine(dir, "turbosquid.meta"))) return false;

        var changed = WasDirectoryChanged(dir, out var newdata);
        if (changed is not null)
        {
            Logger.Info(changed + " CHANGED, reWOUDING");
            (Input.DirectoryStructure ??= [])[dir] = newdata;
            SaveTask();
            return false;
        }

        var firstline = File.ReadLines(Path.Combine(dir, "turbosquid.meta")).FirstOrDefault();
        return firstline is not null && Regex.IsMatch(firstline, @"\[\d+\]");
    }
    async Task PublishRFProducts(CancellationToken token)
    {
        var products = RFProducts.RFProducts.Values
            .Where(p => p.Type == nameof(RFProduct._3D) && p.Path.StartsWith(Path.GetFullPath(Input.InputDirectory)))
            .ToArray();

        SetState(state => state with { PublishedCount = 0 });

        foreach (var rfpgroup in products.GroupBy(r => ReadSubmitJson(r.Idea.Path)["LoginSquid"]!.Value<string>().ThrowIfNull()))
        {
            async Task updateSalesIfNeeded()
            {
                if (Input.LastSalesFetch.AddHours(1) > DateTimeOffset.Now)
                    return;

                //if (Settings.MPlusUsername is null || Settings.MPlusPassword is null)

                //    return;

                //var mpcreds = new NetworkCredential(Settings.MPlusUsername, Settings.MPlusUsername);

                var submitJson = ReadSubmitJson(rfpgroup.First().Idea.Path);
                var tsusername = submitJson["LoginSquid"]!.ToObject<string>().ThrowIfNull();
                var tspassword = submitJson["PasswordSquid"]!.ToObject<string>().ThrowIfNull();

                var turbo = await TurboSquidContainer.GetAsync(tsusername, tspassword, token);

                var sales = await (await turbo.SaleReports).ScanAsync(token).ToArrayAsync(token);
                foreach (var product in rfpgroup)
                    await product.UpdateSalesAsync(sales.SelectMany(s => s.SaleReports).ToArray(), token);

                //await (await MPAnalytics.LoginAsync(mpcreds, token)).SendAsync(sales.ToAsyncEnumerable(), token);
            }

            await updateSalesIfNeeded();


            foreach (var rfproduct in rfpgroup)
            {
                try
                {
                    var submitJson = ReadSubmitJson(rfproduct.Idea.Path);
                    SetState(state => state with { CurrentPublishing = rfproduct.Path });

                    var username = submitJson["LoginSquid"]!.ToObject<string>().ThrowIfNull();
                    var password = submitJson["PasswordSquid"]!.ToObject<string>().ThrowIfNull();

                    Logger.Info($"Publishing to turbosquid: {rfproduct.Path}");

                    try
                    {
                        var turbo = await TurboSquidContainer.GetAsync(username, password, token);
                        await turbo.PublishAsync(rfproduct, NodeGui, token);
                    }
                    catch (Exception ex) when (ex is TaskCanceledException or OperationCanceledException)
                    {
                        Logger.Info($"Username {rfpgroup.Key} cancelled login, skipping.");
                        break;
                    }

                    try { File.Delete(Path.Combine(rfproduct.Idea.Path, "publish_exception.txt")); }
                    catch { }
                }
                catch (Exception ex)
                {
                    SetState(state => state with { Error = $"Error PUBLISHING product {rfproduct.Path}:\n{ex}", });

                    Logger.Error(ex);
                    File.WriteAllText(Path.Combine(rfproduct.Idea.Path, "publish_exception.txt"), ex.ToString());
                }

                /*
                if ((submitJson["toSubmitTrader"]?.ToObject<ToSubmit>() ?? ToSubmit.None) == ToSubmit.Submit)
                {
                    var username = submitJson["LoginCGTrader"]!.ToObject<string>().ThrowIfNull();
                    var password = submitJson["PasswordCGTrader"]!.ToObject<string>().ThrowIfNull();
                }
                */

                SetState(state => state with { PublishedCount = state.PublishedCount + 1 });
                (Input.DirectoryStructure ??= [])[rfproduct.Idea.Path] = GetDirectoryData(rfproduct.Idea.Path);
                SaveTask();
            }
        }

        Input.LastSalesFetch = DateTimeOffset.Now;
    }

    string? WasDirectoryChanged(string directory, out Dictionary<string, DirectoryStructurePart> data)
    {
        if (Input.DirectoryStructure is null || !Input.DirectoryStructure.TryGetValue(directory, out var prevdirstr))
        {
            data = GetDirectoryData(directory);
            return null;
        }

        data = GetDirectoryData(directory);
        foreach (var (path, _) in data)
        {
            if (path.Contains("_Submit.")) continue;
            if (path.Contains("_Status.")) continue;
            if (path.Contains("turbosquid.meta")) continue;

            if (!Input.DirectoryStructure.TryGetValue(directory, out var dirstr))
                return "dirstructure no get value ";

            if (!dirstr.ContainsKey(path))
                return "dirstr no get value";
        }

        foreach (var (path, info) in prevdirstr)
        {
            // submit file and turbosquid.meta are being updated so exclude them from the check
            if (path.Contains("_Submit.")) continue;
            if (path.Contains("_Status.")) continue;
            if (path.Contains("turbosquid.meta")) continue;

            if (File.Exists(path))
            {
                if (new FileInfo(path).Length != info.Size)
                    return path + " length not equals";
                if (new DateTimeOffset(new FileInfo(path).LastWriteTimeUtc) != info.LastChanged)
                    return path + " last write not equals";
            }
            else if (Directory.Exists(path))
            {
                if (new DateTimeOffset(new DirectoryInfo(path).LastWriteTimeUtc) != info.LastChanged)
                    return path + " dir last write not equals";
            }
            else return path + " not exists";
        }

        return null;
    }
    Dictionary<string, DirectoryStructurePart> GetDirectoryData(string directory)
    {
        var dirs = new Dictionary<string, DirectoryStructurePart>();

        foreach (var file in Directory.GetFiles(Path.Combine(Input.InputDirectory, directory), "*", SearchOption.AllDirectories))
            dirs[file] = new(new DateTimeOffset(new FileInfo(file).LastWriteTimeUtc), new FileInfo(file).Length);
        foreach (var dir in Directory.GetDirectories(Path.Combine(Input.InputDirectory, directory), "*", SearchOption.AllDirectories))
            dirs[dir] = new(new DateTimeOffset(new DirectoryInfo(dir).LastWriteTimeUtc), null);

        return dirs;
    }
}
