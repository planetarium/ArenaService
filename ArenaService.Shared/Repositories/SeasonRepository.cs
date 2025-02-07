namespace ArenaService.Shared.Repositories;

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
        var query = _context.Seasons.AsQueryable();

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
        var query = _context.Seasons.AsQueryable();

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
        var query = _context.Seasons.AsQueryable();

        if (includeQuery != null)
        {
            query = includeQuery(query);
        }

        return await query.SingleAsync(s => s.Id == id);
    }
}
