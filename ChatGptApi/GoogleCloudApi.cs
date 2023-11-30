using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;

namespace ChatGptApi;

public class GoogleCloudApi
{
    readonly object CredsLock = new();

    readonly string CredsFile;
    readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromHours(1) };
    GoogleToken? Token;

    public GoogleCloudApi(string credsFile) => CredsFile = credsFile;

    public async Task<GoogleResult> SendRequest(GoogleRequest crequest)
    {
        var respjson = null as JObject;
        for (int i = 0; i < 2; i++)
        {
            lock (CredsLock)
            {
                if (Token is null || Token.ExpiresAt < DateTime.Now)
                    Token = GetGoogleKey(CredsFile).GetAwaiter().GetResult();
            }

            using var reqcontent = new StringContent(JsonConvert.SerializeObject(crequest)) { Headers = { ContentType = new("application/json", "utf-8") } };
            using var request = new HttpRequestMessage(HttpMethod.Post, "https://us-central1-aiplatform.googleapis.com/v1/projects/arcane-footing-294111/locations/us-central1/publishers/google/models/imagetext:predict") { Content = reqcontent, };

            request.Headers.Authorization = new(Token.TokenType, Token.AccessToken);

            Console.WriteLine("sending req " + i);
            using var response = await HttpClient.SendAsync(request);
            using var resp = new JsonTextReader(new StreamReader(await response.Content.ReadAsStreamAsync()));
            respjson = await JObject.LoadAsync(resp);
            Console.WriteLine("parsing req " + respjson);

            if (respjson.ContainsKey("error"))
            {
                var msg = respjson["error"]?["message"]?.Value<string>();
                if (msg?.Contains("Rate limit") == true || msg?.Contains("Quota exceeded") == true)
                {
                    try
                    {
                        const string tryagain = "Please try again in ";
                        var start = msg.IndexOf(tryagain, StringComparison.Ordinal) + tryagain.Length;
                        var end = msg.IndexOf("s.", start, StringComparison.Ordinal);

                        var wait = double.Parse(msg.AsSpan(start, end - start));
                        await Task.Delay(TimeSpan.FromSeconds(wait));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                        await Task.Delay(60_000);
                    }

                    return await SendRequest(crequest);
                }
                else
                {
                    Token = null;
                    await Task.Delay(60_000);
                    continue;
                }

                throw new Exception("Error in chat completion: " + msg);
            }

            break;
        }

        return respjson.ThrowIfNull().ToObject<GoogleResult>().ThrowIfNull();
    }

    static async Task<GoogleToken> GetGoogleKey(string credsFile)
    {
        Console.WriteLine("Getting new gkey...");

        var sakeyjson = JsonConvert.DeserializeObject<Dictionary<string, string>>(await File.ReadAllTextAsync(credsFile)).ThrowIfNull();
        var token = GenerateToken(sakeyjson);
        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenString = tokenHandler.WriteToken(token);
        var response = await RequestAccessToken(tokenString, sakeyjson["token_uri"]);

        return JsonConvert.DeserializeObject<GoogleToken>(response).ThrowIfNull();
    }
    static JwtSecurityToken GenerateToken(Dictionary<string, string> saKeyJson)
    {
        var base64key = saKeyJson["private_key"]
            .Replace("-----BEGIN PRIVATE KEY-----", "")
            .Replace("-----END PRIVATE KEY-----", "")
            .Trim();

        var rsakey = RSA.Create();
        rsakey.ImportPkcs8PrivateKey(Convert.FromBase64String(base64key), out _);

        var header = new JwtHeader(new SigningCredentials(new RsaSecurityKey(rsakey), "RS256"));
        var iat = (int) (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
        var payload = new JwtPayload()
        {
            { "iss", saKeyJson["client_email"]},
            { "iat", iat },
            { "aud", saKeyJson["token_uri"] },
            { "exp", iat + 60},
            { "scope", "https://www.googleapis.com/auth/cloud-platform" }
        };

        return new JwtSecurityToken(header, payload);
    }
    static async Task<string> RequestAccessToken(string tokenString, string uri)
    {
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "assertion", tokenString },
            { "grant_type", "urn:ietf:params:oauth:grant-type:jwt-bearer" }
        });

        using var response = Api.Default.Client.PostAsync(uri, content).Result;
        return await response.Content.ReadAsStringAsync();
    }


    record GoogleToken(
        [property: JsonProperty("access_token")][field: JsonProperty("access_token")] string AccessToken,
        [property: JsonProperty("expires_in")][field: JsonProperty("expires_in")] int ExpiresIn,
        [property: JsonProperty("token_type")][field: JsonProperty("token_type")] string TokenType
    )
    {
        public DateTime ExpiresAt { get; } = DateTime.Now.AddSeconds(ExpiresIn);
    }

#pragma warning disable IDE1006 // Properties should be uppercase
    public record GoogleRequest(GoogleRequest.GoogleRequestParameters parameters, IReadOnlyList<GoogleRequest.GoogleRequestInstances> instances)
    {
        public record GoogleRequestParameters(int sampleCount, string language);

        public record GoogleRequestInstances(GoogleRequestInstancesImage image);
        public record GoogleRequestInstancesImage(string bytesBase64Encoded);
    }
#pragma warning restore IDE1006

    public record GoogleResult(IReadOnlyList<string> Predictions);
}
