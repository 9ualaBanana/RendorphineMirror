namespace NodeUI.Pages
{
    public class OkMessageBox : MessageBox<object?>
    {
        public OkMessageBox() : base(null)
        {
            var button = AddButton("general.ok", null);
            button.MaxWidth = 100;
        }
    }
    public class YesNoMessageBox : MessageBox<bool>
    {
        public YesNoMessageBox(bool defaultresult) : base(defaultresult)
        {
            AddButton("general.yes", true);
            AddButton("general.no", false);
        }
    }

    public interface IMessageBox
    {
        string Text { get; set; }

        void Show();
        void Close();
    }
    public class MessageBox : MessageBox<object?>
    {
        public MessageBox(LocalizedString buttontext) : base(null) => AddButton(buttontext, null);
    }
    public class MessageBox<T> : Window, IMessageBox
    {
        public string Text { get => TextBlock.Text; set => TextBlock.Text = value; }

        public Action<T>? OnClick;

        readonly TextBlock TextBlock;
        readonly Grid Grid;

        public MessageBox(T closeresult)
        {
            SizeToContent = SizeToContent.WidthAndHeight;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            this.FixStartupLocation();
            Background = Colors.DarkDarkGray;
            CanResize = false;
            Closed += (_, _) => OnClick?.Invoke(closeresult);

            TextBlock = new TextBlock() { Margin = new Avalonia.Thickness(20, 10), Foreground = Colors.From(210), FontSize = 14 };

            Grid = new Grid();
            Grid.ColumnDefinitions.Add(new ColumnDefinition());

            Grid.RowDefinitions.Add(new RowDefinition(4, GridUnitType.Star));
            Grid.RowDefinitions.Add(new RowDefinition(3, GridUnitType.Star));

            Grid.Children.Add(TextBlock);
            Grid.SetColumnSpan(TextBlock, 2);

            Content = Grid;
        }

        public MessageBox<T> WithButton(LocalizedString text, T result, Action<MBMPButton>? modifybtnfunc = null)
        {
            var button = AddButton(text, result);
            modifybtnfunc?.Invoke(button);

            return this;
        }
        public MBMPButton AddButton(LocalizedString text, T result)
        {
            var button = new MBMPButton(text, () =>
            {
                OnClick?.Invoke(result);
                OnClick = null;
                Close();
            });

            Grid.Children.Add(button);

            if (Grid.Children.Count != 2) Grid.ColumnDefinitions.Add(new ColumnDefinition());
            Grid.SetRow(button, 1);
            Grid.SetColumn(button, Grid.Children.Count - 2);

            return button;
        }


        public class MBMPButton : MPButton
        {
            public MBMPButton(LocalizedString text, Action onClick)
            {
                Text = text;
                OnClick += onClick;

                Margin = new Avalonia.Thickness(10);
                Foreground = Colors.From(210);
                Background = Colors.MenuItemBackground;
                HoverBackground = Colors.From(113, 168, 79);
            }
        }
    }
}