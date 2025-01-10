namespace ArenaService.Repositories;

using ArenaService.Data;
using ArenaService.Models;
using Microsoft.EntityFrameworkCore;

public interface ISeasonRepository
{
    Task<List<Season>> GetAllSeasonsAsync();
    Task<Season?> GetSeasonAsync(int id);
}

public class SeasonRepository : ISeasonRepository
{
    private readonly ArenaDbContext _context;

    public SeasonRepository(ArenaDbContext context)
    {
        _context = context;
    }

    public async Task<List<Season>> GetAllSeasonsAsync()
    {
        return await _context.Seasons.OrderByDescending(s => s.StartBlock).ToListAsync();
    }

    public async Task<Season?> GetSeasonAsync(int id)
    {
        return await _context.Seasons.Include(s => s.Rounds).FirstOrDefaultAsync(s => s.Id == id);
    }
}
