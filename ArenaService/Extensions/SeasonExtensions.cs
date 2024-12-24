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
            StartBlockIndex = season.StartBlockIndex,
            EndBlockIndex = season.EndBlockIndex,
            TicketRefillInterval = season.TicketRefillInterval
        };
    }
}
