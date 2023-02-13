using Telegram.Models;

namespace Telegram.Security.Authentication;

public class MPlusAuthenticationClient
{
    readonly MPlusClient _mPlusClient;

    public MPlusAuthenticationClient(MPlusClient mPlusClient)
	{
        _mPlusClient = mPlusClient;
    }

    internal async Task<MPlusIdentity?> TryLogInAsyncUsing(CredentialsFromChat credentialsFromChat)
        => await _mPlusClient.TryLogInAsyncUsing(credentialsFromChat.UserName, credentialsFromChat.Password);
}
