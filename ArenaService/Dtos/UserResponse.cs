using Libplanet.Crypto;

namespace ArenaService.Dtos;

public class UserResponse
{
    public Address AvatarAddress { get; set; }

    public required string NameWithHash { get; set; }

    public int PortraitId { get; set; }
    public long Cp { get; set; }
    public int Level { get; set; }
}
