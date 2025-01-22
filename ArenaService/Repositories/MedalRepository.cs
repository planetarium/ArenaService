namespace ArenaService.Repositories;

using ArenaService.Data;
using ArenaService.Models;
using Libplanet.Crypto;
using Microsoft.EntityFrameworkCore;

public interface IMedalRepository
{
    Task<Medal?> GetMedalAsync(int seasonId, Address avatarAddress);
    Task<Medal> UpdateMedalAsync(Medal medal, Action<Medal> updateFields);
    Task<Medal> AddMedalAsync(int seasonId, Address avatarAddress);
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
}
