using IronBarCode;

var resourceArg = args.FirstOrDefault();
ArgumentNullException.ThrowIfNull(resourceArg, nameof(resourceArg));
var resource = new Uri(resourceArg, UriKind.Absolute);

var logoArg = args.ElementAtOrDefault(1);
ArgumentNullException.ThrowIfNull(logoArg, nameof(logoArg));
var logo = new Uri(logoArg, UriKind.Absolute);

var qrCode = QRCodeWriter.CreateQrCodeWithLogo(resource.AbsoluteUri, new QRCodeLogo(logo));
