namespace ArenaService.Repositories;

using ArenaService.Data;
using ArenaService.Views;
using Microsoft.EntityFrameworkCore;

public interface IRefreshPriceRepository
{
    Task<List<RefreshPriceMaterializedView>> GetPricesForCurrentSeasonAsync(int seasonId);
    Task<RefreshPriceMaterializedView> GetPriceAsync(int seasonId, int refreshCount);
    Task<int> GetMaxRefreshCountAsync(int seasonId);
    Task<bool> CanRefreshAsync(int seasonId, int refreshCount);
}

public class RefreshPriceRepository : IRefreshPriceRepository
{
    private readonly ArenaDbContext _context;

    public RefreshPriceRepository(ArenaDbContext context)
    {
        _context = context;
    }

    public async Task<List<RefreshPriceMaterializedView>> GetPricesForCurrentSeasonAsync(
        int seasonId
    )
    {
        return await _context
            .Set<RefreshPriceMaterializedView>()
            .Where(view => view.SeasonId == seasonId)
            .OrderBy(view => view.RefreshOrder)
            .ToListAsync();
    }

    public async Task<RefreshPriceMaterializedView> GetPriceAsync(int seasonId, int refreshCount)
    {
        var price = await _context
            .RefreshPriceView.Where(view =>
                view.SeasonId == seasonId && view.RefreshOrder == refreshCount + 1
            )
            .FirstAsync();

        return price;
    }

    public async Task<int> GetMaxRefreshCountAsync(int seasonId)
    {
        return await _context
            .Set<RefreshPriceMaterializedView>()
            .Where(view => view.SeasonId == seasonId)
            .MaxAsync(view => view.RefreshOrder);
    }

    public async Task<bool> CanRefreshAsync(int seasonId, int refreshCount)
    {
        var maxRefreshCount = await GetMaxRefreshCountAsync(seasonId);
        return refreshCount < maxRefreshCount;
    }
}
