using System.Net;
using _3DProductsPublish;
using _3DProductsPublish.CGTrader;
using _3DProductsPublish.Turbosquid.Upload;
using Node.Common.Models.GuiRequests;

namespace Node.Tasks.Watching.Handlers.Input;

public class Generate3DRFProductTaskHandler : WatchingTaskInputHandler<Generate3DRFProductTaskInputInfo>, ITypedTaskWatchingInput
{
    public static WatchingTaskInputType Type => WatchingTaskInputType.Generate3DRFProduct;

    public required RFProduct.Factory RFProductFactory { get; init; }
    public required IRFProductStorage RFProducts { get; init; }
    public required INodeGui NodeGui { get; init; }
    public required TurboSquidUploader TurboSquid { get; init; }
    public required CGTraderUploader CGTrader { get; init; }
    public required NodeGlobalState GlobalState { get; init; }
    public required INodeSettings Settings { get; init; }
    public required Init Init { get; init; }
    CancellationTokenSource? CurrentTask;

    ImmutableArray<IUploader>? _Uploaders;
    ImmutableArray<IUploader> Uploaders => _Uploaders ??= [TurboSquid, CGTrader];

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
        foreach (var uploader in Uploaders)
            uploader.ClearLoginCache();

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

    void BumpSubmitJsonVersion(RFProduct rfproduct)
    {
        var submitJsonPath = GetSubmitJsonFile(rfproduct.Idea.Path);
        if (submitJsonPath is null) return;

        JObject jobj;
        try { jobj = JObject.Parse(File.ReadAllText(submitJsonPath)); }
        catch { jobj = new(); }

        var newversion = (jobj["Version"]?.ToObject<int>() ?? 0) + 1;
        jobj["Version"] = newversion;
        File.WriteAllText(submitJsonPath, jobj.ToString(Formatting.None));

        Logger.Info($"Updating {rfproduct.Idea.Path} version to {newversion}");
        (Input.DirectoryStructure2 ??= [])[rfproduct.Idea.Path] = new(true, Init.Version, GetDirectoryData(rfproduct.Idea.Path));
    }

    static string? GetSubmitJsonFile(string dir) => Directory.GetFiles(dir).SingleOrDefault(f => f.EndsWith("_Submit.json", StringComparison.Ordinal));
    static JObject? ReadSubmitJson(string dir) => GetSubmitJsonFile(dir) is string jf ? JObject.Parse(File.ReadAllText(jf)) : null;
    bool NeedsPublish(IUploader uploader, RFProduct rfproduct)
    {
        if (!File.Exists(Path.Combine(rfproduct.Idea.Path, uploader.MetaJsonName)))
            return true;

        if (Input.DirectoryStructure2?.GetValueOrDefault(rfproduct.Idea.Path) is null)
        {
            BumpSubmitJsonVersion(rfproduct);
            return true;
        }

        if (Input.DirectoryStructure2[rfproduct.Idea.Path].NodeVersion != Init.Version)
            return true;
        if (Input.DirectoryStructure2[rfproduct.Idea.Path].NeedsUploading)
            return true;

        var changed = WasDirectoryChanged(rfproduct.Idea.Path, out _);
        if (changed is not null)
        {
            BumpSubmitJsonVersion(rfproduct);
            Logger.Info(changed + " CHANGED, reWOUDING");
            return true;
        }

        var submitStatus = uploader.GetStatus(rfproduct);
        if (submitStatus is null or RFProduct._3D.Status.none)
        {
            Logger.Info(submitStatus + " submit status, reWOUDING");
            return true;
        }

        return false;
    }

    static async Task<T> GetStockRetryOrSkip<T>(StockCredentialContainer<T> container, string username, string password, INodeGui nodeGui, CancellationToken token, bool force = false) where T : class, I3DStock<T>
    {
        try
        {
            if (force)
                return await container.ForceGetAsync(username, password, token);
            return await container.GetAsync(username, password, token);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            var retry = await nodeGui.Request<bool>(new RetryOrSkipRequest("Login failed. Retry?"), token);
            if (!retry || !retry.Value)
                throw new OperationCanceledException();

            return await GetStockRetryOrSkip(container, username, password, nodeGui, token, true);
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
        Logger.Info("PUBLISHING ~" + products.Length + " PRODUCTS");

        SetState(state => state with { DraftedCount = 0, PublishedCount = 0 });

        var skippedLogins = new Dictionary<IUploader, List<string>>();

        token.ThrowIfCancellationRequested();

        foreach (var uploader in Uploaders)
        {
            /*async Task updateSalesIfNeeded()
            {
                if (Input.LastSalesFetch.AddHours(1) > DateTimeOffset.Now)
                    return;

                //if (Settings.MPlusUsername is null || Settings.MPlusPassword is null)
                //    return;

                //var mpcreds = new NetworkCredential(Settings.MPlusUsername, Settings.MPlusUsername);

                Logger.Info("Getting turbo squid sales");
                var sales = await turbo.SaleReports.ScanAsync(token).ToArrayAsync(token);

                foreach (var product in rfpgroup)
                    await product.UpdateSalesAsync(sales.SelectMany(s => s.SaleReports).ToArray(), turbo, token);

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
            }, token).Consume();*/

            Logger.Info("beforergfproduct");
            foreach (var rfproduct in products)
            {
                Logger.Info("hello " + rfproduct.Idea.Path);
                token.ThrowIfCancellationRequested();
                Dictionary<string, DirectoryStructurePart>? dirdata = null;

                if (NeedsPublish(uploader, rfproduct))
                {
                    dirdata = GetDirectoryData(rfproduct.Idea.Path);
                    Logger.Info($"INPRODUCT {uploader} {rfproduct.Idea.Path}");

                    try
                    {
                        SetState(state => state with { CurrentPublishing = rfproduct.Path });
                        Logger.Info($"Publishing to {uploader}: {rfproduct.Path}");

                        var token2 = CancellationTokenSource.CreateLinkedTokenSource(token, new CancellationTokenSource().Token);
                        token2.CancelAfter(TimeSpan.FromMinutes(20));

                        var creds = uploader.ReadCredentials(rfproduct);
                        if (creds is null) continue;

                        var login = await uploader.TryLogin(creds, token);
                        if (!login)
                        {
                            if (!skippedLogins.TryGetValue(uploader, out var list))
                                skippedLogins[uploader] = (list = []);
                            list.Add(creds.UserName);

                            continue;
                        }

                        var success = await uploader.TryUpload(rfproduct, token2.Token);
                        if (!success) continue;

                        try { File.Delete(Path.Combine(rfproduct.Idea.Path, "publish_exception.txt")); }
                        catch { }
                    }
                    catch (Exception ex)
                    {
                        SetState(state => state with { Error = $"{DateTimeOffset.Now} Error PUBLISHING product {uploader} {rfproduct.Path}:\n{ex}", });

                        Logger.Error(ex);
                        File.WriteAllText(Path.Combine(rfproduct.Idea.Path, "publish_exception.txt"), ex.ToString());
                        continue;
                    }
                }

                token.ThrowIfCancellationRequested();

                var submitStatus = ((RFProduct._3D.Idea_) rfproduct.Idea).Status;
                if (submitStatus == RFProduct._3D.Status.draft)
                    SetState(state => state with { DraftedCount = state.DraftedCount + 1 });
                else SetState(state => state with { PublishedCount = state.PublishedCount + 1 });

                (Input.DirectoryStructure2 ??= [])[rfproduct.Idea.Path] = new(false, Init.Version, dirdata ?? GetDirectoryData(rfproduct.Idea.Path));
                SaveTask();
            }
        }

        Input.LastSalesFetch = DateTimeOffset.Now;
    }

    string? WasDirectoryChanged(string directory, out Dictionary<string, DirectoryStructurePart> data)
    {
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
            foreach (var uploader in Uploaders)
                if (path.Contains(uploader.MetaJsonName)) continue;
            if (path.Contains("publish_exception")) continue;

            if (!dirstr.Parts.ContainsKey(path))
                return "dirstr no get value";
        }

        foreach (var (path, info) in prevdirstr.Parts)
        {
            // submit file and meta.json are being updated so exclude them from the check
            foreach (var uploader in Uploaders)
                if (path.Contains(uploader.MetaJsonName)) continue;
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


    public interface IUploader
    {
        string MetaJsonName { get; }

        void ClearLoginCache();

        NetworkCredential? ReadCredentials(RFProduct rfproduct);
        Task<bool> TryLogin(RFProduct rfproduct, CancellationToken token);
        Task<bool> TryLogin(NetworkCredential creds, CancellationToken token);

        /// <returns> False if the login was cancelled. </returns>
        Task<bool> TryUpload(RFProduct rfproduct, CancellationToken token);
        RFProduct._3D.Status? GetStatus(RFProduct rfproduct);
    }

    [AutoRegisteredService(false)]
    public abstract class Uploader<T> : IUploader where T : class, I3DStock<T>
    {
        public abstract string MetaJsonName { get; }
        public required StockCredentialContainer<T> Container { get; init; }
        public required Init Init { get; init; }
        public required INodeGui NodeGui { get; init; }
        public required ILogger<Uploader<T>> Logger { get; init; }

        public void ClearLoginCache() => Container.ClearCache();
        public async Task<bool> TryUpload(RFProduct rfproduct, CancellationToken token)
        {
            var stock = await TryLogin(rfproduct, token);
            if (stock is null) return false;

            return await Upload(stock, rfproduct, token);
        }

        protected abstract NetworkCredential? ReadCredentials(JObject submitJson);
        public NetworkCredential? ReadCredentials(RFProduct rfproduct)
        {
            var submitJson = ReadSubmitJson(rfproduct.Idea.Path);
            if (submitJson is null) return null;

            return ReadCredentials(submitJson);
        }

        async Task<bool> IUploader.TryLogin(RFProduct rfproduct, CancellationToken token) => (await TryLogin(rfproduct, token)) is not null;
        async Task<bool> IUploader.TryLogin(NetworkCredential creds, CancellationToken token) => (await TryLogin(creds, token)) is not null;
        protected async Task<T?> TryLogin(NetworkCredential creds, CancellationToken token)
        {
            try
            {
                var stock = await GetStockRetryOrSkip(Container, creds.UserName, creds.Password, NodeGui, token);
                Logger.Info($"logged in to {this} with {creds.UserName}");

                return stock;
            }
            catch (OperationCanceledException)
            {
                Logger.Info($"Username {creds.UserName} cancelled login, skipping.");
                return null;
            }
        }
        protected async Task<T?> TryLogin(RFProduct rfproduct, CancellationToken token)
        {
            var submitJson = ReadSubmitJson(rfproduct.Idea.Path);
            if (submitJson is null) return null;

            var creds = ReadCredentials(submitJson);
            if (creds is null) return null;

            return await TryLogin(creds, token);
        }

        public RFProduct._3D.Status? GetStatus(RFProduct rfproduct)
        {
            var metafile = Path.Combine(rfproduct.Idea.Path, MetaJsonName);
            if (!File.Exists(metafile)) return null;

            return JObject.Parse(metafile)?["Status"]?.ToObject<RFProduct._3D.Status>();
        }
        protected abstract Task<bool> Upload(T stock, RFProduct rfproduct, CancellationToken token);
    }
    public sealed class TurboSquidUploader : Uploader<TurboSquid>
    {
        public override string MetaJsonName => "turbosquid.json";
        readonly RateLimiter RateLimiter = new RateLimiter(100, TimeSpan.FromDays(1));

        protected override NetworkCredential? ReadCredentials(JObject submitJson)
        {
            var username = submitJson["LoginSquid"]?.ToObject<string>();
            if (string.IsNullOrEmpty(username)) return null;

            var password = submitJson["PasswordSquid"]?.ToObject<string>();
            if (string.IsNullOrEmpty(password)) return null;

            return new NetworkCredential(username, password);
        }

        protected override async Task<bool> Upload(TurboSquid stock, RFProduct rfproduct, CancellationToken token)
        {
            if (!RateLimiter.TryUse()) return false;

            await stock.UploadAsync(rfproduct, NodeGui, token);
            return true;
        }
    }
    public sealed class CGTraderUploader : Uploader<CGTrader>
    {
        public override string MetaJsonName => "cgtrader.json";

        protected override NetworkCredential? ReadCredentials(JObject submitJson)
        {
            var username = submitJson["LoginCGTrader"]?.ToObject<string>();
            if (string.IsNullOrEmpty(username)) return null;

            var password = submitJson["PasswordCGTrader"]?.ToObject<string>();
            if (string.IsNullOrEmpty(password)) return null;

            return new NetworkCredential(username, password);
        }

        protected override async Task<bool> Upload(CGTrader stock, RFProduct rfproduct, CancellationToken token)
        {
            await stock.UploadAsync(rfproduct, token);
            return true;
        }
    }


    class RateLimiter
    {
        readonly int MaxPerInterval;
        int Used;

        public RateLimiter(int maxPerInterval, TimeSpan interval)
        {
            MaxPerInterval = maxPerInterval;

            new Thread(() =>
            {
                while (true)
                {
                    Thread.Sleep(interval);
                    Used = 0;
                }
            })
            { IsBackground = true }.Start();
        }

        public bool TryUse()
        {
            if (Used >= MaxPerInterval)
                return false;

            Used++;
            return true;
        }
    }
}
