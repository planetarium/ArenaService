using Newtonsoft.Json;

namespace ArenaService.Shared.Dtos;

public class UserRegisterRequest
{
    [JsonProperty(Required = Required.DisallowNull)]
    public required string NameWithHash { get; set; }
    public required int PortraitId { get; set; }
    public required long Cp { get; set; }
    public required int Level { get; set; }
}
