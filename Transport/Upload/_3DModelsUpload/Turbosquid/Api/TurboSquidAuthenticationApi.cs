using NLog;
using Transport.Models;
using Transport.Upload._3DModelsUpload.Turbosquid.Network.Authenticity;

namespace Transport.Upload._3DModelsUpload.Turbosquid.Api;

internal class TurboSquidAuthenticationApi : IBaseAddressProvider
{
    readonly ILogger _logger = LogManager.GetCurrentClassLogger();

    readonly HttpClient _httpClient;

    public string BaseAddress => "https://auth.turbosquid.com/";

    internal TurboSquidAuthenticationApi(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    internal async Task _LoginAsync(TurboSquidNetworkCredential credential, CancellationToken cancellationToken)
    {
        try
        {
            await __LoginAsync(credential, cancellationToken);
            _logger.Debug("{User} is successfully logged in.", credential.UserName);
        }
        catch (Exception ex)
        {
            string errorMessage = $"Login attempt for {credential.UserName} was unsuccessful.";
            _logger.Error(ex, errorMessage);
            throw new Exception(errorMessage, ex);
        }
    }

    async Task __LoginAsync(TurboSquidNetworkCredential credential, CancellationToken cancellationToken)
    {
        var loginResponse = await _LoginAsyncCore(credential, cancellationToken);
        if (loginResponse.RequestMessage!.RequestUri!.AbsoluteUri.StartsWith((this as IBaseAddressProvider).Endpoint("/users/two_factor_authentication")))
        {
            // Prompt user for verification code sent to his email via GUI.
            // Submit the form received from _LoginAsyncCore call with that verification code by inserting it after <input type="text" name="code" id="code" value=".
        }
    }

    // If this call redirects to https://auth.turbosquid.com/users/two_factor_authentication?locale=en prompt user for email verificatoin code.
    async Task<HttpResponseMessage> _LoginAsyncCore(TurboSquidNetworkCredential credential, CancellationToken cancellationToken) =>
        (await _httpClient.PostAsync(
            (this as IBaseAddressProvider).Endpoint("/users/sign_in?locale=en"),
            credential._AsMultipartFormData,
            cancellationToken))
        .EnsureSuccessStatusCode();
}
