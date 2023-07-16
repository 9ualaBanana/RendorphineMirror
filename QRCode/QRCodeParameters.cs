using ZXing;
using ZXing.QrCode;

namespace QRCode;

[CommandLine.Verb("without-logo",
    isDefault: false,
    aliases: new string[] { "w/ologo" })]
internal record QRCodeParameters
{
    [CommandLine.Value(index: 0, Required = true)]
    public string Data { get; init; }

    [CommandLine.Option('s', "size", Default = 500)]
    public int Size { get; init; }

    [CommandLine.Option("ecl", Default = 'Q')]
    public char Ecl { get; init; }

    [CommandLine.Option('p', "padded", Default = true)]
    public bool IsPadded { get; init; }

    internal virtual Image<Rgba32> UseToGenerateQrCode()
    {
        var qrCodeWriter = new BarcodeWriterPixelData
        {
            Format = BarcodeFormat.QR_CODE,
            Options = new QrCodeEncodingOptions
            {
                Height = Size,
                Width = Size,
                ErrorCorrection = QRCodeGenerator.ErrorCorrectionLevelFrom(Ecl),
                NoPadding = !IsPadded,
            }
        };
        var qrCode = qrCodeWriter.WriteAsImageSharp<Rgba32>(Data);

        return qrCode;
    }

    [CommandLine.Option('o', "output", Default = null)]
    public virtual string OutputPath
    {
        get => _outputPath ??= Path.GetFileNameWithoutExtension(Path.GetRandomFileName());
        init => _outputPath = value;
    }
    string? _outputPath;

    /// <summary>
    /// Intended to be called only by CommandLineParser framework
    /// </summary>
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public QRCodeParameters() { }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
}

[CommandLine.Verb("with-logo",
    isDefault: true,
    aliases: new string[] { "w/logo" })]
internal record QRCodeWithLogoParameters : QRCodeParameters
{
    [CommandLine.Value(index: 1, Required = true,
        HelpText = "Local path referring to an image that will be at the center of the resulting QR code.")]
    public string Logo { get; init; }

    [CommandLine.Option("logo-size", Default = 150)]
    public int LogoSize { get; init; }

    internal override Image<Rgba32> UseToGenerateQrCode()
    {
        var qrCode = base.UseToGenerateQrCode();
        DrawLogoOn(qrCode);
        return qrCode;


        void DrawLogoOn(Image<Rgba32> qrCode)
        {
            using var logo = Image.Load(Logo);
            logo.Mutate(_ => _.Resize(new Size(LogoSize < Size / 3 ? LogoSize : Size / 3)));
            var center = (Size - logo.Width) / 2;
            qrCode.Mutate(_ => _.DrawImage(logo, new Point(center, center), opacity: 1));
        }
    }

    /// <summary>
    /// Intended to be called only by CommandLineParser framework
    /// </summary>
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public QRCodeWithLogoParameters() { }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
}
