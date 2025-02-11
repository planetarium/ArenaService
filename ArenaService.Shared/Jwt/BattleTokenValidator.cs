using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using Libplanet.Crypto;
using Microsoft.IdentityModel.Tokens;

namespace ArenaService.Shared.Jwt;

public class BattleTokenValidator
{
    private readonly string _publicKey;

    public BattleTokenValidator(string publicKeyPem)
    {
        _publicKey = publicKeyPem;
    }

    public bool ValidateBattleToken(string token, int battleId)
    {
        try
        {
            var rsa = RSA.Create();
            rsa.ImportFromPem(_publicKey.ToCharArray());

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = "planetarium arena service",

                ValidateAudience = true,
                ValidAudience = "NineChronicles headless",

                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,

                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new RsaSecurityKey(rsa)
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
}
