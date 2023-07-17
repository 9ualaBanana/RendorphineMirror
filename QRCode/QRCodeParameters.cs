using ZXing;
using ZXing.ImageSharp.Rendering;
using ZXing.QrCode;

namespace QRCode;

[CommandLine.Verb("without-logo",
    isDefault: false,
    aliases: new string[] { "w/ologo" })]
internal record QRCodeParameters
{
    [CommandLine.Value(index: 0, Required = true,
        MetaName = nameof(Data))]
    public string Data { get; init; }

    [CommandLine.Option('s', "size", Default = 500)]
    public int Size { get; init; }

    [CommandLine.Option("ecl", Default = 'Q',
        MetaValue = "L,M,Q,H")]
    public char ErrorCorrectionLevel { get; init; }

    [CommandLine.Option('p', "padded", Default = false)]
    public bool IsPadded { get; init; }

    internal virtual Image<Rgba32> UseToGenerateQrCode()
        => new QRCodeGenerator(this).WriteAsImageSharp<Rgba32>(Data);

    [CommandLine.Option('o', "output",
        HelpText = """
        (Default: <random>)
        When not specified, random file name with the extension will be appended to the current working directory.
        When directory is specified (i.e. path ending with directory separator), random file name with the extension will be appended to that directory.
        Extension specified as part of this option's value will be used unless overriden by --extension.
        """)]
    public virtual string OutputPath
    {
        get
        {
            if (_outputPath is null)
                _outputPath = RandomFileName();
            else if (Path.EndsInDirectorySeparator(_outputPath))
                _outputPath = Path.Combine(_outputPath, RandomFileName());
        
            return _outputPath;
                
            static string RandomFileName() => Path.GetFileNameWithoutExtension(Path.GetRandomFileName());
        }
        init
        {
            if (ExtensionSpecifiedInOutputPath() is string extension)
                Extension = extension;
            _outputPath = OutputPathWithoutExtension();


            string? ExtensionSpecifiedInOutputPath()
                => Path.GetExtension(value) is string extension && !string.IsNullOrWhiteSpace(extension) ?
                extension : null;
            string OutputPathWithoutExtension() => Path.ChangeExtension(value, null);
        }
    }
    string? _outputPath;

    /// <remarks>
    /// Must follow <see cref="OutputPath"/> option to override the behavior defined there.
    /// Default extension is lazily computed inside the getter to prevent overriding the extension set explicitly.
    /// </remarks>
    [CommandLine.Option('x', "extension",
        HelpText = $"(Default: {DefaultExtension}) Specify an extension of the resulting file.")]
    public string Extension
    {
        get => _extension ?? DefaultExtension;
        init => _extension = value;
    }
    string? _extension;
    const string DefaultExtension = ".png";

    internal QrCodeEncodingOptions ToEncodingOptions() => new()
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
        MetaName = nameof(Logo),
        HelpText = "Local path referring to an image that will be at the center of the resulting QR code.")]
    public string Logo { get; init; }

    [CommandLine.Option("logo-size", Default = 150,
        HelpText = $"Desired logo size that will be capped to not exceed {nameof(MaxLogoToQRCodeRatio)} ({MaxLogoToQRCodeRatio})")]
    public int LogoSize { get; init; }

    internal override Image<Rgba32> UseToGenerateQrCode()
    {
        var qrCodeGenerator = new QRCodeGenerator(this);
        var encodedData = qrCodeGenerator.Encode(Data);
        var qrCode = new ImageSharpRenderer<Rgba32>().Render(encodedData, BarcodeFormat.QR_CODE, Data);
        var actuaQrCodelSize = IsPadded ? Size : encodedData.Width;

        using var logo = Image.Load(Logo);
        var maxLogoSize = actuaQrCodelSize / int.Parse(MaxLogoToQRCodeRatio); 
        var actualLogoSize = LogoSize < maxLogoSize ? LogoSize : maxLogoSize;
        logo.Mutate(_ => _.Resize(new Size(actualLogoSize)));
        // We minus logo.Width because we need the top left point from which the logo will be drawn.
        var center = (actuaQrCodelSize - actualLogoSize) / 2;
        qrCode.Mutate(_ => _.DrawImage(logo, new Point(center, center), opacity: 1));

        return qrCode;
    }
    const string MaxLogoToQRCodeRatio = "4";

    /// <inheritdoc cref="QRCodeParameters()"/>
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public QRCodeWithLogoParameters() { }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
}
