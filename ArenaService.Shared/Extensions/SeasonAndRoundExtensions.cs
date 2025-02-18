namespace ArenaService.Shared.Extensions;

using ArenaService.Shared.Dtos;
using ArenaService.Shared.Models;

public static class SeasonAndRoundExtensions
{
    public static SeasonAndRoundResponse ToResponse(
        this (Season Season, Round Round) seasonAndRound
    )
    {
        return new SeasonAndRoundResponse
        {
            Id = seasonAndRound.Season.Id,
            SeasonGroupId = seasonAndRound.Season.SeasonGroupId,
            StartBlockIndex = seasonAndRound.Season.StartBlock,
            EndBlockIndex = seasonAndRound.Season.EndBlock,
            ArenaType = seasonAndRound.Season.ArenaType,
            RoundInterval = seasonAndRound.Season.RoundInterval,
            RequiredMedalCount = seasonAndRound.Season.RequiredMedalCount,
            TotalPrize = seasonAndRound.Season.TotalPrize,
            PrizeDetailSiteURL = seasonAndRound.Season.PrizeDetailUrl,
            Round = seasonAndRound.Round.ToResponse(),
        };
    }
}
