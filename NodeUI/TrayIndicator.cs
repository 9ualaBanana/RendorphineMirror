using Avalonia.Controls.ApplicationLifetimes;

namespace NodeUI
{
    public static class TrayIndicator
    {
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
                (Localized.Menu.Open, open),
                null,
                (Localized.Menu.Close, exit),
            }.ToImmutableArray();

            var icon = new TrayIcon() { ToolTipText = App.AppName, Icon = new WindowIcon(Resource.LoadStream(typeof(TrayIndicator).Assembly, "img.tray_icon.png")) };
            icon.Clicked += (_, _) => open();
            icon.FixException();


            LocalizedString.ChangeLangWeakEvent.Subscribe(app, updateMenus);
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
                    var window = ((IClassicDesktopStyleApplicationLifetime) app.ApplicationLifetime!).MainWindow;

                    if (window.IsVisible) window.Hide();
                    else window.Show();
                });
            }
            void exit() => new Thread(() =>
            {
                try { SystemService.Stop(); }
                catch (Exception ex) { Console.WriteLine(ex); }

                foreach (var process in FileList.GetProcesses())
                {
                    Console.WriteLine("Killing " + process.ProcessName);
                    try { process.Kill(); }
                    catch (Exception ex) { Console.WriteLine("fail " + ex); }
                }

                Environment.Exit(0);
            }).Start();
        }
    }
}