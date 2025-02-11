namespace ArenaService.Repositories;

using ArenaService.Data;
using ArenaService.Models;
using Libplanet.Crypto;
using Microsoft.EntityFrameworkCore;

public interface IClanRepository
{
    Task<Clan?> GetClan(int clanId);
}

public class ClanRepository : IClanRepository
{
    private readonly ArenaDbContext _context;

    public ClanRepository(ArenaDbContext context)
    {
        _context = context;
    }

    public async Task<Clan?> GetClan(int clanId)
    {
        return await _context.Clans.SingleOrDefaultAsync(c => c.Id == clanId);
    }
}
