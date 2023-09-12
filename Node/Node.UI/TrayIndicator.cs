using Avalonia.Controls.ApplicationLifetimes;
using SkiaSharp;

namespace Node.UI
{
    public static class TrayIndicator
    {
        readonly static Logger _logger = LogManager.GetCurrentClassLogger();
        static readonly byte[] IconBytes, GreenIconBytes;
        static readonly WindowIcon None, Red, Yellow, Green;

        static TrayIndicator()
        {
            using (var iconstream = Resource.LoadStream(typeof(TrayIndicator).Assembly, "img.tray_icon.png"))
                iconstream.Read(IconBytes = new byte[iconstream.Length]);

            InitializeColoredIcons(out GreenIconBytes, out None, out Red, out Yellow, out Green);

            {
                var g = SKBitmap.Decode(Resource.LoadStream(typeof(TrayIndicator).Assembly, "img.tray_icon.png"));
                using (var c = new SKCanvas(g))
                    c.Clear();
                GreenIconBytes = drawCircle(g, new SKColor(40, 255, 60, 255)).ToArray();
            }


            static void InitializeColoredIcons(out byte[] greenbytes, out WindowIcon none, out WindowIcon red, out WindowIcon yellow, out WindowIcon green)
            {
                var source = SKBitmap.Decode(IconBytes);

                none = new WindowIcon(drawCircle(source, new SKColor(0, 0, 0, 0)));
                red = new WindowIcon(drawCircle(source, new SKColor(255, 0, 0, 255)));
                yellow = new WindowIcon(drawCircle(source, new SKColor(255, 255, 0, 255)));

                var g = drawCircle(source, new SKColor(40, 255, 60, 255));
                greenbytes = g.ToArray();
                g.Seek(0, SeekOrigin.Begin);
                green = new WindowIcon(g);
            }
            static MemoryStream drawCircle(SKBitmap source, SKColor color)
            {
                using var bitmap = source.Copy();
                using (var canvas = new SKCanvas(bitmap))
                {
                    var size = bitmap.Width / 4;
                    canvas.DrawCircle(bitmap.Width - 10 - size / 2, bitmap.Height - 10 - size / 2, size, new SKPaint() { Color = color });
                    canvas.Flush();
                }

                var output = new MemoryStream();
                bitmap.Encode(output, SKEncodedImageFormat.Png, 100);
                output.Seek(0, SeekOrigin.Begin);

                return output;
            }

        }

        public static void PreventClosing(this Window window)
        {
            window.Closing += (_, e) =>
            {
                e.Cancel = true;
                window.Hide();
            };
        }
        public static void InitializeTrayIndicator(this Application app)
        {
            var items = new (LocalizedString, Action)?[]
            {
                ("menu.open", open),
                null,
                ("menu.close", exit),
            }.ToImmutableArray();

            // TODO: remove four transparent pixels after fix
            var icon = new TrayIcon() { ToolTipText = App.Instance.AppName, Icon = None };
            icon.Clicked += (_, _) => open();
            icon.FixException();

            InitializeIconInfo(icon);

            LocalizedString.ChangeLangWeakEvent.Subscribe(app, () => Dispatcher.UIThread.Post(updateMenus));
            updateMenus();

            void updateMenus()
            {
                var menu = new NativeMenu();
                foreach (var item in items)
                {
                    if (item is null) menu.Items.Add(new NativeMenuItemSeparator());
                    else
                    {
                        var nitem = new NativeMenuItem(item.Value.Item1.ToString());
                        nitem.Click += (obj, e) => item.Value.Item2();

                        menu.Items.Add(nitem);
                    }
                }

                try { icon.Menu = menu; }
                catch { }
            }
            void open()
            {
                Dispatcher.UIThread.Post(() =>
                {
                    var lifetime = (IClassicDesktopStyleApplicationLifetime) app.ApplicationLifetime!;

                    var window = lifetime.MainWindow ?? App.Instance.SetMainWindow(lifetime);
                    if (window.IsVisible) window.Hide();
                    else window.Show();
                });
            }
            void exit() => new Thread(() =>
            {
                try { SystemService.Stop(); }
                catch (Exception ex) { Console.WriteLine(ex); }

                FileList.KillProcesses();
                Environment.Exit(0);
            }).Start();
        }

        static void InitializeIconInfo(TrayIcon icon)
        {
            var iconcache = new Dictionary<string, WindowIcon>();
            int time = -1;

            void update()
            {
                time++;

                var wicon = getIcon();
                if (icon.Icon != wicon)
                    icon.Icon = wicon;


                WindowIcon getIcon()
                {
                    if (!NodeStateUpdater.IsConnectedToNode.Value)
                    {
                        icon.ToolTipText = LocalizedString.String("No connection to node");
                        return Red;
                    }
                    if (NodeGlobalState.Instance.ExecutingTasks.Count == 0)
                    {
                        icon.ToolTipText = LocalizedString.String("Idle");
                        return Yellow;
                    }


                    var task = NodeGlobalState.Instance.ExecutingTasks[0];

                    icon.ToolTipText = $@"
                        {task.Id}
                        {string.Join('-', task.Actions)}
                        {Newtonsoft.Json.JsonConvert.SerializeObject(task.Info.Input, Newtonsoft.Json.Formatting.None)}
                        ".TrimLines();

                    if ((time / 3) % 2 == 0)
                        return drawOnGreen(NodeGlobalState.Instance.ExecutingTasks.Count + "/" + NodeGlobalState.Instance.QueuedTasks.Count);

                    return drawOnGreen((int) ((NodeGlobalState.Instance.ExecutingTasks.FirstOrDefault()?.Progress ?? 0) * 100) + "%");
                }
                WindowIcon drawOnGreen(string text)
                {
                    if (iconcache.TryGetValue(text, out var cachedicon))
                        return cachedicon;


                    var bitmap = SKBitmap.Decode(GreenIconBytes);
                    using (var canvas = new SKCanvas(bitmap))
                    {
                        var paint = new SKPaint(new SKFont(SKTypeface.Default, size: bitmap.Height * .8f * (MathF.Pow(.9f, text.Length)))) { Color = new SKColor(uint.MaxValue), TextAlign = SKTextAlign.Center, };
                        canvas.DrawText(text, bitmap.Width / 2, bitmap.Height * .75f, paint);
                    }

                    var output = new MemoryStream();
                    bitmap.Encode(output, SKEncodedImageFormat.Png, 100);
                    output.Seek(0, SeekOrigin.Begin);

                    return iconcache[text] = new WindowIcon(new Bitmap(output));
                }
            }


            new Thread(() =>
            {
                while (true)
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        try { update(); }
                        catch (Exception ex) { _logger.Error(ex); }
                    });

                    Thread.Sleep(1000);
                }
            })
            { IsBackground = true }.Start();

        }
    }
}