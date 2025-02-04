namespace ArenaService.Services;

using System.Threading.Tasks;
using ArenaService.Constants;
using ArenaService.Models;
using ArenaService.Repositories;
using Libplanet.Crypto;
using Microsoft.EntityFrameworkCore;

public interface ISeasonService
{
    Task<List<Season>> ClassifyByChampionship(
        long blockIndex,
        Func<IQueryable<Season>, IQueryable<Season>>? includeQuery = null
    );
}

public class SeasonService : ISeasonService
{
    private readonly ISeasonRepository _seasonRepo;

    public SeasonService(ISeasonRepository seasonRepo)
    {
        _seasonRepo = seasonRepo;
    }

    public async Task<List<Season>> ClassifyByChampionship(
        long blockIndex,
        Func<IQueryable<Season>, IQueryable<Season>>? includeQuery = null
    )
    {
        var seasons = await _seasonRepo.GetAllSeasonsAsync(includeQuery);

        var currentSeason = seasons.FirstOrDefault(s =>
            s.StartBlock <= blockIndex && s.EndBlock >= blockIndex
        );

        if (currentSeason == null)
        {
            return [];
        }

        var championshipSeasons = seasons
            .Where(s => s.ArenaType == ArenaType.CHAMPIONSHIP)
            .OrderBy(s => s.StartBlock)
            .ToList();

        Season? nextChampionship;
        if (currentSeason.ArenaType == ArenaType.CHAMPIONSHIP)
        {
            nextChampionship = currentSeason;
        }
        else
        {
            nextChampionship = championshipSeasons.FirstOrDefault(s =>
                s.StartBlock > blockIndex && s.EndBlock >= blockIndex
            );
        }

        if (nextChampionship == null)
        {
            return [currentSeason];
        }

        var previousChampionship = championshipSeasons.LastOrDefault(s =>
            s.EndBlock < nextChampionship.StartBlock
        );

        if (previousChampionship == null)
        {
            return [currentSeason];
        }

        return seasons
            .Where(s =>
                s.StartBlock > previousChampionship.StartBlock
                && s.StartBlock <= nextChampionship.StartBlock
            )
            .OrderBy(s => s.StartBlock)
            .ToList();
    }
}
