namespace NodeUI.Controls
{
    public class ClickableControl : UserControl
    {
        public Action OnClick = delegate { };
        bool IsPressed;

        public ClickableControl()
        {
            Background = Colors.AlmostTransparent;

            updateFocusable(0);
            void updateFocusable<T>(T _) => Focusable = IsVisible && IsEnabled;

            this.GetObservable(IsVisibleProperty).Subscribe(updateFocusable);
            this.GetObservable(IsEnabledProperty).Subscribe(updateFocusable);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (!IsFocused || !IsEnabled || e.Key != Key.Enter) return;

            OnClick();
            e.Handled = true;
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);

            if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;

            IsPressed = true;
            e.Handled = true;
        }
        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);

            if (!IsPressed || e.InitialPressMouseButton != MouseButton.Left) return;

            IsPressed = false;
            e.Handled = true;

            if (this.GetVisualsAt(e.GetPosition(this)).Any(c => this == c || this.IsVisualAncestorOf(c)))
                OnClick();
        }
        protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e) => IsPressed = false;
    }
    public class ClickableSwitchControl : ClickableControl
    {
        public event Action<bool> OnToggle = delegate { };
        public bool IsToggled { get; private set; }

        public ClickableSwitchControl() => OnClick += () => OnToggle(IsToggled = !IsToggled);
    }
}