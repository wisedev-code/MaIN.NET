using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MaIN.Domain.Configuration.Vertex;

namespace MaIN.Services.Services.LLMService.Auth;

internal sealed class VertexTokenProvider
{
    private const string Scope = "https://www.googleapis.com/auth/cloud-platform";
    private const int TokenLifetimeSeconds = 3600;
    private const int RefreshBufferMinutes = 5;

    private readonly GoogleServiceAccountAuth _config;
    private readonly RSA _rsa;

    private string? _cachedToken;
    private DateTime _tokenExpiry = DateTime.MinValue;

    public VertexTokenProvider(GoogleServiceAccountAuth config)
    {
        _config = config;
        _rsa = RSA.Create();
        _rsa.ImportFromPem(config.PrivateKey.Replace("\\n", "\n"));
    }

    public async Task<string> GetAccessTokenAsync(HttpClient httpClient)
    {
        if (_cachedToken != null && DateTime.UtcNow < _tokenExpiry)
            return _cachedToken;

        var jwt = BuildSignedJwt();
        var token = await ExchangeJwtForTokenAsync(httpClient, jwt);

        _cachedToken = token.AccessToken
                       ?? throw new InvalidOperationException("Token response missing access_token.");
        _tokenExpiry = DateTime.UtcNow.AddSeconds(token.ExpiresIn).AddMinutes(-RefreshBufferMinutes);

        return _cachedToken;
    }

    private string BuildSignedJwt()
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        var header = Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(new
        {
            alg = "RS256",
            typ = "JWT"
        }));

        var payload = Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(new
        {
            iss = _config.ClientEmail,
            scope = Scope,
            aud = _config.TokenUri,
            iat = now,
            exp = now + TokenLifetimeSeconds
        }));

        var dataToSign = Encoding.ASCII.GetBytes($"{header}.{payload}");
        var signature = _rsa.SignData(dataToSign, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        return $"{header}.{payload}.{Base64UrlEncode(signature)}";
    }

    private async Task<TokenResponse> ExchangeJwtForTokenAsync(HttpClient httpClient, string jwt)
    {
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "urn:ietf:params:oauth:grant-type:jwt-bearer",
            ["assertion"] = jwt
        });

        using var response = await httpClient.PostAsync(_config.TokenUri, content);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException(
                $"Vertex AI token exchange failed ({response.StatusCode}): {error}");
        }

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<TokenResponse>(json)
               ?? throw new InvalidOperationException("Failed to parse Vertex AI token response.");
    }

    private static string Base64UrlEncode(byte[] data)
        => Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private sealed class TokenResponse
    {
        [JsonPropertyName("access_token")] public string? AccessToken { get; set; }
        [JsonPropertyName("expires_in")] public int ExpiresIn { get; set; }
    }
}
