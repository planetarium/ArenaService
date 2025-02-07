namespace ArenaService.Shared.Repositories;

using ArenaService.Shared.Data;
using ArenaService.Shared.Models;
using Libplanet.Crypto;
using Microsoft.EntityFrameworkCore;

public interface IRankingSnapshotRepository
{
    Task AddRankingsSnapshot(
        List<(Address AvatarAddress, int Score)> rankings,
        int seasonId,
        int roundId
    );

    Task<List<RankingSnapshot>> GetRankingSnapshots(
        int seasonId,
        int roundId,
        Func<IQueryable<RankingSnapshot>, IQueryable<RankingSnapshot>>? includeQuery = null
    );

    Task<int> GetRankingSnapshotsCount(
        int seasonId,
        int roundId,
        Func<IQueryable<RankingSnapshot>, IQueryable<RankingSnapshot>>? includeQuery = null
    );

    Task AddClanRankingsSnapshot(List<(int ClanId, int Score)> rankings, int seasonId, int roundId);

    Task<List<ClanRankingSnapshot>> GetClanRankingSnapshots(
        int seasonId,
        int roundId,
        Func<IQueryable<ClanRankingSnapshot>, IQueryable<ClanRankingSnapshot>>? includeQuery = null
    );
}

public class RankingSnapshotRepository : IRankingSnapshotRepository
{
    private readonly ArenaDbContext _context;

    public RankingSnapshotRepository(ArenaDbContext context)
    {
        _context = context;
    }

    public async Task AddRankingsSnapshot(
        List<(Address AvatarAddress, int Score)> rankings,
        int seasonId,
        int roundId
    )
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var rankingSnapshots = new List<RankingSnapshot>();
            foreach (var rankingEntry in rankings)
            {
                var rankingSnapshot = new RankingSnapshot
                {
                    SeasonId = seasonId,
                    RoundId = roundId,
                    AvatarAddress = rankingEntry.AvatarAddress,
                    Score = rankingEntry.Score,
                    CreatedAt = DateTime.UtcNow
                };

                rankingSnapshots.Add(rankingSnapshot);
            }

            await _context.RankingSnapshots.AddRangeAsync(rankingSnapshots);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task AddClanRankingsSnapshot(
        List<(int ClanId, int Score)> rankings,
        int seasonId,
        int roundId
    )
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var rankingSnapshots = new List<ClanRankingSnapshot>();
            foreach (var rankingEntry in rankings)
            {
                var rankingSnapshot = new ClanRankingSnapshot
                {
                    SeasonId = seasonId,
                    RoundId = roundId,
                    ClanId = rankingEntry.ClanId,
                    Score = rankingEntry.Score,
                    CreatedAt = DateTime.UtcNow
                };

                rankingSnapshots.Add(rankingSnapshot);
            }

            await _context.ClanRankingSnapshots.AddRangeAsync(rankingSnapshots);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<List<RankingSnapshot>> GetRankingSnapshots(
        int seasonId,
        int roundId,
        Func<IQueryable<RankingSnapshot>, IQueryable<RankingSnapshot>>? includeQuery = null
    )
    {
        var query = _context.RankingSnapshots.AsQueryable();

        if (includeQuery != null)
        {
            query = includeQuery(query);
        }

        return await query.Where(r => r.SeasonId == seasonId && r.RoundId == roundId).ToListAsync();
    }

    public async Task<int> GetRankingSnapshotsCount(
        int seasonId,
        int roundId,
        Func<IQueryable<RankingSnapshot>, IQueryable<RankingSnapshot>>? includeQuery = null
    )
    {
        var query = _context.RankingSnapshots.AsQueryable();

        if (includeQuery != null)
        {
            query = includeQuery(query);
        }

        return await query.Where(r => r.SeasonId == seasonId && r.RoundId == roundId).CountAsync();
    }

    public async Task<List<ClanRankingSnapshot>> GetClanRankingSnapshots(
        int seasonId,
        int roundId,
        Func<IQueryable<ClanRankingSnapshot>, IQueryable<ClanRankingSnapshot>>? includeQuery = null
    )
    {
        var query = _context.ClanRankingSnapshots.AsQueryable();

        if (includeQuery != null)
        {
            query = includeQuery(query);
        }

        return await query.Where(r => r.SeasonId == seasonId && r.RoundId == roundId).ToListAsync();
    }
}
