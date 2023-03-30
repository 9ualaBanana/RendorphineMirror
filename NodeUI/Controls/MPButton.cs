using Avalonia.Animation.Easings;

namespace NodeUI.Controls
{
    public class MPButton : Button, IStyleable
    {
        Type IStyleable.StyleKey => typeof(Button);

        public LocalizedString Text { get => (Content is LocalizedString ls) ? ls : (Content as string) ?? ""; set => Content = value; }

        public new Action OnClick = delegate { };
        public Action<MPButton> OnClickSelf = delegate { };

        // border only !!!!
        public IBrush HoverBackground { set => this.AddStyle(("ThemeBorderMidBrush", value)); }

        public MPButton()
        {
            Click += (_, _) =>
            {
                OnClick();
                OnClickSelf(this);
            };
        }


        public Task<bool> FlashErrorIfErr<T>(OperationResult<T> opres, int duration = 2000) => FlashErrorIfErr(opres.GetResult(), duration);
        public Task<bool> FlashErrorIfErr(OperationResult opres, int duration = 2000)
        {
            if (opres) return Task.FromResult(false);
            return FlashError(opres.Message!, duration).ContinueWith(_ => true);
        }
        public Task<bool> Flash<T>(OperationResult<T> opres, int duration = 2000) => Flash(opres.GetResult(), duration);
        public Task<bool> Flash(OperationResult opres, int duration = 2000)
        {
            if (opres) return FlashOk("ok", duration).ContinueWith(_ => true);
            return FlashError(opres.Message!, duration).ContinueWith(_ => false);
        }

        public Task FlashError(string text, int duration = 2000) => Flash(Brushes.Red, text, duration);
        public Task FlashOk(string text, int duration = 2000) => Flash(Brushes.Green, text, duration);
        public async Task Flash(IBrush color, string text, int duration = 2000)
        {
            using var _ = new FuncDispose(() => IsEnabled = true);
            IsEnabled = false;

            var prevbg = Background;
            Background = color;
            Transitions ??= new();
            Transitions.Add(new BrushTransition() { Property = Border.BackgroundProperty, Duration = TimeSpan.FromMilliseconds(duration), Easing = new QuarticEaseIn() });
            Background = prevbg;

            var prevtext = Text;
            Text = text;

            await Task.Delay(duration);
            Text = prevtext;
            Transitions.Clear();
        }
    }
}