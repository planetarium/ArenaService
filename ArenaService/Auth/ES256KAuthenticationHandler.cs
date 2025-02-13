namespace ArenaService.Auth;

using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using ArenaService.Utils;
using Libplanet.Crypto;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

public class ES256KAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public ES256KAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock
    )
        : base(options, logger, encoder, clock) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var authorizationHeader = GetAuthorizationHeader();
        if (string.IsNullOrEmpty(authorizationHeader))
        {
            return Task.FromResult(
                AuthenticateResult.Fail("Missing or invalid Authorization header.")
            );
        }

        var token = GetTokenFromHeader(authorizationHeader);
        if (
            !TryExtractTokenParts(
                token,
                out var payloadBytes,
                out var signature,
                out var publicKey,
                out var address,
                out var avtAdr,
                out var role
            )
        )
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid token."));
        }

        if (!ValidateSignature(payloadBytes, signature, publicKey))
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid signature."));
        }

        if (role == "Admin" && !ValidateAdminKey(publicKey))
        {
            return Task.FromResult(AuthenticateResult.Fail("Unauthorized Admin key."));
        }

        return Task.FromResult(
            AuthenticateResult.Success(CreateAuthenticationTicket(publicKey, address, avtAdr, role))
        );
    }

    private string GetAuthorizationHeader()
    {
        return Request.Headers["Authorization"].ToString();
    }

    private string GetTokenFromHeader(string authorizationHeader)
    {
        return authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? authorizationHeader.Substring("Bearer ".Length).Trim()
            : string.Empty;
    }

    private bool TryExtractTokenParts(
        string jwt,
        out byte[] payloadBytes,
        out byte[] signature,
        out string publicKey,
        out string address,
        out string avtAdr,
        out string role
    )
    {
        payloadBytes = null;
        signature = null;
        publicKey = null;
        address = null;
        avtAdr = null;
        role = null;

        var parts = jwt.Split('.');
        if (parts.Length != 3)
        {
            return false;
        }

        try
        {
            payloadBytes = Convert.FromBase64String(parts[1]);
            signature = Convert.FromBase64String(parts[2]);

            var payload = Encoding.UTF8.GetString(payloadBytes);
            var payloadJson = JsonConvert.DeserializeObject<Dictionary<string, string>>(payload);

            if (
                payloadJson == null
                || !payloadJson.TryGetValue("sub", out publicKey)
                || !payloadJson.TryGetValue("avt_adr", out avtAdr)
                || !payloadJson.TryGetValue("role", out role)
            )
            {
                return false;
            }

            var pubKey = PublicKey.FromHex(publicKey);
            address = pubKey.Address.ToString();

            try
            {
                if (
                    !AvatarAddressValidator.CheckSignerContainsAvatar(
                        pubKey.Address,
                        new Address(avtAdr)
                    )
                )
                {
                    return false;
                }
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to parse token parts.");
            return false;
        }
    }

    private bool ValidateSignature(byte[] payloadBytes, byte[] signature, string publicKey)
    {
        try
        {
            var pubKey = PublicKey.FromHex(publicKey);

            return pubKey.Verify(payloadBytes, signature);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to validate signature.");
            return false;
        }
    }

    private bool ValidateAdminKey(string publicKey)
    {
        var allowedAdminKey = Environment.GetEnvironmentVariable("ALLOWED_ADMIN_PUBLIC_KEY");
        if (string.IsNullOrEmpty(allowedAdminKey))
        {
            Logger.LogWarning("ALLOWED_ADMIN_PUBLIC_KEY environment variable is not set.");
            return false;
        }

        try
        {
            var adminKey = PublicKey.FromHex(allowedAdminKey);
            return adminKey.ToHex(true) == publicKey;
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to validate admin key.");
            return false;
        }
    }

    private AuthenticationTicket CreateAuthenticationTicket(
        string publicKey,
        string address,
        string avtAdr,
        string role
    )
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.Role, role),
            new Claim("public_key", publicKey),
            new Claim("address", address),
            new Claim("avatar_address", avtAdr)
        };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        return new AuthenticationTicket(principal, Scheme.Name);
    }
}
