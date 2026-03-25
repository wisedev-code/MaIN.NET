using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MaIN.Domain.Configuration.Vertex;

namespace MaIN.Services.Services.LLMService.Auth;

internal sealed class GoogleServiceAccountTokenProvider
{
    private const string Scope = "https://www.googleapis.com/auth/cloud-platform";
    private const int TokenLifetimeSeconds = 3600;
    private const int RefreshBufferMinutes = 5;

    private readonly GoogleServiceAccountConfig _config;
    private readonly RSA _rsa;

    // Static cache shared across all VertexService instances (keyed by ClientEmail)
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, CachedToken> _tokenCache = new();
    private static readonly SemaphoreSlim _refreshLock = new(1, 1);

    public GoogleServiceAccountTokenProvider(GoogleServiceAccountConfig config)
    {
        _config = config;
        _rsa = RSA.Create();
        _rsa.ImportFromPem(config.PrivateKey.Replace("\\n", "\n"));
    }

    public async Task<string> GetAccessTokenAsync(HttpClient httpClient)
    {
        if (_tokenCache.TryGetValue(_config.ClientEmail, out var cached) && DateTime.UtcNow < cached.Expiry)
            return cached.Token;

        await _refreshLock.WaitAsync();
        try
        {
            // Double-check after acquiring lock
            if (_tokenCache.TryGetValue(_config.ClientEmail, out cached) && DateTime.UtcNow < cached.Expiry)
                return cached.Token;

            var jwt = BuildSignedJwt();
            var token = await ExchangeJwtForTokenAsync(httpClient, jwt);

            var accessToken = token.AccessToken
                              ?? throw new InvalidOperationException("Token response missing access_token.");
            var expiry = DateTime.UtcNow.AddSeconds(token.ExpiresIn).AddMinutes(-RefreshBufferMinutes);

            _tokenCache[_config.ClientEmail] = new CachedToken(accessToken, expiry);
            return accessToken;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private sealed record CachedToken(string Token, DateTime Expiry);

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
