using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;
using Libplanet.Crypto;
using Microsoft.IdentityModel.Tokens;

namespace ArenaService.Shared.Jwt;

public class BattleTokenValidator
{
    private readonly RSA _rsa;

    public BattleTokenValidator(string publicKeyBase64)
    {
        byte[] publicKeyBytes = Convert.FromBase64String(publicKeyBase64);
        string publicKeyPem = Encoding.UTF8.GetString(publicKeyBytes);

        _rsa = RSA.Create();
        _rsa.ImportFromPem(publicKeyPem.ToCharArray());
    }

    public bool ValidateBattleToken(string token, int battleId)
    {
        try
        {
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = "planetarium arena service",

                ValidateAudience = true,
                ValidAudience = "NineChronicles headless",

                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,

                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new RsaSecurityKey(_rsa)
            };

            var handler = new JwtSecurityTokenHandler();
            handler.ValidateToken(token, validationParameters, out SecurityToken validated);
            var validatedToken = (JwtSecurityToken)validated;

            bool isPayloadValid = Convert.ToInt32(validatedToken.Payload["bid"]) == battleId;

            return isPayloadValid;
        }
        catch (Exception)
        {
            return false;
        }
    }
    public bool TryValidateBattleToken(string token, out JwtPayload payload)
    {
        try
        {
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = "planetarium arena service",

                ValidateAudience = true,
                ValidAudience = "NineChronicles headless",

                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,

                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new RsaSecurityKey(_rsa)
            };

            var handler = new JwtSecurityTokenHandler();
            handler.ValidateToken(token, validationParameters, out SecurityToken validated);
            var validatedToken = (JwtSecurityToken)validated;

            payload = validatedToken.Payload;
            return true;
        }
        catch (Exception)
        {
            payload = null;
            return false;
        }
    }
}
