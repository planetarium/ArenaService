namespace ArenaService.Repositories;

using ArenaService.Data;
using ArenaService.Models;
using Libplanet.Crypto;
using Microsoft.EntityFrameworkCore;

public interface IRankingSnapshotRepository
{
    Task AddRankingsSnapshot(
        List<(Address AvatarAddress, int? ClanId, int Score)> rankings,
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
}

public class RankingSnapshotRepository : IRankingSnapshotRepository
{
    private readonly ArenaDbContext _context;

    public RankingSnapshotRepository(ArenaDbContext context)
    {
        _context = context;
    }

    public async Task AddRankingsSnapshot(
        List<(Address AvatarAddress, int? ClanId, int Score)> rankings,
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
                    ClanId = rankingEntry.ClanId,
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
}
