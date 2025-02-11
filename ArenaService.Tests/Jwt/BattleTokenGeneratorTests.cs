using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;
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
        _privateKey =
            "LS0tLS1CRUdJTiBSU0EgUFJJVkFURSBLRVktLS0tLQpNSUlFb3dJQkFBS0NBUUVBMWN6eGxSN1B6eHhmeC9CMVFkV2diVDdMWUo4ZDltTWhjY0pzN0pVNzJ2eFlsWExtCjhRTnJMWUxabDZ5OWZMTEhROTNPV2ZyWGIwMHlNK0ZlWk5sNFZTRjNkbXZwWDd0ZTlnWGxmT3lScGZUR2RObFAKMmhlQ282NFpSMS9KSnErb3NqOEdZQmIrTHhHeGE1Q3A4MWlnVXFJejBieXlWWmZySWdEb1JuWXVDaUJaa1FGaAo3NzZhMnBxSTBGaXlCTzlaL1I4cWJZdHVhaStIc3hYeXVjbXhZdHorODFDN2dsWTgxdUgvb2pvZXdhRS9XNUJ3ClU2T3B3dElNWklMMEQ3N0t3U3dybzQ2RE1HU3JvUzlFNDFHbkJKb0NZZ0FFeVZPS3lzaVlEWnptSStHZ0ZFSmoKZ1FtTGpkSVVCRjdpZjkwcFVhd25tcGpYTCt2NGlHV1plcmx2NVFJREFRQUJBb0lCQUFDaU9pQVZSb3ZvVzE5UwpGdUxpVVhhcGJqeGoxRmluM0JCRWdZS2xBWmVwVWxEbEZZcW0xakU5RjB5RUNrNjlqK29qVk9wKzNCY2NIVGM2CnBvYzVmc29QenBVWEJrck9tUjQxRDhSU1JpODkvYjJZYkV4cTcrdnd4dW5uZm9iakUrYXRXYVU4eERXYkFaSGUKVEthb1AxZG5YQ3gxdVBpWWVlMnduMytmOTR4M29URkt0ZmRUMWNmNjRCbG93ODJqVnJmYWNheEQvMW5YZlhUNgpYdnIwTmxNVlpvajJYVFVGNEpLY01XTW1WaDVnUFUxekQ0VkNCV253L3VGbldnSW5jWGdEcjIxODljQnc5V3dNCjJVVzlOT09JbUp3OW1LSWVER1JnaXZqRXpFU1Y5M2RFV1N5WVpTTUJ2ZWhmWjBtUk1LMjRaN1FLN1REajNrNS8KV1hjZ0xMa0NnWUVBNnIyMTV4TURuNWYxNjJnRjQ5cHV2MGtzZ2FoaFpLQUY4aXlINUhMYjRXQjd6S3FNUmE1NApWT1o1TS9aeGIyTDduREQ3aW9QSURSYStPdVNLYmlkaGFLd3FEclJMLy9vc01pb3pPd2wvQ3NXTUxORm5oTDlLCmRtUkRxMDJZeGxETmhxS09mNDJLcG44Q040VjhqN04ybmoxbWd0VnRaRnlpQjByWXlSYWdNdjBDZ1lFQTZTbSsKZEkxQ2xMVnNLN0dsT1FzRDdIMEtyUnRDN0syMU4xYXR2WFVoZ0l2TmI3OHVVN1lhWnlIUFE4NXB4ZExPNm1tdwo4VURQMnFtUG12Q3NoK0NGYXJSRkVuNFJ3NWJPNU54dlFoTXR0S1BrZGVkTjRFc0lGQ3RNeU1Hak5nMXFXbDhxCjZFNy9hYUtUV2QrckZ6UGlialFlM2RaMTdrb21iZlV3bkYzN3lRa0NnWUJydkx5REhNLzU3S1hhOEhoYmxvSmoKMnZMSlkzMm4wR1pLT3pQM250dmFPZzAzNTBMUUxINWdBUk81eks5TmZ6R2FBMFUvMHJIN2grZXhZZmxEQzJJQwp4NW5aKzlneDJTRjF1TGFncndBVzhvb2VlOUcyTkpHNWV0VXdCMEpLS3dYWmVEeE13S3JWTmMvUHFiMTh1dEtECldBejNtWHRHcDlsWjNYbFgrY0YzQlFLQmdFWEVZZUxLS2ZRSlhUYXR6enlFV1VZNEhDdTBEcEIzWWRRUE1hbVIKRk5jNy9kckVILzZZYk1vVFNjdUdSZ3dWaWlpR081WFV5TjByQTNkZk1LRHZ3N3dyK01jUnhncjZZeW9EODU2WAowb05NengyZ2VxTDBrSlJJYUk5aHNZNkk4UnZkZ2g5RkZCUHR1NTJXMmNEMW0zbFNTekloNitQRGVFQktyNkw2ClZFN1JBb0dCQUpzYjh5Z2k2T3NvdG9YZzEvZk9IM09MSWM0aHFRR0VyYVp5MmthUVYyK2g2ZzB6UEY0OVFQWVEKQUV3dDdNcVhGUXNmaE5tcTRtUENONTR0TXB0ZUU4UXd2M0lkc0szb1NmTlNYYUdPc2xxdFVZYXNmb2NyazNBNwpCbUVibG9mNlVoVnVaanpYRDdtZ2RXbzFYdW5qZE42VThrYXdJM2RkdFZvQ0xVRDhmeE5GCi0tLS0tRU5EIFJTQSBQUklWQVRFIEtFWS0tLS0t";
        _publicKey =
            "LS0tLS1CRUdJTiBSU0EgUFVCTElDIEtFWS0tLS0tCk1JSUJDZ0tDQVFFQTFjenhsUjdQenh4ZngvQjFRZFdnYlQ3TFlKOGQ5bU1oY2NKczdKVTcydnhZbFhMbThRTnIKTFlMWmw2eTlmTExIUTkzT1dmclhiMDB5TStGZVpObDRWU0YzZG12cFg3dGU5Z1hsZk95UnBmVEdkTmxQMmhlQwpvNjRaUjEvSkpxK29zajhHWUJiK0x4R3hhNUNwODFpZ1VxSXowYnl5VlpmcklnRG9Sbll1Q2lCWmtRRmg3NzZhCjJwcUkwRml5Qk85Wi9SOHFiWXR1YWkrSHN4WHl1Y214WXR6KzgxQzdnbFk4MXVIL29qb2V3YUUvVzVCd1U2T3AKd3RJTVpJTDBENzdLd1N3cm80NkRNR1Nyb1M5RTQxR25CSm9DWWdBRXlWT0t5c2lZRFp6bUkrR2dGRUpqZ1FtTApqZElVQkY3aWY5MHBVYXdubXBqWEwrdjRpR1daZXJsdjVRSURBUUFCCi0tLS0tRU5EIFJTQSBQVUJMSUMgS0VZLS0tLS0=";
        _tokenGenerator = new BattleTokenGenerator(_privateKey);
    }

    [Fact]
    public void GenerateBattleToken_ShouldCreateValidJwt()
    {
        int battleId = 5001;

        string token = _tokenGenerator.GenerateBattleToken(battleId);

        Assert.NotNull(token);
        Assert.NotEmpty(token);

        byte[] publicKeyBytes = Convert.FromBase64String(_publicKey);
        string publicKeyPem = Encoding.UTF8.GetString(publicKeyBytes);

        var handler = new JwtSecurityTokenHandler();
        var rsa = RSA.Create();
        rsa.ImportFromPem(publicKeyPem.ToCharArray());

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
