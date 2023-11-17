using Avalonia.Animation.Easings;
using Avalonia.Data;

namespace Node.UI.Controls
{
    public class MPButton : Button
    {
        protected override Type StyleKeyOverride => typeof(Button);

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

            /*
            <Style Selector="^:pressed  /template/ ContentPresenter#PART_ContentPresenter">
                <Setter Property="Background" Value="{DynamicResource ButtonBackgroundPressed}" />
                <Setter Property="BorderBrush" Value="{DynamicResource ButtonBorderBrushPressed}" />
                <Setter Property="Foreground" Value="{DynamicResource ButtonForegroundPressed}" />
            </Style>
            */

            Styles.Add(new Style(t => t.Class("success").Template().Is<ContentPresenter>()) { Setters = { new Setter(ContentPresenter.BackgroundProperty, Brushes.Green) }, });
            Styles.Add(new Style(t => t.Class("error").Template().Is<ContentPresenter>()) { Setters = { new Setter(ContentPresenter.BackgroundProperty, Brushes.Red) }, });
        }


        public Task<bool> FlashErrorIfErr<T>(OperationResult<T> opres, int duration = 2000) => FlashErrorIfErr(opres.GetResult(), duration);
        public Task<bool> FlashErrorIfErr(OperationResult opres, int duration = 2000)
        {
            if (opres) return Task.FromResult(false);
            return FlashError(opres.ToString()!, duration).ContinueWith(_ => true);
        }
        public Task<bool> Flash<T>(OperationResult<T> opres, int duration = 2000) => Flash(opres.GetResult(), duration);
        public Task<bool> Flash(OperationResult opres, int duration = 2000)
        {
            if (opres) return FlashOk("ok", duration).ContinueWith(_ => true);
            return FlashError(opres.ToString()!, duration).ContinueWith(_ => false);
        }

        public Task FlashError(string text, int duration = 2000) => Flash(Brushes.Red, text, duration);
        public Task FlashOk(string? text = null, int duration = 2000) => Flash(Brushes.Green, text ?? Text.ToString(), duration);
        public async Task Flash(IBrush color, string text, int duration = 2000)
        {
            using var _ = new FuncDispose(() => IsEnabled = true);
            IsEnabled = false;

            if (color == Brushes.Red) Classes.Set("error", true);
            else if (color == Brushes.Green) Classes.Set("success", true);

            var prevtext = Text;
            Text = text;

            await Task.Delay(duration);
            Text = prevtext;
            Classes.Set("success", false);
            Classes.Set("error", false);
        }
    }
}
