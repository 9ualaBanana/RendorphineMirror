using System.Net;
using System.Security;

namespace _3DProductsPublish.CGTrader.Network;

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
