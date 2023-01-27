using Avalonia.Animation.Easings;

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
                TextBlock.Foreground = v ? Colors.White : Colors.Black;
            });
        }


        public Task<bool> FlashErrorIfErr<T>(OperationResult<T> opres, int duration = 2000) => FlashErrorIfErr(opres.GetResult(), duration);
        public Task<bool> FlashErrorIfErr(OperationResult opres, int duration = 2000)
        {
            if (opres) return Task.FromResult(false);
            return FlashError(opres.Message!, duration).ContinueWith(_ => true);
        }
        public Task<bool> Flash(OperationResult opres, int duration = 2000)
        {
            if (opres) return FlashOk("ok", duration).ContinueWith(_ => false);
            return FlashError(opres.Message!, duration).ContinueWith(_ => true);
        }

        public Task FlashError(string text, int duration = 2000) => Flash(Brushes.Red, text, duration);
        public Task FlashOk(string text, int duration = 2000) => Flash(Brushes.Green, text, duration);
        public async Task Flash(IBrush color, string text, int duration = 2000)
        {
            using var _ = new FuncDispose(() => IsEnabled = true);
            IsEnabled = false;

            var prevbg = Background;
            Background = color;
            Border.Transitions ??= new();
            Border.Transitions.Add(new BrushTransition() { Property = Border.BackgroundProperty, Duration = TimeSpan.FromMilliseconds(duration), Easing = new QuarticEaseIn() });
            Background = prevbg;

            var prevtext = Text;
            Text = text;

            await Task.Delay(duration);
            Text = prevtext;
            Border.Transitions.Clear();
        }
    }
}