global using SixLabors.ImageSharp;
global using SixLabors.ImageSharp.PixelFormats;
global using SixLabors.ImageSharp.Processing;
using CommandLine;
using QRCode;

CommandLine.Parser.Default
    .ParseArguments<QRCodeWithLogoParameters, QRCodeParameters>(args)
    .MapResult(
        (QRCodeWithLogoParameters _) => QRCodeGenerator.Execute(_),
        (QRCodeParameters _) => QRCodeGenerator.Execute(_),
        QRCodeGenerator.ExecuteErrorHandler
    );
