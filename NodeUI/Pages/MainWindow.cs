namespace NodeUI.Pages
{
    public class MainWindow : Window
    {
        public MainWindow()
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            this.FixStartupLocation();
            Width = 692;
            Height = 410;
            Title = App.AppName;
            Icon = App.Icon;

            this.PreventClosing();


            var tabs = new TabbedControl();
            tabs.Add(Localized.Tab.Dashboard, new DashboardTab());
            tabs.Add(Localized.Tab.Plugins, new PluginsTab());
            tabs.Add(Localized.Tab.Benchmark, new BenchmarkTab());

            Content = tabs;
        }


        class DashboardTab : Panel
        {
            public DashboardTab()
            {
                Children.Add(new TextBlock()
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Text = "dashboard window says hello world from " + DateTimeOffset.Now,
                });

                var langbtn = new MPButton()
                {
                    MaxWidth = 100,
                    MaxHeight = 30,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Bottom,
                    Text = Localized.Lang.Current,
                    OnClick = () => Settings.Language = Settings.Language == "ru-RU" ? "en-US" : "ru-RU",
                };
                Children.Add(langbtn);
            }
        }
        class PluginsTab : Panel
        {
            public PluginsTab()
            {
                Children.Add(new TextBlock()
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Text = "im plugin tab hello",
                });
            }
        }
        class BenchmarkTab : Panel
        {
            public BenchmarkTab()
            {
                Background = Brushes.Aqua;
                Children.Add(new TextBlock()
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Text = "beach mark",
                });
            }
        }
    }
}