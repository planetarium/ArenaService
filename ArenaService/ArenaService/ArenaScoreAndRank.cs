using Libplanet.Crypto;

namespace ArenaService;

public class ArenaScoreAndRank(Address avatarAddr, int score, int rank)
{
    public Address AvatarAddr { get; } = avatarAddr;
    public int Score { get; } = score;
    public int Rank { get; } = rank;
}
