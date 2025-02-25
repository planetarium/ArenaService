namespace ArenaService.Shared.Repositories;

using ArenaService.Shared.Data;
using ArenaService.Shared.Models;
using Libplanet.Crypto;
using Microsoft.EntityFrameworkCore;

public interface IMedalRepository
{
    Task<Medal?> GetMedalAsync(int seasonId, Address avatarAddress);
    Task<Dictionary<Address, int>> GetMedalsBySeasonsAsync(List<int> seasons);
    Task<Medal> UpdateMedalAsync(Medal medal, Action<Medal> updateFields);
    Task<Medal> AddMedalAsync(int seasonId, Address avatarAddress);
    Task<bool> AddOrUpdateMedal(int seasonId, Address avatarAddress);
}

public class MedalRepository : IMedalRepository
{
    private readonly ArenaDbContext _context;

    public MedalRepository(ArenaDbContext context)
    {
        _context = context;
    }

    public async Task<Medal?> GetMedalAsync(int seasonId, Address avatarAddress)
    {
        return await _context.Medals.SingleOrDefaultAsync(m =>
            m.SeasonId == seasonId && m.AvatarAddress == avatarAddress
        );
    }

    public async Task<Dictionary<Address, int>> GetMedalsBySeasonsAsync(List<int> seasons)
    {
        var medalCounts = await _context
            .Medals.Where(m => seasons.Contains(m.SeasonId))
            .GroupBy(m => m.AvatarAddress)
            .Select(g => new { AvatarAddress = g.Key, TotalMedals = g.Sum(m => m.MedalCount) })
            .ToDictionaryAsync(m => m.AvatarAddress, m => m.TotalMedals);

        return medalCounts;
    }

    public async Task<Medal> UpdateMedalAsync(
        int seasonId,
        Address avatarAddress,
        Action<Medal> updateFields
    )
    {
        var medal = await GetMedalAsync(seasonId, avatarAddress);

        if (medal is null)
        {
            throw new ArgumentException(
                $"Medal not found for {seasonId} and avatarAddress {avatarAddress}"
            );
        }
        return await UpdateMedalAsync(medal, updateFields);
    }

    public async Task<Medal> UpdateMedalAsync(Medal medal, Action<Medal> updateFields)
    {
        updateFields(medal);
        medal.UpdatedAt = DateTime.UtcNow;

        _context.Medals.Update(medal);
        await _context.SaveChangesAsync();

        return medal;
    }

    public async Task<Medal> AddMedalAsync(int seasonId, Address avatarAddress)
    {
        var addedMedal = await _context.Medals.AddAsync(
            new Medal
            {
                AvatarAddress = avatarAddress,
                SeasonId = seasonId,
                MedalCount = 1
            }
        );
        await _context.SaveChangesAsync();

        return addedMedal.Entity;
    }

    public async Task<bool> AddOrUpdateMedal(int seasonId, Address avatarAddress)
    {
        try
        {
            await _context.Medals.AddAsync(new Medal
            {
                SeasonId = seasonId,
                AvatarAddress = avatarAddress,
                MedalCount = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException)
        {
            var affected = await _context.Medals
                .Where(m => m.SeasonId == seasonId && m.AvatarAddress == avatarAddress)
                .ExecuteUpdateAsync(m => m
                    .SetProperty(x => x.MedalCount, x => x.MedalCount + 1)
                    .SetProperty(x => x.UpdatedAt, DateTime.UtcNow)
                );

            return affected > 0;
        }
    }
}
