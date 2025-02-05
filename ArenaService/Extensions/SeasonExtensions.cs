namespace ArenaService.Extensions;

using ArenaService.Dtos;
using ArenaService.Models;

public static class SeasonExtensions
{
    public static SeasonResponse ToResponse(this Season season)
    {
        return new SeasonResponse
        {
            Id = season.Id,
            SeasonGroupId = season.Id,
            StartBlockIndex = season.StartBlock,
            EndBlockIndex = season.EndBlock,
            ArenaType = season.ArenaType,
            RoundInterval = season.RoundInterval,
            RequiredMedalCount = season.RequiredMedalCount,
            BattleTicketPolicy = season.BattleTicketPolicy.ToResponse(),
            RefreshTicketPolicy = season.RefreshTicketPolicy.ToResponse(),
            TotalPrize = season.TotalPrize,
            PrizeDetailSiteURL = "https://github.com/orgs/planetarium/projects/97/views/12",
            Rounds = season.Rounds.OrderBy(r => r.StartBlock).Select(r => r.ToResponse()).ToList(),
        };
    }
}
