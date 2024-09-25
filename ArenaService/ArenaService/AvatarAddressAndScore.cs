using Libplanet.Crypto;

namespace ArenaService;

public struct AvatarAddressAndScore(Address avatarAddr, int score) : IEquatable<AvatarAddressAndScore>
{
    public bool Equals(AvatarAddressAndScore other)
    {
        return AvatarAddr.Equals(other.AvatarAddr) && Score == other.Score;
    }

    public override bool Equals(object? obj)
    {
        return obj is AvatarAddressAndScore other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (AvatarAddr.GetHashCode() * 397) ^ Score;
        }
    }

    public Address AvatarAddr { get; set; } = avatarAddr;
    public int Score { get; set; } = score;
}
