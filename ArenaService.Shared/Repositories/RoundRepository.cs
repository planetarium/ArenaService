namespace ArenaService.Shared.Repositories;

using ArenaService.Shared.Data;
using ArenaService.Shared.Models;
using Libplanet.Crypto;
using Microsoft.EntityFrameworkCore;

public interface IRoundRepository
{
    Task<Round?> GetRoundAsync(
        int roundId,
        Func<IQueryable<Round>, IQueryable<Round>>? includeQuery = null
    );
}

public class RoundRepository : IRoundRepository
{
    private readonly ArenaDbContext _context;

    public RoundRepository(ArenaDbContext context)
    {
        _context = context;
    }

    public async Task<Round?> GetRoundAsync(
        int roundId,
        Func<IQueryable<Round>, IQueryable<Round>>? includeQuery = null
    )
    {
        var query = _context.Rounds.AsQueryable().AsNoTracking();

        if (includeQuery != null)
        {
            query = includeQuery(query);
        }

        return await query.SingleOrDefaultAsync(r => r.Id == roundId);
    }
}
