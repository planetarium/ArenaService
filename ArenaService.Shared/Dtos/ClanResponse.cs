using ArenaService.Shared.Constants;
using Newtonsoft.Json;

namespace ArenaService.Shared.Dtos;

public class ClanResponse
{
    [JsonProperty(Required = Required.DisallowNull)]
    public required string ImageURL { get; set; }

    [JsonProperty(Required = Required.DisallowNull)]
    public required string Name { get; set; }
    public required int Rank { get; set; }
    public required int Score { get; set; }
}
