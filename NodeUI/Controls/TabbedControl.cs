namespace NodeUI.Controls
{
    public class TabbedControl : ContentPresenter
    {
        readonly Grid TitleGrid;
        readonly Panel ItemPanel;

        public TabbedControl()
        {
            TitleGrid = new Grid() { Margin = new Thickness(0, 0, 0, 5) };
            ItemPanel = new Panel();

            var grid = new Grid()
            {
                RowDefinitions = RowDefinitions.Parse("Auto *"),
                Children =
                {
                    TitleGrid.WithRow(0),
                    ItemPanel.WithRow(1),
                },
            };
            Content = grid;
        }

        public void Add(LocalizedString name, Control control)
        {
            if (TitleGrid.Children.Count != 0)
                control.IsVisible = false;

            var button = new MPButton()
            {
                Text = name,
                OnClick = () => Show(control),
            };

            TitleGrid.ColumnDefinitions.Add(new(GridLength.Auto));
            TitleGrid.Children.Add(button.WithColumn(TitleGrid.Children.Count));
            ItemPanel.Children.Add(control);
        }

        void Show(Control control)
        {
            foreach (var item in ItemPanel.Children)
                item.IsVisible = false;

            control.IsVisible = true;
        }
    }
}