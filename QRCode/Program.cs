using CommandLine;
using QRCode;

CommandLine.Parser.Default
    .ParseArguments<QRCodeWithLogoParameters, QRCodeParameters>(args)
    .MapResult(
        (QRCodeWithLogoParameters _) => QRCodeGenerator.Execute(_),
        (QRCodeParameters _) => QRCodeGenerator.Execute(_),
        QRCodeGenerator.ExecuteErrorHandler
    );
