using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using ArenaService.Jwt;
using Libplanet.Crypto;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace ArenaService.Tests.Jwt;

public class BattleTokenGeneratorTests
{
    private BattleTokenGenerator _tokenGenerator;
    private string _privateKey;
    private string _publicKey;

    public BattleTokenGeneratorTests()
    {
        _privateKey = """
-----BEGIN RSA PRIVATE KEY-----
MIIEowIBAAKCAQEA1czxlR7Pzxxfx/B1QdWgbT7LYJ8d9mMhccJs7JU72vxYlXLm
8QNrLYLZl6y9fLLHQ93OWfrXb00yM+FeZNl4VSF3dmvpX7te9gXlfOyRpfTGdNlP
2heCo64ZR1/JJq+osj8GYBb+LxGxa5Cp81igUqIz0byyVZfrIgDoRnYuCiBZkQFh
776a2pqI0FiyBO9Z/R8qbYtuai+HsxXyucmxYtz+81C7glY81uH/ojoewaE/W5Bw
U6OpwtIMZIL0D77KwSwro46DMGSroS9E41GnBJoCYgAEyVOKysiYDZzmI+GgFEJj
gQmLjdIUBF7if90pUawnmpjXL+v4iGWZerlv5QIDAQABAoIBAACiOiAVRovoW19S
FuLiUXapbjxj1Fin3BBEgYKlAZepUlDlFYqm1jE9F0yECk69j+ojVOp+3BccHTc6
poc5fsoPzpUXBkrOmR41D8RSRi89/b2YbExq7+vwxunnfobjE+atWaU8xDWbAZHe
TKaoP1dnXCx1uPiYee2wn3+f94x3oTFKtfdT1cf64Blow82jVrfacaxD/1nXfXT6
Xvr0NlMVZoj2XTUF4JKcMWMmVh5gPU1zD4VCBWnw/uFnWgIncXgDr2189cBw9WwM
2UW9NOOImJw9mKIeDGRgivjEzESV93dEWSyYZSMBvehfZ0mRMK24Z7QK7TDj3k5/
WXcgLLkCgYEA6r215xMDn5f162gF49puv0ksgahhZKAF8iyH5HLb4WB7zKqMRa54
VOZ5M/Zxb2L7nDD7ioPIDRa+OuSKbidhaKwqDrRL//osMiozOwl/CsWMLNFnhL9K
dmRDq02YxlDNhqKOf42Kpn8CN4V8j7N2nj1mgtVtZFyiB0rYyRagMv0CgYEA6Sm+
dI1ClLVsK7GlOQsD7H0KrRtC7K21N1atvXUhgIvNb78uU7YaZyHPQ85pxdLO6mmw
8UDP2qmPmvCsh+CFarRFEn4Rw5bO5NxvQhMttKPkdedN4EsIFCtMyMGjNg1qWl8q
6E7/aaKTWd+rFzPibjQe3dZ17kombfUwnF37yQkCgYBrvLyDHM/57KXa8HhbloJj
2vLJY32n0GZKOzP3ntvaOg0350LQLH5gARO5zK9NfzGaA0U/0rH7h+exYflDC2IC
x5nZ+9gx2SF1uLagrwAW8ooee9G2NJG5etUwB0JKKwXZeDxMwKrVNc/Pqb18utKD
WAz3mXtGp9lZ3XlX+cF3BQKBgEXEYeLKKfQJXTatzzyEWUY4HCu0DpB3YdQPMamR
FNc7/drEH/6YbMoTScuGRgwViiiGO5XUyN0rA3dfMKDvw7wr+McRxgr6YyoD856X
0oNMzx2geqL0kJRIaI9hsY6I8Rvdgh9FFBPtu52W2cD1m3lSSzIh6+PDeEBKr6L6
VE7RAoGBAJsb8ygi6OsotoXg1/fOH3OLIc4hqQGEraZy2kaQV2+h6g0zPF49QPYQ
AEwt7MqXFQsfhNmq4mPCN54tMpteE8Qwv3IdsK3oSfNSXaGOslqtUYasfocrk3A7
BmEblof6UhVuZjzXD7mgdWo1XunjdN6U8kawI3ddtVoCLUD8fxNF
-----END RSA PRIVATE KEY-----
""";

        _publicKey = """
-----BEGIN RSA PUBLIC KEY-----
MIIBCgKCAQEA1czxlR7Pzxxfx/B1QdWgbT7LYJ8d9mMhccJs7JU72vxYlXLm8QNr
LYLZl6y9fLLHQ93OWfrXb00yM+FeZNl4VSF3dmvpX7te9gXlfOyRpfTGdNlP2heC
o64ZR1/JJq+osj8GYBb+LxGxa5Cp81igUqIz0byyVZfrIgDoRnYuCiBZkQFh776a
2pqI0FiyBO9Z/R8qbYtuai+HsxXyucmxYtz+81C7glY81uH/ojoewaE/W5BwU6Op
wtIMZIL0D77KwSwro46DMGSroS9E41GnBJoCYgAEyVOKysiYDZzmI+GgFEJjgQmL
jdIUBF7if90pUawnmpjXL+v4iGWZerlv5QIDAQAB
-----END RSA PUBLIC KEY-----
""";
        _tokenGenerator = new BattleTokenGenerator(_privateKey);
    }

    [Fact]
    public void GenerateBattleToken_ShouldCreateValidJwt()
    {
        int battleId = 5001;

        string token = _tokenGenerator.GenerateBattleToken(battleId);

        Assert.NotNull(token);
        Assert.NotEmpty(token);

        var handler = new JwtSecurityTokenHandler();
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

        handler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);
        var jwtToken = (JwtSecurityToken)validatedToken;

        Assert.Equal("planetarium arena service", jwtToken.Issuer);
        Assert.Equal("NineChronicles headless", jwtToken.Audiences.First());

        Assert.Equal(battleId, Convert.ToInt32(jwtToken.Payload["bid"]));

        long issuedAt = Convert.ToInt64(jwtToken.Payload["iat"]);
        long expiration = Convert.ToInt64(jwtToken.Payload["exp"]);
        Assert.Equal(expiration - issuedAt, 6 * 3600);
    }
}
