using System.Text.RegularExpressions;
using _3DProductsPublish.Turbosquid;
using _3DProductsPublish.Turbosquid.Upload;
using Node.Common.Models.GuiRequests;

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
    CancellationTokenSource? CurrentTask;

    public override void StartListening() => StartThreadRepeated(10_000, RunOnce);

    void SetState(Func<AutoRFProductPublishInfo, AutoRFProductPublishInfo> editfunc)
    {
        if (!GlobalState.AutoRFProductPublishInfos.Value.TryGetValue(Task.Id, out var state))
        {
            state = new AutoRFProductPublishInfo()
            {
                TaskId = Task.Id,
                IsPaused = Task.IsPaused,
                InputDirectory = Input.InputDirectory,
            };
        }

        state = editfunc(state);
        GlobalState.AutoRFProductPublishInfos[Task.Id] = state;
    }

    public void FullRestart()
    {
        CurrentTask?.Cancel();
        TurboSquidContainer.ClearCache();
        Input.DirectoryStructure2?.Clear();
        SaveTask();
    }

    public async Task RunOnce()
    {
        CurrentTask = new CancellationTokenSource();

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
            Logger.Info("fgenerating ");
            await GenerateQSProducts(CurrentTask.Token);
            Logger.Info("gen end ");
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
            Logger.Info("publishing ");
            await PublishRFProducts(CurrentTask.Token);
            Logger.Info("publish end ");

        }
        catch (Exception ex)
        {
            Logger.Error(ex);
        }
        finally
        {
            SetState(state => state with { CurrentPublishing = null, CurrentRFProducting = null });
            CurrentTask = null;
        }
    }

    async Task GenerateQSProducts(CancellationToken token)
    {
        foreach (var productDir in Directory.GetDirectories(Input.InputDirectory))
        {
            token.ThrowIfCancellationRequested();
            if (File.Exists(Path.Combine(productDir, ".rfproducted"))) continue;

            try
            {
                SetState(state => state with { CurrentRFProducting = productDir });

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

                SetState(state => state with { RFProductedCount = state.RFProductedCount + 1 });

                try { File.Delete(Path.Combine(productDir, "publish_exception.txt")); }
                catch { }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                SetState(state => state with { Error = $"{DateTimeOffset.Now} Error RFPRODUCTING product {productDir}:\n{ex}", });

                Logger.Error(ex);
                File.WriteAllText(Path.Combine(productDir, "publish_exception.txt"), ex.ToString());
            }
        }
    }

    static RFProduct._3D.Status? GetStatus(string productDir)
    {
        var stateFile = Directory.GetFiles(productDir).FirstOrDefault(d => d.Contains("_Status.json"));
        if (stateFile is null) return null;

        return JObject.Parse(stateFile)?["status"]?.ToObject<RFProduct._3D.Status>();
    }

    void BumpSubmitJsonVersion(RFProduct rfproduct)
    {
        var submitJsonPath = GetSubmitJsonFile(rfproduct.Idea.Path);
        JObject jobj;
        try { jobj = JObject.Parse(File.ReadAllText(submitJsonPath)); }
        catch { jobj = new(); }

        var newversion = (jobj["Version"]?.ToObject<int>() ?? 0) + 1;
        jobj["Version"] = newversion;
        File.WriteAllText(submitJsonPath, jobj.ToString(Formatting.None));

        Logger.Info($"Updating {rfproduct.Idea.Path} version to {newversion}");
        (Input.DirectoryStructure2 ??= [])[rfproduct.Idea.Path] = new(true, GetDirectoryData(rfproduct.Idea.Path));
    }

    static string GetSubmitJsonFile(string dir) => Directory.GetFiles(dir).Single(f => f.EndsWith("_Submit.json", StringComparison.Ordinal));
    static JObject ReadSubmitJson(string dir) => JObject.Parse(File.ReadAllText(GetSubmitJsonFile(dir)));
    bool NeedsTurboSquidPublish(RFProduct rfproduct)
    {
        if (!File.Exists(Path.Combine(rfproduct.Idea.Path, "meta.json")))
            return true;

        if (Input.DirectoryStructure2?.GetValueOrDefault(rfproduct.Idea.Path) is null)
        {
            BumpSubmitJsonVersion(rfproduct);
            return true;
        }
        if (Input.DirectoryStructure2[rfproduct.Idea.Path].NeedsUploading)
            return true;

        var changed = WasDirectoryChanged(rfproduct.Idea.Path, out _);
        if (changed is not null)
        {
            BumpSubmitJsonVersion(rfproduct);
            Logger.Info(changed + " CHANGED, reWOUDING");
            return true;
        }

        var submitStatus = ((RFProduct._3D.Idea_) rfproduct.Idea).Status;
        if (submitStatus == RFProduct._3D.Status.none)
        {
            Logger.Info(submitStatus + " submit status, reWOUDING");
            return true;
        }

        return false;
    }
    async Task<TurboSquid> GetTurboRetryOrSkip(string username, string password, CancellationToken token, bool force = false)
    {
        try
        {
            if (force)
                return await TurboSquidContainer.ForceGetAsync(username, password, token);
            return await TurboSquidContainer.GetAsync(username, password, token);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            var retry = await NodeGui.Request<bool>(new RetryOrSkipRequest("Login failed. Retry?"), token);
            if (!retry || !retry.Value)
                throw new OperationCanceledException();

            return await GetTurboRetryOrSkip(username, password, token, true);
        }
    }
    async Task PublishRFProducts(CancellationToken token)
    {
        foreach (var product in RFProducts.RFProducts.Values.ToArray())
        {
            if (product.Type != nameof(RFProduct._3D))
                continue;

            if (!product.Path.StartsWith(Path.GetFullPath(Input.InputDirectory)))
                continue;

            if (Directory.Exists(product.Path))
                continue;

            RFProducts.RFProducts.Remove(product.ID);
        }

        Logger.Info("IN PUBLISH");
        var products = RFProducts.RFProducts.Values
            .Where(p => Directory.Exists(p.Path) && p.Type == nameof(RFProduct._3D) && p.Path.StartsWith(Path.GetFullPath(Input.InputDirectory)))
            .ToArray();

        SetState(state => state with { DraftedCount = 0, PublishedCount = 0 });

        foreach (var rfpgroup in products.GroupBy(r => ReadSubmitJson(r.Idea.Path)["LoginSquid"]?.ToObject<string>() ?? string.Empty))
        {
            if (string.IsNullOrWhiteSpace(rfpgroup.Key))
                continue;

            Logger.Info("INGROUP " + rfpgroup.Key);
            token.ThrowIfCancellationRequested();

            TurboSquid turbo;
            {
                var path = rfpgroup.FirstOrDefault(g => !string.IsNullOrWhiteSpace(ReadSubmitJson(g)["LoginSquid"]?.ToObject<string>()) && !string.IsNullOrEmpty(ReadSubmitJson(g)["PasswordSquid"]?.ToObject<string>()))?.Idea.Path;
                if (path is null) continue;

                var submitJson = ReadSubmitJson(path);
                var tsusername = submitJson["LoginSquid"]!.ToObject<string>().ThrowIfNull();
                var tspassword = submitJson["PasswordSquid"]!.ToObject<string>().ThrowIfNull();

                try
                {
                    turbo = await GetTurboRetryOrSkip(tsusername, tspassword, token);
                    Logger.Info("logged in");
                }
                catch (OperationCanceledException)
                {
                    Logger.Info($"Username {rfpgroup.Key} cancelled login, skipping.");
                    break;
                }
            }

            async Task updateSalesIfNeeded()
            {
                if (Input.LastSalesFetch.AddHours(1) > DateTimeOffset.Now)
                    return;

                //if (Settings.MPlusUsername is null || Settings.MPlusPassword is null)
                //    return;

                //var mpcreds = new NetworkCredential(Settings.MPlusUsername, Settings.MPlusUsername);

                Logger.Info("Getting turbo squid sales");
                var sales = await turbo.SaleReports.ScanAsync(token).ToArrayAsync(token);

                foreach (var product in rfpgroup)
                    await product.UpdateSalesAsync(sales.SelectMany(s => s.SaleReports).ToArray(), token);

                //await (await MPAnalytics.LoginAsync(mpcreds, token)).SendAsync(sales.ToAsyncEnumerable(), token);
            }

            Logger.Info("beforesles");
            System.Threading.Tasks.Task.Run(async () =>
            {
                try { await updateSalesIfNeeded(); }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                    // SetState(state => state with { Error = $"{DateTimeOffset.Now} Error UPDATING the sales:\n{ex}", });
                }
            }, token).Consume();

            Logger.Info("beforergfproduct");
            foreach (var rfproduct in rfpgroup)
            {
                Logger.Info("hello " + rfproduct.Idea.Path);
                token.ThrowIfCancellationRequested();
                if (NeedsTurboSquidPublish(rfproduct))
                {
                    Logger.Info("INPRODUCT " + rfproduct.Idea.Path);

                    try
                    {
                        SetState(state => state with { CurrentPublishing = rfproduct.Path });
                        Logger.Info($"Publishing to turbosquid: {rfproduct.Path}");

                        var token2 = CancellationTokenSource.CreateLinkedTokenSource(token, new CancellationTokenSource().Token);
                        token2.CancelAfter(TimeSpan.FromMinutes(20));
                        await turbo.UploadAsync(rfproduct, NodeGui, token2.Token);
                    }
                    catch (Exception ex)
                    {
                        SetState(state => state with { Error = $"{DateTimeOffset.Now} Error PUBLISHING product {rfproduct.Path}:\n{ex}", });

                        Logger.Error(ex);
                        File.WriteAllText(Path.Combine(rfproduct.Idea.Path, "publish_exception.txt"), ex.ToString());
                        continue;
                    }

                    /*
                    if ((submitJson["toSubmitTrader"]?.ToObject<ToSubmit>() ?? ToSubmit.None) == ToSubmit.Submit)
                    {
                        var username = submitJson["LoginCGTrader"]!.ToObject<string>().ThrowIfNull();
                        var password = submitJson["PasswordCGTrader"]!.ToObject<string>().ThrowIfNull();
                    }
                    */
                }

                token.ThrowIfCancellationRequested();

                var submitStatus = ((RFProduct._3D.Idea_) rfproduct.Idea).Status;
                if (submitStatus == RFProduct._3D.Status.draft)
                    SetState(state => state with { DraftedCount = state.DraftedCount + 1 });
                else SetState(state => state with { PublishedCount = state.PublishedCount + 1 });

                (Input.DirectoryStructure2 ??= [])[rfproduct.Idea.Path] = new(false, GetDirectoryData(rfproduct.Idea.Path));
                SaveTask();
            }
        }

        Input.LastSalesFetch = DateTimeOffset.Now;
    }

    string? WasDirectoryChanged(string directory, out Dictionary<string, DirectoryStructurePart> data)
    {
        Logger.Info("Wasdirectorychanged? " + directory);
        if (Input.DirectoryStructure2 is null || !Input.DirectoryStructure2.TryGetValue(directory, out var prevdirstr))
        {
            data = GetDirectoryData(directory);
            return "no item in directorystructure";
        }

        data = GetDirectoryData(directory);
        if (!Input.DirectoryStructure2.TryGetValue(directory, out var dirstr))
            return "dirstructure no get value ";

        foreach (var (path, _) in data)
        {
            if (path.Contains("_Status.")) continue;
            if (path.Contains("meta.json")) continue;
            if (path.Contains("publish_exception")) continue;

            if (!dirstr.Parts.ContainsKey(path))
                return "dirstr no get value";
        }

        foreach (var (path, info) in prevdirstr.Parts)
        {
            // submit file and meta.json are being updated so exclude them from the check
            if (path.Contains("_Status.")) continue;
            if (path.Contains("meta.json")) continue;
            if (path.Contains("publish_exception")) continue;

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
