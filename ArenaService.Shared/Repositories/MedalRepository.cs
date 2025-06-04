namespace ArenaService.Shared.Repositories;

using ArenaService.Shared.Data;
using ArenaService.Shared.Models;
using EFCore.BulkExtensions;
using Libplanet.Crypto;
using Microsoft.EntityFrameworkCore;
using Npgsql;

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
        var sql = @"
            INSERT INTO medals (season_id, avatar_address, medal_count, created_at, updated_at)
            VALUES (@seasonId, @avatarAddress, 1, @now, @now)
            ON CONFLICT (avatar_address, season_id) 
            DO UPDATE SET 
                medal_count = medals.medal_count + 1,
                updated_at = @now";
        
        var now = DateTime.UtcNow;
        var affected = await _context.Database.ExecuteSqlRawAsync(sql, 
            new NpgsqlParameter("@seasonId", seasonId),
            new NpgsqlParameter("@avatarAddress", avatarAddress.ToHex().ToLower()),
            new NpgsqlParameter("@now", now));
        
        return affected > 0;
    }
}
