namespace ArenaService.Tests.Auth;

using System;
using System.Security.Cryptography;
using System.Text;
using Libplanet.Common;
using Libplanet.Crypto;
using Newtonsoft.Json;

public class JwtCreator
{
    public static string CreateJwt(PrivateKey privateKey, string role = "user")
    {
        var payload = new
        {
            iss = "user",
            avt_adr = "c106714d1bf09c37bcff24362eea5508d925f37a",
            sub = privateKey.PublicKey.ToHex(true),
            role,
            iat = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            exp = DateTimeOffset.UtcNow.AddYears(1).ToUnixTimeSeconds()
        };

        string payloadJson = JsonConvert.SerializeObject(payload);
        byte[] payloadBytes = Encoding.UTF8.GetBytes(payloadJson);

        byte[] signature = privateKey.Sign(payloadBytes);

        string signatureBase64 = Convert.ToBase64String(signature);

        string headerJson = JsonConvert.SerializeObject(new { alg = "ES256K", typ = "JWT" });
        string headerBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(headerJson));
        string payloadBase64 = Convert.ToBase64String(payloadBytes);

        return $"{headerBase64}.{payloadBase64}.{signatureBase64}";
    }
}
