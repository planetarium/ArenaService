namespace ArenaService.Shared.Repositories;

using ArenaService.Shared.Constants;
using ArenaService.Shared.Data;
using ArenaService.Shared.Models;
using Microsoft.EntityFrameworkCore;

public interface ISeasonRepository
{
    Task<List<Season>> GetAllSeasonsAsync(
        Func<IQueryable<Season>, IQueryable<Season>>? includeQuery = null
    );
    Task<Season?> GetSeasonByBlockIndexAsync(
        long blockIndex,
        Func<IQueryable<Season>, IQueryable<Season>>? includeQuery = null
    );
    Task<Season> GetSeasonAsync(
        int id,
        Func<IQueryable<Season>, IQueryable<Season>>? includeQuery = null
    );
    Task<List<Season>> GetSeasonsPagedAsync(
        int pageNumber,
        int pageSize,
        Func<IQueryable<Season>, IQueryable<Season>>? includeQuery = null
    );
    Task<int> GetTotalSeasonsCountAsync();
    Task<int?> GetLastSeasonEndBlockAsync();
    Task<bool> IsBlockRangeOverlappingAsync(long startBlock, long endBlock);
    Task<Season> AddSeasonWithRoundsAsync(
        long startBlock,
        int roundInterval,
        int roundCount,
        int seasonGroupId,
        ArenaType arenaType,
        int requiredMedalCount,
        int totalPrize,
        int battleTicketPolicyId,
        int refreshTicketPolicyId
    );
    Task<Season> UpdateSeasonAsync(
        int id,
        int seasonGroupId,
        ArenaType arenaType,
        int roundInterval,
        int requiredMedalCount,
        int totalPrize,
        int battleTicketPolicyId,
        int refreshTicketPolicyId
    );
    Task<Season> AdjustSeasonEndBlockAsync(int seasonId, long newEndBlock);
    Task DeleteSeasonAsync(int seasonId);
}

public class SeasonRepository : ISeasonRepository
{
    private readonly ArenaDbContext _context;

    public SeasonRepository(ArenaDbContext context)
    {
        _context = context;
    }

    public async Task<List<Season>> GetAllSeasonsAsync(
        Func<IQueryable<Season>, IQueryable<Season>>? includeQuery = null
    )
    {
        var query = _context.Seasons.AsQueryable().AsNoTracking();

        if (includeQuery != null)
        {
            query = includeQuery(query);
        }

        return await query.OrderByDescending(s => s.StartBlock).ToListAsync();
    }

    public async Task<Season?> GetSeasonByBlockIndexAsync(
        long blockIndex,
        Func<IQueryable<Season>, IQueryable<Season>>? includeQuery = null
    )
    {
        var query = _context.Seasons.AsQueryable().AsNoTracking();

        if (includeQuery != null)
        {
            query = includeQuery(query);
        }

        return await query
            .Where(s => s.StartBlock <= blockIndex && s.EndBlock >= blockIndex)
            .SingleOrDefaultAsync();
    }

    public async Task<Season> GetSeasonAsync(
        int id,
        Func<IQueryable<Season>, IQueryable<Season>>? includeQuery = null
    )
    {
        var query = _context.Seasons.AsQueryable().AsNoTracking();

        if (includeQuery != null)
        {
            query = includeQuery(query);
        }

        return await query.SingleAsync(s => s.Id == id);
    }

    public async Task<List<Season>> GetSeasonsPagedAsync(
        int pageNumber,
        int pageSize,
        Func<IQueryable<Season>, IQueryable<Season>>? includeQuery = null
    )
    {
        var query = _context.Seasons.AsQueryable().AsNoTracking();

        if (includeQuery != null)
        {
            query = includeQuery(query);
        }

        return await query
            .OrderByDescending(s => s.StartBlock)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetTotalSeasonsCountAsync()
    {
        return await _context.Seasons.CountAsync();
    }

    public async Task<int?> GetLastSeasonEndBlockAsync()
    {
        return await _context
            .Seasons.OrderByDescending(s => s.EndBlock)
            .Select(s => (int?)s.EndBlock)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> IsBlockRangeOverlappingAsync(long startBlock, long endBlock)
    {
        return await _context.Seasons.AnyAsync(s =>
            !(s.EndBlock < startBlock || s.StartBlock > endBlock)
        );
    }

    public async Task<Season> AddSeasonWithRoundsAsync(
        long startBlock,
        int roundInterval,
        int roundCount,
        int seasonGroupId,
        ArenaType arenaType,
        int requiredMedalCount,
        int totalPrize,
        int battleTicketPolicyId,
        int refreshTicketPolicyId
    )
    {
        long endBlock = startBlock + (roundInterval * roundCount) - 1;

        bool isOverlapping = await IsBlockRangeOverlappingAsync(startBlock, endBlock);
        if (isOverlapping)
        {
            throw new InvalidOperationException();
        }

        var season = new Season
        {
            StartBlock = startBlock,
            EndBlock = endBlock,
            ArenaType = arenaType,
            RoundInterval = roundInterval,
            SeasonGroupId = seasonGroupId,
            RequiredMedalCount = requiredMedalCount,
            TotalPrize = totalPrize,
            BattleTicketPolicyId = battleTicketPolicyId,
            RefreshTicketPolicyId = refreshTicketPolicyId
        };

        await _context.Seasons.AddAsync(season);
        await _context.SaveChangesAsync();

        long currentStart = startBlock;
        for (int i = 1; i <= roundCount; i++)
        {
            long currentEnd = currentStart + roundInterval - 1;

            _context.Rounds.Add(
                new Round
                {
                    SeasonId = season.Id,
                    StartBlock = currentStart,
                    EndBlock = currentEnd,
                    RoundIndex = i
                }
            );

            currentStart = currentEnd + 1;
        }

        await _context.SaveChangesAsync();
        return season;
    }

    public async Task<Season> UpdateSeasonAsync(
        int id,
        int seasonGroupId,
        ArenaType arenaType,
        int roundInterval,
        int requiredMedalCount,
        int totalPrize,
        int battleTicketPolicyId,
        int refreshTicketPolicyId
    )
    {
        var season = await _context.Seasons.SingleAsync(s => s.Id == id);
        season.ArenaType = arenaType;
        season.RoundInterval = roundInterval;
        season.RequiredMedalCount = requiredMedalCount;
        season.TotalPrize = totalPrize;
        season.BattleTicketPolicyId = battleTicketPolicyId;
        season.RefreshTicketPolicyId = refreshTicketPolicyId;
        season.SeasonGroupId = seasonGroupId;

        await _context.SaveChangesAsync();
        return season;
    }

    public async Task<Season> AdjustSeasonEndBlockAsync(int seasonId, long newEndBlock)
    {
        var season = await _context.Seasons.SingleAsync(s => s.Id == seasonId);
        season.EndBlock = newEndBlock;

        await _context.SaveChangesAsync();
        return season;
    }

    public async Task DeleteSeasonAsync(int seasonId)
    {
        var season = await _context.Seasons.SingleAsync(s => s.Id == seasonId);
        _context.Seasons.Remove(season);
        await _context.SaveChangesAsync();
    }
}
