namespace Node.UI.Controls
{
    public class LoadCircle : UserControl
    {
        static readonly Geometry BarGeometry = Geometry.Parse(@"m 12.5 25 c -0.48303 -0.0254 -0.9379 -0.2352 -1.27082 -0.5861 -0.332924 -0.3509 -0.518517
            -0.8162 -0.518517 -1.2999 0 -0.4837 0.185593 -0.9489 0.518517 -1.2998 0.33292 -0.3509 0.78779 -0.5607 1.27082 -0.5861 2.31441
            -0.0014 4.53363 -0.9214 6.17017 -2.55793 1.6365 -1.63654 2.5565 -3.85576 2.5579 -6.17017 0.0254 -0.48303 0.2352 -0.9379 0.5861
            -1.27082 0.3509 -0.332924 0.8162 -0.518517 1.2999 -0.518517 0.4837 0 0.9489 0.185593 1.2998 0.518517 0.3509 0.33292 0.5607
            0.78779 0.5861 1.27082 -0.0055 3.31351 -1.3242 6.48973 -3.6672 8.8327 C 18.98973 23.6758 15.81351 24.9945 12.5 25 Z");

        public LoadCircle() : this(Colors.From(204, 234, 155), Colors.From(99, 195, 24)) { }
        public LoadCircle(IBrush circleColor, IBrush barColor)
        {
            Width = Height = 25;

            var circle = new Ellipse
            {
                StrokeThickness = 4,
                Stroke = circleColor,
            };

            var loadbar = new APath
            {
                Data = BarGeometry,
                Fill = barColor,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Bottom
            };

            StartRotatiing(loadbar);

            var panel = new Panel();
            panel.Children.Add(circle);
            panel.Children.Add(loadbar);

            Content = panel;
        }

        static void StartRotatiing(Control control)
        {
            const int rotateduration = 2000;

            var transform = new RotateTransform();
            control.RenderTransform = transform;

            transform.Transitions = new Transitions
            {
                new DoubleTransition()
                {
                    Duration = TimeSpan.FromMilliseconds(rotateduration),
                    Property = RotateTransform.AngleProperty
                }
            };


            Task.Run(() =>
            {
                var angle = 0;
                while (true)
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        if (control.IsVisible)
                            transform.Angle = 360 * angle++;
                    });

                    Thread.Sleep(rotateduration);
                }
            });
        }
    }
}