using Libplanet.Crypto;

namespace ArenaService;

public struct ArenaScoreAndRank(Address avatarAddr, int score, int rank)
{
    public Address AvatarAddr { get; set; } = avatarAddr;
    public int Score { get; set; } = score;
    public int Rank { get; set; } = rank;
}
