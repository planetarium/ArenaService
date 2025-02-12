namespace ArenaService.Repositories;

using ArenaService.Data;
using ArenaService.Models;
using Libplanet.Crypto;
using Microsoft.EntityFrameworkCore;

public interface IClanRepository
{
    Task<Clan?> GetClan(int clanId, Func<IQueryable<Clan>, IQueryable<Clan>>? includeQuery = null);
}

public class ClanRepository : IClanRepository
{
    private readonly ArenaDbContext _context;

    public ClanRepository(ArenaDbContext context)
    {
        _context = context;
    }

    public async Task<Clan?> GetClan(
        int clanId,
        Func<IQueryable<Clan>, IQueryable<Clan>>? includeQuery = null
    )
    {
        var query = _context.Clans.AsQueryable();

        if (includeQuery != null)
        {
            query = includeQuery(query);
        }

        return await query.SingleOrDefaultAsync(c => c.Id == clanId);
    }
}
