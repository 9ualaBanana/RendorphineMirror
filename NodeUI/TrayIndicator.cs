namespace NodeUI
{
    public class TrayIndicator
    {
        public void Initialize()
        {
            var items = new (LocalizedString, Action)?[] { (Localized.Menu.Close, exit), }.ToImmutableArray();
            var icon = new TrayIcon() { ToolTipText = App.AppName, Icon = new WindowIcon(Resource.LoadStream(typeof(TrayIndicator).Assembly, "img.tray_icon.png")) };

            LocalizedString.ChangeLangWeakEvent.Subscribe(this, updateMenus);
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
            void exit()
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
            }
        }
    }
}