using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace ArenaService.BackOffice.Authentication;

public static class ApiKeyAuthenticationDefaults
{
    public const string AuthenticationScheme = "ApiKey";
    public const string HeaderName = "X-API-Key";
    public const string ConfigurationKey = "ARENA_API_KEY";
}

/// <summary>
/// Authenticates API requests with a shared secret supplied in the <c>X-API-Key</c> header
/// and compared against the <c>ARENA_API_KEY</c> configuration value.
/// The Blazor UI keeps using Google OAuth; only controllers opt into this scheme.
/// </summary>
public class ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly IConfiguration _configuration;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IConfiguration configuration
    )
        : base(options, logger, encoder)
    {
        _configuration = configuration;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(ApiKeyAuthenticationDefaults.HeaderName, out var provided))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var providedApiKey = provided.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(providedApiKey))
        {
            return Task.FromResult(AuthenticateResult.Fail("API Key is empty."));
        }

        var configuredApiKey = _configuration[ApiKeyAuthenticationDefaults.ConfigurationKey];
        if (string.IsNullOrWhiteSpace(configuredApiKey))
        {
            Logger.LogError(
                "{Key} is not configured; API key authentication is disabled.",
                ApiKeyAuthenticationDefaults.ConfigurationKey
            );
            return Task.FromResult(AuthenticateResult.Fail("API Key authentication is not configured."));
        }

        if (!string.Equals(providedApiKey, configuredApiKey, StringComparison.Ordinal))
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid API Key."));
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "ApiUser"),
            new Claim(
                ClaimTypes.AuthenticationMethod,
                ApiKeyAuthenticationDefaults.AuthenticationScheme
            )
        };
        var identity = new ClaimsIdentity(claims, ApiKeyAuthenticationDefaults.AuthenticationScheme);
        var ticket = new AuthenticationTicket(
            new ClaimsPrincipal(identity),
            ApiKeyAuthenticationDefaults.AuthenticationScheme
        );

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
