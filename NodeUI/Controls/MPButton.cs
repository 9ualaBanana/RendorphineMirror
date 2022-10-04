namespace NodeUI.Controls
{
    public class MPButton : ClickableControl<MPButton>
    {
        public LocalizedString Text { get => TextBlock.GetValue(TextBlock.TextProperty); set => TextBlock.Bind(TextBlock.TextProperty, value); }
        public new FontWeight FontWeight { get => TextBlock.FontWeight; set => TextBlock.FontWeight = value; }
        public new double FontSize { get => TextBlock.FontSize; set => TextBlock.FontSize = value; }

        IBrush _Background = null!;
        public new IBrush Background
        {
            get => _Background;
            set => Border.Background = _Background = value;
        }
        public IBrush HoverBackground = Colors.DarkDarkGray;

        readonly TextBlock TextBlock;
        readonly Border Border;

        public MPButton()
        {
            TextBlock = new TextBlock
            {
                Foreground = Colors.White,
                FontSize = 16,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };

            Border = new Border()
            {
                CornerRadius = new CornerRadius(5),
                Child = TextBlock
            };

            Content = Border;
            Background = Colors.Accent;

            PointerEnter += (_, _) =>
            {
                if (IsEnabled) Border.Background = HoverBackground;
            };
            PointerLeave += (_, _) =>
            {
                if (IsEnabled) Border.Background = Background;
            };

            this.GetObservable(IsEnabledProperty).Subscribe(v =>
            {
                Border.Background = v ? Background : HoverBackground;
                TextBlock.Foreground = v ? Colors.White : Colors.GrayButton;
            });
        }

        public Task TemporarySetTextIfErr<T>(OperationResult<T> opres, int duration = 2000) => TemporarySetTextIfErr(opres.GetResult(), duration);
        public Task TemporarySetTextIfErr(OperationResult opres, int duration = 2000)
        {
            if (opres) return Task.CompletedTask;
            return TemporarySetText(opres.Message!, duration);
        }
        public async Task TemporarySetText(string text, int duration = 2000)
        {
            var prevtext = Text;
            Text = text;

            await Task.Delay(duration);
            Text = prevtext;
        }
    }
}