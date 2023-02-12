namespace Telegram.Security.Authentication;

public class MPlusAuthenticationAgent
{
    readonly HttpClient _httpClient;

    public MPlusAuthenticationAgent(HttpClient httpClient)
	{
        _httpClient = httpClient;
        _httpClient.BaseAddress = new("https://tasks.microstock.plus/rphtaskmgr/");
    }

    internal async Task<MPlusIdentity?> TryLogInAsyncUsing(CredentialsFromChat credentialsFromChat)
    {
        var credentialsForm = new FormUrlEncodedContent(new Dictionary<string, string>()
        {
            ["email"] = credentialsFromChat.UserName,
            ["password"] = credentialsFromChat.Password,
            ["guid"] = Guid.NewGuid().ToString()
        });
        var response = await _httpClient.PostAsync("login", credentialsForm);
        return (await response.GetJsonIfSuccessfulAsync()).ToObject<MPlusIdentity>();
    }
}
