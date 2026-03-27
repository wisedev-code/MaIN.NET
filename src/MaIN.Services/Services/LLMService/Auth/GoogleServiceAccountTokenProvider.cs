using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using MaIN.Domain.Configuration.Vertex;

namespace MaIN.Services.Services.LLMService.Auth;

internal sealed class GoogleServiceAccountTokenProvider : IDisposable
{
    private const string Scope = "https://www.googleapis.com/auth/cloud-platform";
    private const int TokenLifetimeSeconds = 3600;
    private const int RefreshBufferMinutes = 5;

    private readonly GoogleServiceAccountConfig _config;
    private readonly RSA _rsa;

    private static readonly ConcurrentDictionary<string, CachedToken> TokenCache = new();
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> RefreshLocks = new();

    public GoogleServiceAccountTokenProvider(GoogleServiceAccountConfig config)
    {
        _config = config;
        _rsa = RSA.Create();
        _rsa.ImportFromPem(config.PrivateKey.Replace("\\n", "\n"));
    }

    public async Task<string> GetAccessTokenAsync(HttpClient httpClient)
    {
        var email = _config.ClientEmail;

        if (TokenCache.TryGetValue(email, out var cached) && !cached.IsExpired)
            return cached.Token;

        var refreshLock = RefreshLocks.GetOrAdd(email, _ => new SemaphoreSlim(1, 1));
        await refreshLock.WaitAsync();
        try
        {
            // Double-check after acquiring lock
            if (TokenCache.TryGetValue(email, out cached) && !cached.IsExpired)
                return cached.Token;

            var jwt = BuildSignedJwt();
            var token = await ExchangeJwtForTokenAsync(httpClient, jwt);

            var accessToken = token.AccessToken
                              ?? throw new InvalidOperationException("Vertex AI token response missing access_token.");
            var expiry = DateTime.UtcNow.AddSeconds(token.ExpiresIn).AddMinutes(-RefreshBufferMinutes);

            TokenCache[email] = new CachedToken(accessToken, expiry);
            return accessToken;
        }
        finally
        {
            refreshLock.Release();
        }
    }

    private string BuildSignedJwt()
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        var header = Base64UrlEncode(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(new
        {
            alg = "RS256",
            typ = "JWT"
        }));

        var payload = Base64UrlEncode(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(new
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

    private static async Task<TokenResponse> ExchangeJwtForTokenAsync(HttpClient httpClient, string jwt)
    {
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "urn:ietf:params:oauth:grant-type:jwt-bearer",
            ["assertion"] = jwt
        });

        using var response = await httpClient.PostAsync("https://oauth2.googleapis.com/token", content);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException(
                $"Vertex AI token exchange failed ({response.StatusCode}): {error}");
        }

        return await response.Content.ReadFromJsonAsync<TokenResponse>()
               ?? throw new InvalidOperationException("Failed to parse Vertex AI token response.");
    }

    public void Dispose() => _rsa.Dispose();

    private static string Base64UrlEncode(byte[] data)
        => Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private sealed record CachedToken(string Token, DateTime Expiry)
    {
        public bool IsExpired => DateTime.UtcNow >= Expiry;
    }

    private sealed class TokenResponse
    {
        [JsonPropertyName("access_token")] public string? AccessToken { get; set; }
        [JsonPropertyName("expires_in")] public int ExpiresIn { get; set; }
    }
}
