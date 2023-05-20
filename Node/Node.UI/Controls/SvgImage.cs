using System.Xml;

namespace Node.UI.Controls
{
    public class SvgImage : Decorator
    {
        static readonly Dictionary<string, (Geometry geometry, SolidColorBrush fill, SolidColorBrush stroke)[]> CachedSources = new Dictionary<string, (Geometry, SolidColorBrush, SolidColorBrush)[]>();

        Stretch _Stretch;
        public Stretch Stretch
        {
            get => _Stretch;
            set
            {
                _Stretch = value;

                if (Child is Panel panel)
                    foreach (var child in panel.Children.OfType<APath>())
                        child.Stretch = value;
            }
        }

        public string Source
        {
            set
            {
                if (!CachedSources.TryGetValue(value, out var source))
                {
                    var xml = new XmlDocument();
                    xml.Load(Resource.LoadStream(this, value));


                    static SolidColorBrush parseColor(string color)
                    {
                        if (string.IsNullOrEmpty(color)) return Colors.Transparent;
                        return new SolidColorBrush(Color.Parse(color));
                    }

                    CachedSources[value] = source = xml["svg"]!.ChildNodes.OfType<XmlElement>().Select(x =>
                        (Geometry.Parse(x.GetAttribute("d")), parseColor(x.GetAttribute("fill")), parseColor(x.GetAttribute("stroke")))).ToArray();
                }

                var panel = new Panel();
                panel.Children.AddRange(source.Select(x => new APath() { Stretch = Stretch, Data = x.geometry, Fill = x.fill, Stroke = x.stroke, StrokeThickness = 1 }));

                Child = panel;
            }
        }

        public IEnumerable<APath> Children => (Child as Panel)?.Children.OfType<APath>() ?? throw new NullReferenceException();
    }
}