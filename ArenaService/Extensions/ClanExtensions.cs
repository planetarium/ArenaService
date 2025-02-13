namespace ArenaService.Extensions;

using ArenaService.Dtos;
using ArenaService.Models;

public static class ClanExtensions
{
    public static ClanResponse ToResponse(this Clan clan, int rank, int score)
    {
        return new ClanResponse
        {
            ImageURL = clan.ImageURL,
            Name = clan.Name,
            Rank = rank,
            Score = score,
        };
    }
}
