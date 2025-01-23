using ArenaService.Constants;
using Swashbuckle.AspNetCore.Annotations;

namespace ArenaService.Dtos;

public class SeasonResponse
{
    public int Id { get; set; }
    public ArenaType ArenaType { get; set; }
    public long StartBlockIndex { get; set; }
    public long EndBlockIndex { get; set; }
    public int RoundInterval { get; set; }
    public int RequiredMedalCount { get; set; }
    public TicketPolicyResponse BattleTicketPolicy { get; set; }
    public TicketPolicyResponse RefreshTicketPolicy { get; set; }
    public int TotalPrize { get; set; }
    public string PrizeDetailSiteURL { get; set; }

    public List<RoundResponse> Rounds { get; set; } = new List<RoundResponse>();
}
