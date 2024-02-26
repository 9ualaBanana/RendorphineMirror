using System.Text.RegularExpressions;
using _3DProductsPublish.Turbosquid;

namespace Node.Tasks.Watching.Handlers.Input;

public class Generate3DRFProductTaskHandler : WatchingTaskInputHandler<Generate3DRFProductTaskInputInfo>, ITypedTaskWatchingInput
{
    public static WatchingTaskInputType Type => WatchingTaskInputType.Generate3DRFProduct;

    public required RFProduct.Factory RFProductFactory { get; init; }
    public required IRFProductStorage RFProducts { get; init; }
    public required INodeGui NodeGui { get; init; }
    public required TurboSquidContainer TurboSquidContainer { get; init; }
    public required NodeGlobalState GlobalState { get; init; }

    public override void StartListening() => StartThreadRepeated(5_000, RunOnce);

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
            return state with
            {
                FileCount = Directory.GetDirectories(Input.InputDirectory).Length,
                CurrentPublishing = null,
                CurrentRFProducting = null,
                PublishedCount = null,
                RFProductedCount = null,
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
        SetState(state => state with
        {
            FileCount = Directory.GetDirectories(Input.InputDirectory).Length,
            RFProductedCount = Directory.GetDirectories(Input.InputDirectory).Count(dir => File.Exists(Path.Combine(dir, ".rfproducted"))),
        });

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
            }
            catch (Exception ex)
            {
                SetState(state => state with { Error = $"Error RFPRODUCTING product {productDir}:\n{ex}", });

                Logger.Error(ex);
                File.WriteAllText(Path.Combine(productDir, "publish_exception.txt"), ex.ToString());
            }
        }
    }
    async Task PublishRFProducts(CancellationToken token)
    {
        JObject readSubmitJson(string dir) => JObject.Parse(File.ReadAllText(Directory.GetFiles(dir).Single(f => f.EndsWith("_Submit.json"))));
        bool shouldSubmitToTurboSquid(string dir, JObject submitJson) => (submitJson["toSubmitSquid"]?.ToObject<ToSubmit>() ?? ToSubmit.None) == ToSubmit.Submit;
        bool isSubmittedTurboSquid(string dir)
        {
            if (!File.Exists(Path.Combine(dir, "turbosquid.meta"))) return false;

            var firstline = File.ReadLines(Path.Combine(dir, "turbosquid.meta")).FirstOrDefault();
            return firstline is not null && Regex.IsMatch(firstline, @"\[\d+\]");
        }

        var products = RFProducts.RFProducts.Values.Where(r => r.Type == nameof(RFProduct._3D) && r.Path.StartsWith(Path.GetFullPath(Input.InputDirectory))).ToArray();

        SetState(state => state with
        {
            RFProductedCount = products.Length,
            PublishedCount = products.Count(p => shouldSubmitToTurboSquid(p.Idea.Path, readSubmitJson(p.Idea.Path)) && isSubmittedTurboSquid(p.Idea.Path)),
        });

        foreach (var rfproduct in products)
        {
            SetState(state => state with { CurrentPublishing = rfproduct.Path });

            try
            {
                var submitJson = readSubmitJson(rfproduct.Idea.Path);
                if (shouldSubmitToTurboSquid(rfproduct.Idea.Path, submitJson) && !isSubmittedTurboSquid(rfproduct.Idea.Path))
                {
                    var username = submitJson["LoginSquid"]!.ToObject<string>().ThrowIfNull();
                    var password = submitJson["PasswordSquid"]!.ToObject<string>().ThrowIfNull();

                    Logger.Info($"Publishing to turbosquid: {rfproduct.Path}");
                    var turbo = await TurboSquidContainer.GetAsync(username, password, token);
                    await turbo.PublishAsync(rfproduct, NodeGui, token);

                    SetState(state => state with { PublishedCount = state.PublishedCount + 1 });
                }
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
        }
    }
    enum ToSubmit { Submit, SubmitOffline, None }

}
