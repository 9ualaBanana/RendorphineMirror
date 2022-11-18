using CefNet;
using CefNet.Internal;

namespace NodeUI.Controls;

/// <summary>
/// Lazy-loading wrapper for <see cref="CefNet.Avalonia.WebView"/> to not immediately crash when debugging.
/// TODO: remove if debugging with CEF fixed
/// </summary>
public class WebView : Avalonia.Controls.Decorator
{
    class _WebView : CefNet.Avalonia.WebView
    {
        protected override WebViewGlue CreateWebViewGlue() => new Glue(this);


        class Glue : WebViewGlue
        {
            readonly Dictionary<string, MemoryStream> Bodies = new();

            public Glue(IChromiumWebViewPrivate view) : base(view) { }

            protected override void OnResourceLoadComplete(CefBrowser browser, CefFrame frame, CefRequest request, CefResponse response, CefUrlRequestStatus status, long receivedContentLength)
            {
                if (Bodies.Remove(request.Url, out var stream))
                {
                    stream.Position = 0;

                    /*
                    if (request.Url.StartsWith("https://google.com/search", StringComparison.Ordinal))
                    {
                        string data;
                        using (var reader = new StreamReader(stream))
                            data = await reader.ReadToEndAsync();

                        Console.WriteLine(data);
                    }
                    */
                }

                base.OnResourceLoadComplete(browser, frame, request, response, status, receivedContentLength);
            }
            protected override CefResponseFilter GetResourceResponseFilter(CefBrowser browser, CefFrame frame, CefRequest request, CefResponse response) =>
                new RFilter(Bodies[request.Url] = new MemoryStream());


            // Empty response filter that also saves all data into a stream
            public class RFilter : CefResponseFilter
            {
                readonly Stream Stream;

                public RFilter(Stream stream) => Stream = stream;

                protected override bool InitFilter() => true;
                protected override unsafe CefResponseFilterStatus Filter(IntPtr dataIn, long dataInSize, ref long dataInRead, IntPtr dataOut, long dataOutSize, ref long dataOutWritten)
                {
                    if (dataIn == null)
                    {
                        dataInRead = 0;
                        dataOutWritten = 0;

                        return CefResponseFilterStatus.Done;
                    }

                    var amount = (int) Math.Min(dataInSize, Math.Min(dataOutSize, 65536));

                    var din = new Span<byte>((void*) dataIn, amount);
                    din.CopyTo(new Span<byte>((void*) dataOut, amount));

                    dataInRead += amount;
                    dataOutWritten += amount;

                    Stream.Write(din);
                    return CefResponseFilterStatus.Done;
                }
            }
        }
    }


    public CefNet.Avalonia.WebView View { get; private set; } = null!;

    public WebView()
    {
        View = new _WebView();

        if (!Debugger.IsAttached) init();
        else
        {
            Child = createtb("WebView placeholder; Hover to enable");
            Child.PointerEnter += (obj, e) => init();
        }


        static TextBlock createtb(string text) =>
            new TextBlock()
            {
                Background = Colors.AlmostTransparentWhite,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                Text = text,
            };

        void init()
        {
            try
            {
                Program.InitializeCef();
                Child = View;
            }
            catch (Exception ex)
            {
                LogManager.GetCurrentClassLogger().Error($"Could not initialize CEF: {ex}");

                Child = createtb($"Could not initialize CEF: {ex.Message}");
                ((TextBlock) Child).Foreground = Brushes.Red;
            }
        }
    }
}
