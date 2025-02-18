namespace ArenaService.Shared.Extensions;

using ArenaService.Shared.Dtos;
using ArenaService.Shared.Models;

public static class RoundExtensions
{
    public static RoundResponse ToResponse(this Round round)
    {
        return new RoundResponse
        {
            Id = round.Id,
            StartBlockIndex = round.StartBlock,
            EndBlockIndex = round.EndBlock
        };
    }
}
