namespace ArenaService.Repositories;

using ArenaService.Data;
using ArenaService.Models;
using Libplanet.Crypto;
using Microsoft.EntityFrameworkCore;

public interface IRoundRepository
{
    Task<Round?> GetRoundAsync(int roundId);
}

public class RoundRepository : IRoundRepository
{
    private readonly ArenaDbContext _context;

    public RoundRepository(ArenaDbContext context)
    {
        _context = context;
    }

    public async Task<Round?> GetRoundAsync(int roundId)
    {
        return await _context.Rounds.FirstOrDefaultAsync(ai => ai.Id == roundId);
    }
}
