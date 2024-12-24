namespace ArenaService.Repositories;

using ArenaService.Data;
using ArenaService.Models;
using Microsoft.EntityFrameworkCore;

public interface ISeasonRepository
{
    Task<List<Season>> GetActivatedSeasonsAsync();
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

    public async Task<List<Season>> GetActivatedSeasonsAsync()
    {
        return await _context
            .Seasons.Where(s => s.IsActivated)
            .OrderByDescending(s => s.StartBlockIndex)
            .ToListAsync();
    }

    public async Task<List<Season>> GetAllSeasonsAsync()
    {
        return await _context.Seasons.OrderByDescending(s => s.StartBlockIndex).ToListAsync();
    }

    public async Task<Season?> GetSeasonAsync(int id)
    {
        return await _context.Seasons.FindAsync(id);
    }
}
