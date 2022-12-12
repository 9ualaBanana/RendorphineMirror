﻿namespace Transport.Upload._3DModelsUpload.Turbosquid.Network.Authenticity;

internal static class TurboSquidCaptchaVerifiedToken
{
    internal static ForeignThreadValue<string> _ServerResponse = new(false);

    internal static string _Parse(string html)
    {
        return html.Split('"', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)[3];
    }
}