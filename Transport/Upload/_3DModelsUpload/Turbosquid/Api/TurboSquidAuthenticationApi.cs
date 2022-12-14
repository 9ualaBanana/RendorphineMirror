using NLog;
using NodeToUI;
using NodeToUI.Requests;
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
        if (_IsRedirectTo2FA(loginResponse))
        {
            credential._CsrfToken = CsrfToken._ParseFromMetaTag(await loginResponse.Content.ReadAsStringAsync(cancellationToken));
            string verificationCode = (await NodeGui.Request<string>(new InputRequest("TODO: we send you mesag to email please respond"), cancellationToken)).Result;
            await _SendVerificationCodeFromEmailAsync(verificationCode, credential, cancellationToken);
        }
    }

    async Task<HttpResponseMessage> _LoginAsyncCore(TurboSquidNetworkCredential credential, CancellationToken cancellationToken) =>
        (await _httpClient.PostAsync(
            (this as IBaseAddressProvider).Endpoint("/users/sign_in?locale=en"),
            credential._ToLoginMultipartFormData(),
            cancellationToken))
        .EnsureSuccessStatusCode();

    async Task _SendVerificationCodeFromEmailAsync(string verificationCode, TurboSquidNetworkCredential credential, CancellationToken cancellationToken) =>
        (await _httpClient.PostAsync(
            (this as IBaseAddressProvider).Endpoint("/users/two_factor_authentication.user?locale=en"),
            credential._To2FAMultipartFormDataWith(verificationCode),
            cancellationToken))
        .EnsureSuccessStatusCode();


    #region Helpers

    bool _IsRedirectTo2FA(HttpResponseMessage response) =>
        response.RequestMessage!.RequestUri!.AbsoluteUri.StartsWith((this as IBaseAddressProvider).Endpoint("/users/two_factor_authentication"));

    #endregion
}
