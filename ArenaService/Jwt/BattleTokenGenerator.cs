using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace ArenaService.Jwt;

public class BattleTokenGenerator
{
    private readonly RSA _rsa;

    public BattleTokenGenerator(string privateKeyBase64)
    {
        byte[] privateKeyBytes = Convert.FromBase64String(privateKeyBase64);
        string privateKeyPem = Encoding.UTF8.GetString(privateKeyBytes);

        _rsa = RSA.Create();
        _rsa.ImportFromPem(privateKeyPem.ToCharArray());
    }

    public string GenerateBattleToken(int battleId)
    {
        var securityKey = new RsaSecurityKey(_rsa);
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
