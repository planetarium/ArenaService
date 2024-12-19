namespace ArenaService.Extensions;

using ArenaService.Dtos;
using ArenaService.Models;

public static class SeasonExtensions
{
    public static SeasonDto ToDto(this Season season)
    {
        return new SeasonDto
        {
            Id = season.Id,
            StartBlockIndex = season.StartBlockIndex,
            EndBlockIndex = season.EndBlockIndex,
            TicketRefillInterval = season.TicketRefillInterval
        };
    }
}
