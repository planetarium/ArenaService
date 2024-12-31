using Libplanet.Crypto;

namespace ArenaService.Tests;

public class AvatarAddressAndScoreTest
{
    [Fact]
    public void Except_List()
    {
        var address = new PrivateKey().Address;
        var address2 = new PrivateKey().Address;
        var addressAndScore = new AvatarAddressAndScore(address, 100);
        var addressAndScore2 = new AvatarAddressAndScore(address2, 100);
        var updated = new AvatarAddressAndScore(address, 200);

        // not equal because score is different
        Assert.NotEqual(addressAndScore, updated);
        // not equal because address is different
        Assert.NotEqual(addressAndScore, addressAndScore2);

        var prev = new List<AvatarAddressAndScore>
        {
            addressAndScore,
            addressAndScore2,
        };
        var next = new List<AvatarAddressAndScore>
        {
            updated,
            addressAndScore2
        };
        var excepted = Assert.Single(next.Except(prev));
        Assert.Equal(updated.AvatarAddr, excepted.AvatarAddr);
        Assert.Equal(updated.Score, excepted.Score);
        Assert.Equal(updated, excepted);
    }
}
