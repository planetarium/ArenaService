using Newtonsoft.Json;

namespace ArenaService.Shared.Dtos;

public class BattleTokenResponse
{
    [JsonProperty(Required = Required.DisallowNull)]
    public required string Token { get; set; }
    public required int BattleId { get; set; }
}
