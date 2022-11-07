using System.Net;
using System.Net.Http.Json;
using System.Security;

namespace Transport.Upload._3DModelsUpload.CGTrader.Models;

public class CGTraderNetworkCredential : NetworkCredential
{
    public bool RememberMe;
    internal JsonContent AsJson => _asJson ??= JsonContent.Create(
        new
        {
            user = new
            {
                login = UserName,
                password = Password,
                remember_me = RememberMe.ToString().ToLower()
            },
            location = "/users/login"
        });
    JsonContent? _asJson;


    public CGTraderNetworkCredential(string? userName, string? password, bool rememberMe)
        : base(userName, password, CGTraderAddress.Domain)
    {
        RememberMe = rememberMe;
    }

    public CGTraderNetworkCredential(string? userName, SecureString password, bool rememberMe)
        : base(userName, password, CGTraderAddress.Domain)
    {
        RememberMe = rememberMe;
    }
}
