namespace ArenaService.Shared.Services;

using System.Threading.Tasks;
using ArenaService.Shared.Constants;
using ArenaService.Shared.Exceptions;
using ArenaService.Shared.Models;
using ArenaService.Shared.Repositories;
using Libplanet.Crypto;
using Microsoft.EntityFrameworkCore;

public interface ISeasonService
{
    Task<List<Season>> ClassifyByChampionship(
        long blockIndex,
        Func<IQueryable<Season>, IQueryable<Season>>? includeQuery = null
    );

    Task<(Season Season, Round Round)> GetSeasonAndRoundByBlock(long blockIndex);
    
    Task<bool> CanDeleteSeasonAsync(int seasonId, long currentBlockIndex);
    
    Task DeleteSeasonAsync(int seasonId);
    
    Task<Season?> GetLastSeasonByBlockIndexAsync(long blockIndex);
    
    Task<List<Season>> GetCompletedSeasonsBeforeBlock(long blockIndex);

    Task<(List<Season> Seasons, int TotalCount, int TotalPages, bool HasNextPage, bool HasPreviousPage)> GetSeasonsPagedAsync(
        int pageNumber,
        int pageSize,
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

    public async Task<(Season Season, Round Round)> GetSeasonAndRoundByBlock(long blockIndex)
    {
        var season = await _seasonRepo.GetSeasonByBlockIndexAsync(
            blockIndex,
            q => q.Include(s => s.Rounds)
        );

        if (season == null)
        {
            throw new NotFoundSeasonException(
                $"No matching season found for the current block index ({blockIndex})."
            );
        }

        var round = season.Rounds.SingleOrDefault(ai =>
            ai.StartBlock <= blockIndex && ai.EndBlock >= blockIndex
        );

        if (round == null)
        {
            throw new NotFoundSeasonException(
                $"No matching round found for the current block index ({blockIndex})."
            );
        }

        return (season, round);
    }

    public async Task<bool> CanDeleteSeasonAsync(int seasonId, long currentBlockIndex)
    {
        var season = await _seasonRepo.GetSeasonAsync(seasonId);
        return season.StartBlock >= currentBlockIndex;
    }

    public async Task DeleteSeasonAsync(int seasonId)
    {
        await _seasonRepo.DeleteSeasonAsync(seasonId);
    }

    public async Task<Season?> GetLastSeasonByBlockIndexAsync(long blockIndex)
    {
        var seasons = await _seasonRepo.GetAllSeasonsAsync();
        
        return seasons
            .Where(s => s.ArenaType == ArenaType.SEASON && s.EndBlock < blockIndex)
            .OrderByDescending(s => s.EndBlock)
            .FirstOrDefault();
    }

    public async Task<List<Season>> GetCompletedSeasonsBeforeBlock(long blockIndex)
    {
        var seasons = await _seasonRepo.GetAllSeasonsAsync();
        
        return seasons
            .Where(s => s.ArenaType == ArenaType.SEASON && s.EndBlock < blockIndex)
            .OrderByDescending(s => s.EndBlock)
            .ToList();
    }

    public async Task<(List<Season> Seasons, int TotalCount, int TotalPages, bool HasNextPage, bool HasPreviousPage)> GetSeasonsPagedAsync(
        int pageNumber,
        int pageSize,
        Func<IQueryable<Season>, IQueryable<Season>>? includeQuery = null
    )
    {
        var seasons = await _seasonRepo.GetSeasonsPagedAsync(pageNumber, pageSize, includeQuery);
        var totalCount = await _seasonRepo.GetTotalSeasonsCountAsync();
        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
        var hasNextPage = pageNumber < totalPages;
        var hasPreviousPage = pageNumber > 1;

        return (seasons, totalCount, totalPages, hasNextPage, hasPreviousPage);
    }
}
