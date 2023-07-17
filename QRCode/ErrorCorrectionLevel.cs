namespace QRCode;

internal class ErrorCorrectionLevel
{
    readonly internal ZXing.QrCode.Internal.ErrorCorrectionLevel Value;

    public ErrorCorrectionLevel(char symbol)
    {
        var normalizedSymbol = char.ToUpperInvariant(symbol);
        Value = normalizedSymbol switch
        {
            'L' => ZXing.QrCode.Internal.ErrorCorrectionLevel.L,
            'M' => ZXing.QrCode.Internal.ErrorCorrectionLevel.M,
            'Q' => ZXing.QrCode.Internal.ErrorCorrectionLevel.Q,
            'H' => ZXing.QrCode.Internal.ErrorCorrectionLevel.H,
            _ => throw new ImageProcessingException($"Unknown {nameof(ErrorCorrectionLevel)} - '{normalizedSymbol}'")
        };
    }
}
