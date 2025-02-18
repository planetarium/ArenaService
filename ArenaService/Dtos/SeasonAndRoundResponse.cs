using ArenaService.Constants;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace ArenaService.Dtos;

public class SeasonAndRoundResponse
{
    public required int Id { get; set; }
    public required int SeasonGroupId { get; set; }

    [JsonConverter(typeof(StringEnumConverter))]
    public required ArenaType ArenaType { get; set; }
    public required long StartBlockIndex { get; set; }
    public required long EndBlockIndex { get; set; }
    public required int RoundInterval { get; set; }
    public required int RequiredMedalCount { get; set; }
    public required int TotalPrize { get; set; }

    [JsonProperty(Required = Required.DisallowNull)]
    public required string PrizeDetailSiteURL { get; set; }

    public required RoundResponse Round { get; set; }
}
