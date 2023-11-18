namespace Node.UI.Pages
{
    public class OkMessageBox : MessageBox<object?>
    {
        public OkMessageBox() : base(null)
        {
            var button = AddButton("general.ok", null);
            button.MaxWidth = 100;
        }

        public static async Task ShowDialogIfError(Control owner, OperationResult result)
        {
            if (result) return;
            await new OkMessageBox() { Text = result.Error.ToString() }.ShowDialog((Window) owner.GetVisualRoot()!);
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
        public string Text { get => TextBlock.Text ?? string.Empty; set => TextBlock.Text = value; }

        public Action<T>? OnClick;

        readonly TextBlock TextBlock;
        readonly Grid Grid;
        readonly StackPanel Buttons;

        public MessageBox(T closeresult)
        {
            this.AttachDevToolsIfDebug();

            SizeToContent = SizeToContent.WidthAndHeight;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            this.FixStartupLocation();
            Background = Colors.DarkDarkGray;
            CanResize = false;
            Closed += (_, _) => OnClick?.Invoke(closeresult);

            TextBlock = new TextBlock() { Margin = new Thickness(20, 10), Foreground = Colors.From(210), FontSize = 14 };

            Buttons = new StackPanel()
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
            };

            Grid = new Grid()
            {
                RowDefinitions = RowDefinitions.Parse("4* 3*"),
                Children =
                {
                    TextBlock.WithRow(0),
                    Buttons.WithRow(1),
                },
            };

            Content = Grid;
        }

        public MessageBox<T> WithButton(LocalizedString text, T result, Action<MPButton>? modifybtnfunc = null)
        {
            var button = AddButton(text, result);
            modifybtnfunc?.Invoke(button);

            return this;
        }
        public MPButton AddButton(LocalizedString text, T result)
        {
            return new MPButton()
            {
                Text = text,
                OnClick = () =>
                {
                    OnClick?.Invoke(result);
                    OnClick = null;
                    Close();
                },

                Margin = new Thickness(10),
                Foreground = Colors.From(210),
                Background = Colors.MenuItemBackground,
                HoverBackground = Colors.From(113, 168, 79),
            }.With(Buttons.Children.Add);
        }
    }
}
