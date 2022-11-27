using System.Net;
using System.Security;

namespace Transport.Upload._3DModelsUpload.CGTrader.Network;

public sealed class CGTraderNetworkCredential : NetworkCredential
{
    internal string _RememberMe => RememberMe ? "on" : "off";
    public readonly bool RememberMe;


    public CGTraderNetworkCredential(string? userName, string? password, bool rememberMe)
        : base(userName, password, CGTraderUri.Domain)
    {
        RememberMe = rememberMe;
    }

    public CGTraderNetworkCredential(string? userName, SecureString password, bool rememberMe)
        : base(userName, password, CGTraderUri.Domain)
    {
        RememberMe = rememberMe;
    }
}
