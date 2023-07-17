using ZXing;
using ZXing.ImageSharp.Rendering;
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
    public char ErrorCorrectionLevel { get; init; }

    [CommandLine.Option('p', "padded", Default = false)]
    public bool IsPadded { get; init; }

    internal virtual Image<Rgba32> UseToGenerateQrCode()
        => new QRCodeGenerator(this).WriteAsImageSharp<Rgba32>(Data);

    [CommandLine.Option('o', "output", Default = null)]
    public virtual string OutputPath
    {
        get => _outputPath ??= Path.GetFileNameWithoutExtension(Path.GetRandomFileName());
        init => _outputPath = value;
    }
    string? _outputPath;

    internal QrCodeEncodingOptions ToOptions() => new()
    {
        Height = Size,
        Width = Size,
        ErrorCorrection = new ErrorCorrectionLevel(ErrorCorrectionLevel).Value,
        NoPadding = !IsPadded,
    };

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
        var qrCodeGenerator = new QRCodeGenerator(this);
        var encodedData = qrCodeGenerator.Encode(Data);
        var qrCode = new ImageSharpRenderer<Rgba32>().Render(encodedData, BarcodeFormat.QR_CODE, Data);
        var actuaQrCodelSize = IsPadded ? Size : encodedData.Width;

        using var logo = Image.Load(Logo);
        var maxLogoSize = actuaQrCodelSize / 3; 
        var actualLogoSize = LogoSize < maxLogoSize ? LogoSize : maxLogoSize;
        logo.Mutate(_ => _.Resize(new Size(actualLogoSize)));
        // We minus logo.Width because we need the top left point from which the logo will be drawn.
        var center = (actuaQrCodelSize - actualLogoSize) / 2;
        qrCode.Mutate(_ => _.DrawImage(logo, new Point(center, center), opacity: 1));

        return qrCode;
    }

    /// <inheritdoc cref="QRCodeParameters()"/>
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public QRCodeWithLogoParameters() { }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
}
