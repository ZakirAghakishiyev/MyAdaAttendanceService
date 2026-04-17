using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net;
using Microsoft.AspNetCore.Http;

namespace MyAdaAttendanceService.Web;

/// <summary>
/// Ensures the auth service gets a bearer token. We prefer forwarding the
/// inbound request's Authorization header; if not present (e.g. startup sync),
/// we fall back to AuthService:ServiceAccessToken.
/// </summary>
public sealed class AuthServiceBearerForwardingHandler : DelegatingHandler
{
    private readonly IConfiguration _configuration;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private static readonly SemaphoreSlim RefreshLock = new(1, 1);
    private static string? _cachedServiceAccessToken;
    private static string? _cachedServiceRefreshToken;

    public AuthServiceBearerForwardingHandler(
        IConfiguration configuration,
        IHttpContextAccessor httpContextAccessor)
    {
        _configuration = configuration;
        _httpContextAccessor = httpContextAccessor;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // If something else already set Authorization, don't override.
        var hadAuthHeaderBeforeHandler = request.Headers.Authorization is not null;
        if (hadAuthHeaderBeforeHandler)
            return await base.SendAsync(request, cancellationToken);

        var incomingAuthHeader = _httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
        var bearerToken = ExtractBearerToken(incomingAuthHeader);

        if (string.IsNullOrWhiteSpace(bearerToken))
            bearerToken = GetCurrentServiceAccessToken();

        if (!string.IsNullOrWhiteSpace(bearerToken))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

        var response = await base.SendAsync(request, cancellationToken);

        // Do not attempt service-token refresh when request already had an inbound token.
        if (!response.StatusCode.Equals(HttpStatusCode.Unauthorized) || !string.IsNullOrWhiteSpace(incomingAuthHeader))
            return response;

        if (!await TryRefreshServiceTokenAsync(cancellationToken))
            return response;

        response.Dispose();
        var retryRequest = await CloneRequestAsync(request, cancellationToken);
        var refreshedAccessToken = GetCurrentServiceAccessToken();
        if (!string.IsNullOrWhiteSpace(refreshedAccessToken))
            retryRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", refreshedAccessToken);

        return await base.SendAsync(retryRequest, cancellationToken);
    }

    private string? GetCurrentServiceAccessToken()
    {
        if (!string.IsNullOrWhiteSpace(_cachedServiceAccessToken))
            return _cachedServiceAccessToken;

        var configured = ExtractBearerToken(_configuration["AuthService:ServiceAccessToken"]);
        if (!string.IsNullOrWhiteSpace(configured))
            _cachedServiceAccessToken = configured;

        return configured;
    }

    private string? GetCurrentServiceRefreshToken()
    {
        if (!string.IsNullOrWhiteSpace(_cachedServiceRefreshToken))
            return _cachedServiceRefreshToken;

        var configured = _configuration["AuthService:ServiceRefreshToken"]?.Trim();
        if (!string.IsNullOrWhiteSpace(configured))
            _cachedServiceRefreshToken = configured;

        return configured;
    }

    private async Task<bool> TryRefreshServiceTokenAsync(CancellationToken cancellationToken)
    {
        await RefreshLock.WaitAsync(cancellationToken);
        try
        {
            var refreshToken = GetCurrentServiceRefreshToken();
            if (string.IsNullOrWhiteSpace(refreshToken))
                return false;

            var refreshPath = _configuration["AuthService:RefreshPath"];
            if (string.IsNullOrWhiteSpace(refreshPath))
                refreshPath = "api/auth/refresh";

            var baseUrl = _configuration["AuthService:BaseUrl"] ?? "http://51.20.193.29:5000/";
            using var refreshClient = new HttpClient { BaseAddress = new Uri(baseUrl) };
            var refreshResponse = await refreshClient.PostAsJsonAsync(
                refreshPath,
                new { refreshToken },
                cancellationToken);

            if (!refreshResponse.IsSuccessStatusCode)
                return false;

            var refreshPayload = await refreshResponse.Content.ReadFromJsonAsync<AuthRefreshResponse>(cancellationToken: cancellationToken);
            if (refreshPayload is null || string.IsNullOrWhiteSpace(refreshPayload.AccessToken))
                return false;

            _cachedServiceAccessToken = ExtractBearerToken(refreshPayload.AccessToken);
            if (!string.IsNullOrWhiteSpace(refreshPayload.RefreshToken))
                _cachedServiceRefreshToken = refreshPayload.RefreshToken.Trim();

            return !string.IsNullOrWhiteSpace(_cachedServiceAccessToken);
        }
        catch
        {
            return false;
        }
        finally
        {
            RefreshLock.Release();
        }
    }

    private static async Task<HttpRequestMessage> CloneRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri);

        foreach (var header in request.Headers)
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);

        if (request.Content is not null)
        {
            var contentBytes = await request.Content.ReadAsByteArrayAsync(cancellationToken);
            clone.Content = new ByteArrayContent(contentBytes);
            foreach (var header in request.Content.Headers)
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        clone.Version = request.Version;
        return clone;
    }

    private static string? ExtractBearerToken(string? authHeader)
    {
        if (string.IsNullOrWhiteSpace(authHeader))
            return null;

        authHeader = authHeader.Trim();
        const string bearerPrefix = "Bearer ";
        if (authHeader.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase))
            return authHeader.Substring(bearerPrefix.Length).Trim();

        // Treat non-empty config value as raw token.
        return authHeader;
    }

    private sealed class AuthRefreshResponse
    {
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
    }
}

