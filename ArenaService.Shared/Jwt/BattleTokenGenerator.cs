using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;

namespace ArenaService.Shared.Jwt;

public class BattleTokenGenerator
{
    private readonly string _privateKey;

    public BattleTokenGenerator(string privateKeyPem)
    {
        _privateKey = privateKeyPem;
    }

    public string GenerateBattleToken(int battleId)
    {
        var rsa = RSA.Create();
        rsa.ImportFromPem(_privateKey.ToCharArray());

        var securityKey = new RsaSecurityKey(rsa);
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.RsaSha256);

        var payload = new JwtPayload
        {
            { "iss", "planetarium arena service" },
            { "bid", battleId },
            { "iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds() },
            { "exp", DateTimeOffset.UtcNow.AddHours(6).ToUnixTimeSeconds() },
            { "aud", "NineChronicles headless" }
        };

        var header = new JwtHeader(credentials);
        var token = new JwtSecurityToken(header, payload);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
