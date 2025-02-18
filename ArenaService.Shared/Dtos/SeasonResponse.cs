using ArenaService.Shared.Constants;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace ArenaService.Shared.Dtos;

public class SeasonResponse
{
    public required int Id { get; set; }
    public required int SeasonGroupId { get; set; }

    [JsonConverter(typeof(StringEnumConverter))]
    public required ArenaType ArenaType { get; set; }
    public required long StartBlockIndex { get; set; }
    public required long EndBlockIndex { get; set; }
    public required int RoundInterval { get; set; }
    public required int RequiredMedalCount { get; set; }
    public required TicketPolicyResponse BattleTicketPolicy { get; set; }
    public required TicketPolicyResponse RefreshTicketPolicy { get; set; }
    public required int TotalPrize { get; set; }

    [JsonProperty(Required = Required.DisallowNull)]
    public required string PrizeDetailSiteURL { get; set; }

    public required List<RoundResponse> Rounds { get; set; } = new List<RoundResponse>();
}
