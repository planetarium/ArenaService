using ArenaService.Jwt;

namespace ArenaService.Tests.Jwt;

public class BattleTokenValidatorTests
{
    private BattleTokenValidator _validator;
    private string _publicKey;

    public BattleTokenValidatorTests()
    {
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
        _validator = new BattleTokenValidator(_publicKey);
    }

    [Theory]
    [InlineData("VALID_TOKEN", true)]
    [InlineData("NO_BATTLE_ID", false)]
    [InlineData("INVALID_BATTLE_ID_TYPE", false)]
    [InlineData("INVALID_ISSUER", false)]
    [InlineData("EXPIRED_TOKEN", false)]
    [InlineData("INVALID_SIGNATURE", false)]
    public void ValidateBattleToken_ShouldReturnExpectedResult(
        string tokenType,
        bool expectedResult
    )
    {
        int battleId = 5001;

        string token = GetTestToken(tokenType);

        bool isValid = _validator.ValidateBattleToken(token, battleId);

        Assert.Equal(expectedResult, isValid);
    }

    private string GetTestToken(string tokenType)
    {
        return tokenType switch
        {
            "VALID_TOKEN"
                => "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJwbGFuZXRhcml1bSBhcmVuYSBzZXJ2aWNlIiwiYmlkIjo1MDAxLCJpYXQiOjE3MzkyNjc1NTYsImV4cCI6NDg5NDk0MTE1NiwiYXVkIjoiTmluZUNocm9uaWNsZXMgaGVhZGxlc3MifQ.QNCHaZ9UjGI_cC6OWJ-y7L0GGhtP6BXzNI_uVIuebT5KlCtgiVOm4ZPknIVDbI5kywf3wLBSRLDwRR1HgcvsLCfIq1BzvoltmsplEY7zgjUyEPnNLDdg40FuHHc7RzqczyrC_3iLfMpmZONwjfOIgmtiyLCjcRby27R0kEn5Pqc3WNNE0Erfw8wdQ1FwjP8enDnFYYJ16fj1CgshvK209pwr9jSPNF5dBRB4T1rfuLC6U1S_vFGrZZi7jrOf61wFxNsseqIpCaB0gxt8UWnoyP6r2MKt9PiQ_X0r40o13QXFyavrgp-3jCkq99JuKQ7NX2KZuCbEsTwUJMKVbRHZYQ",
            "NO_BATTLE_ID"
                => "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJwbGFuZXRhcml1bSBhcmVuYSBzZXJ2aWNlIiwiaWF0IjoxNzM5MjY3NDI1LCJleHAiOjQ4OTQ5NDEwMjUsImF1ZCI6Ik5pbmVDaHJvbmljbGVzIGhlYWRsZXNzIn0.swIq0IBE7PzxnJM8ZvrwBL82sZ6qT-IDVzNb5j8XAFfpYYeQ85TkLW5oQeoWKnAmFjFUqT6UFEbTcitJk5IFECwFIEeQwbvlhCaLC5Kn9mbMOrrqruIxj3Rx4suQeX439V47zdKFNR1Fjd8K8FVXyTHp23qNhaiFMRO1ONgND07Ap4cw5kg269dn97PW5tw2Czl46YMdmZcq8PtbqwoAmxKv06qArXs8_JHZljvwXDz2YsPP5uR0wDT-r24_vf_LbYUqG2-OVOvvI0pjvFMhrDPxanvFZm2H1XnyxOLkO0FlfHBbWQAneAnLgFRfUi4N_tBufF9CsRTpipvFo25s0Q",
            "INVALID_BATTLE_ID_TYPE"
                => "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJwbGFuZXRhcml1bSBhcmVuYSBzZXJ2aWNlIiwiYmlkIjoiYmF0dGxlSWQiLCJpYXQiOjE3MzkyNjc0NTgsImV4cCI6NDg5NDk0MTA1OCwiYXVkIjoiTmluZUNocm9uaWNsZXMgaGVhZGxlc3MifQ.Ih4aqHOtCuMSPIr9IYF4TOjRv4rInRwyo6yNsnbBb5oA241FNiaJaYRzJfwpSh8pFL5ZkYNaxTQ6PmgGb2yIxX3oVq9RCKl9GliHY0_hKSjCcB65nJetEbabCQRyPck5fg83mv6u7OqE5FfIVnzJYDwIYVZjVIS_EkyGmY91PBw-bF-Qh8xQWJ2MWC_cKb48ihKZtWiugZbIIM_inKjiaTHNsnvnxKE5dq60KeiNzSTPEP58Vo1htrFz8UvxikfscfNb9LHBsaD-dQ47D6zTszf1Icy1jVlTMGdFMtzPNUNTns4Wo6ITBbhcqLAWg-9o3qaGnjTT55T9R8rX-oxf7w",
            "INVALID_ISSUER"
                => "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJhcmVuYSBzZXJ2aWNlIiwiYmlkIjo1MDAxLCJpYXQiOjE3MzkyNjc0ODcsImV4cCI6NDg5NDk0MTA4NywiYXVkIjoiTmluZUNocm9uaWNsZXMgaGVhZGxlc3MifQ.AsF2DEAWLupJu5VBt1F6BfPeRLNnNBgZ-ugVjLVrIhaw0i0FJOY7ObUghqo5GJHskK0nIJNp0twK4W2jOgLlhFt_W7dtt4Gdsf_fEOG2XORoFv_iVnejriOuu3-TYYgk1nL-Z5sD_CqQCwkTqDMbc8Aa9lI0rz9FAUt6eMBI5vnYRdkQS55iZHv9yxmHDHPiHeG7BDBiPb1juTVrqaO1EjO1BjxUuJt3_oPrua046-GjzYI8i80V2kgUtOzxPO7z0WjXUaMTzaRohhm7pLwYhQKDYxiAvpP9kEYxT7B6SZVV2mVfJyGMF6Ar1EC5zUgg3IIsPAeLGUgKEvpWG4vFsQ",
            "EXPIRED_TOKEN"
                => "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJwbGFuZXRhcml1bSBhcmVuYSBzZXJ2aWNlIiwiYmlkIjo1MDAxLCJpYXQiOjE3MzkyNjc1MTgsImV4cCI6MTczOTI2NzUxOSwiYXVkIjoiTmluZUNocm9uaWNsZXMgaGVhZGxlc3MifQ.HJSAVBP0hLVrxe4wvq1N7L2CA3_aacPbAdYXCRO9Ffp989xdggH_vpD-If-4qVdeAzNaaAonqbJFtusFl52sgIS_PTJIBlCRmHhHwXiNtCy2jAuoq-YVSl1eETLh1uACNQ0rDwlJ3T2joMvLY_HdgttO-vwdEckz5hEIpcxqKfUX6-AnRVg__EcuBvjBscg9r_Z9InAIOz8hXjISX7z0EUlWIGychyW1qoYSU-FHpL3xEPSpkrCCaBq5eBL8t6jzcW8PY42qr3x_5hybNbniITv_6RU5yvsfDfnT5gklWak8Ls2Jf5_JLCZnTEk-9NMaXLWbGKtARLVB9pAGSWJryg",
            "INVALID_SIGNATURE"
                => "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJwbGFuZXRhcml1bSBhcmVuYSBzZXJ2aWNlIiwiYmlkIjo1MDAxLCJpYXQiOjE3MzkyNjc2MjQsImV4cCI6MTczOTI4OTIyNCwiYXVkIjoiTmluZUNocm9uaWNsZXMgaGVhZGxlc3MifQ.UyfLvWUjb-hegwT8NQybpOAEdJTgi1r-oqOT0pK_erBLEcNLbbRQbhDJOz7vLjSp4i_69uT98aaNmAnjTu5K3K6yQTnXGk-B4TdwwitLJFqOBoNCIlvW3X2X1gjEwPvhxX7ghP_t5S_OnrP8Y-lG8tYs40UBSOPMSHGxG28Uh1IE29gnUB9iz8DRqpbr6cTtIfXzs_jHvzHhLjJs3YbrCiheg631o9PXw98TCAx2YyqndaHF9FoqE4KZkfqv4KOZvVly2s5KOaxasrqxP-1-M_tNxYon4X1viEN6fFzXdcAqwmwFlFPHZg4IsEVrTfRG5j8zZBuOwWEvMshxs0bIpw",
            _ => throw new ArgumentException()
        };
    }
}
