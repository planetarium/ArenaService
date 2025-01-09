namespace ArenaService.Repositories;

using ArenaService.Data;
using ArenaService.Models;
using Libplanet.Crypto;
using Microsoft.EntityFrameworkCore;

public interface IArenaIntervalRepository
{
    Task<ArenaInterval?> GetArenaIntervalAsync(int arenaIntervalId);
}

public class ArenaIntervalRepository : IArenaIntervalRepository
{
    private readonly ArenaDbContext _context;

    public ArenaIntervalRepository(ArenaDbContext context)
    {
        _context = context;
    }

    public async Task<ArenaInterval?> GetArenaIntervalAsync(int arenaIntervalId)
    {
        return await _context.ArenaIntervals.FirstOrDefaultAsync(ai => ai.Id == arenaIntervalId);
    }
}
