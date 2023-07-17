using CommandLine;
using ZXing;

namespace QRCode;

class QRCodeGenerator : BarcodeWriterPixelData
{
    public QRCodeGenerator(QRCodeParameters parameters)
    {
        Format = BarcodeFormat.QR_CODE;
        Options = parameters.ToOptions();
    }

    internal static string Execute(QRCodeParameters _)
    {
        // Path.GetFullPath is invoked here from client code because this method doesn't consider
        // current working directory when invoked from getter in objects constructed by CommandLineParser framework.
        var qrCodePath = Path.GetFullPath(Path.ChangeExtension(_.OutputPath, "png"));
        
        _.UseToGenerateQrCode().SaveAsJpeg(qrCodePath);

        Console.WriteLine(qrCodePath);
        return qrCodePath;
    }

    internal static string ExecuteErrorHandler(IEnumerable<Error> errors)
    {
        if (HandleDefaultOptions()) return string.Empty;

        string errorNames = string.Join('\n', errors.Select(_ => _.Tag));
        string errorMessage = $"""
        QR code generation failed.
        {errorNames}
        """;
        throw new ImageProcessingException(errorMessage);


        bool HandleDefaultOptions()
            => errors.SingleOrDefault() is Error defaultOption && defaultOption.Tag switch
            {
                ErrorType.HelpVerbRequestedError or ErrorType.VersionRequestedError => true,
                _ => false
            };
    }
}
