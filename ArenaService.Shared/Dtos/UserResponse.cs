using Libplanet.Crypto;
using Newtonsoft.Json;

namespace ArenaService.Shared.Dtos;

public class UserResponse
{
    public required Address AvatarAddress { get; set; }

    [JsonProperty(Required = Required.DisallowNull)]
    public required string NameWithHash { get; set; }

    public required int PortraitId { get; set; }
    public required long Cp { get; set; }
    public required int Level { get; set; }
}
