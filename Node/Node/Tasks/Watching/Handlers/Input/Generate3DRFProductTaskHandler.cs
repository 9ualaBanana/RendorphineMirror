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

    public override void StartListening() => StartThreadRepeated(5_000, RunOnce);

    public async Task RunOnce()
    {
        if (Input.AutoCreateRFProducts)
        {
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
                SaveTask();
            }
        }

        if (Input.AutoPublishRFProducts)
        {
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
                SaveTask();
            }
        }

    }

    async Task GenerateQSProducts()
    {
        foreach (var productDir in Directory.GetDirectories(Input.InputDirectory))
        {
            if (File.Exists(Path.Combine(productDir, ".rfproducted"))) continue;

            try
            {
                Logger.Info("Creating product " + productDir);
                var rfp = await RFProductFactory.CreateAsync(productDir, Directories.DirCreated(Input.RFProductDirectory, Path.GetFileNameWithoutExtension(productDir)), default, false);
                Logger.Info($"Auto-created rfproduct {rfp.ID} @ {rfp.Idea.Path}");
                File.Create(Path.Combine(productDir, ".rfproducted")).Dispose();
            }
            catch (Exception ex)
            {
                File.WriteAllText(Path.Combine(productDir, "publish_exception.txt"), ex.ToString());
            }
        }
    }
    async Task PublishRFProducts(CancellationToken token)
    {
        foreach (var rfproduct in RFProducts.RFProducts.Values.Where(r => r.Type == nameof(RFProduct._3D) && r.Path.StartsWith(Path.GetFullPath(Input.RFProductDirectory))))
        {
            var submitJson = JObject.Parse(await File.ReadAllTextAsync(Directory.GetFiles(rfproduct.Idea.Path).Single(f => f.EndsWith("_Submit.json")), token));

            if ((submitJson["toSubmitSquid"]?.ToObject<ToSubmit>() ?? ToSubmit.None) == ToSubmit.Submit)
            {
                if (File.Exists(Path.Combine(rfproduct.Idea.Path, "turbosquid.meta")))
                {
                    var firstline = File.ReadLines(Path.Combine(rfproduct.Idea.Path, "turbosquid.meta")).FirstOrDefault();
                    if (firstline is not null && Regex.IsMatch(firstline, @"\[\d+\]"))
                        continue;
                }

                var username = submitJson["LoginSquid"]!.ToObject<string>().ThrowIfNull();
                var password = submitJson["PasswordSquid"]!.ToObject<string>().ThrowIfNull();

                Logger.Info($"Publishing to turbosquid: {rfproduct}");
                var turbo = await TurboSquidContainer.GetAsync(username, password, token);
                await turbo.PublishAsync(rfproduct, NodeGui, token);
            }
            else if ((submitJson["toSubmitTrader"]?.ToObject<ToSubmit>() ?? ToSubmit.None) == ToSubmit.Submit)
            {
                var username = submitJson["LoginCGTrader"]!.ToObject<string>().ThrowIfNull();
                var password = submitJson["PasswordCGTrader"]!.ToObject<string>().ThrowIfNull();
            }
        }
    }
    enum ToSubmit { Submit, SubmitOffline, None }

}
