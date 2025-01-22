namespace ArenaService.Repositories;

using ArenaService.Constants;
using ArenaService.Data;
using ArenaService.Models;
using Libplanet.Crypto;
using Libplanet.Types.Tx;
using Microsoft.EntityFrameworkCore;

public interface IAvailableOpponentRepository
{
    Task<List<AvailableOpponent>> GetAvailableOpponents(
        Address avatarAddress,
        int roundId,
        Func<IQueryable<AvailableOpponent>, IQueryable<AvailableOpponent>>? includeQuery = null
    );
    Task<List<AvailableOpponent>> RefreshAvailableOpponents(
        int seasonId,
        int roundId,
        Address avatarAddress,
        List<(Address, int)> opponentAvatarAddresses
    );
}

public class AvailableOpponentRepository : IAvailableOpponentRepository
{
    private readonly ArenaDbContext _context;

    public AvailableOpponentRepository(ArenaDbContext context)
    {
        _context = context;
    }

    public async Task<List<AvailableOpponent>> GetAvailableOpponents(
        Address avatarAddress,
        int roundId,
        Func<IQueryable<AvailableOpponent>, IQueryable<AvailableOpponent>>? includeQuery = null
    )
    {
        var query = _context.AvailableOpponents.AsQueryable();

        if (includeQuery != null)
        {
            query = includeQuery(query);
        }

        return await query
            .Where(ao =>
                ao.AvatarAddress == avatarAddress && ao.RoundId == roundId && ao.DeletedAt == null
            )
            .ToListAsync();
    }

    public async Task<List<AvailableOpponent>> RefreshAvailableOpponents(
        int seasonId,
        int roundId,
        Address avatarAddress,
        List<(Address, int)> opponents
    )
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var existOpponents = await GetAvailableOpponents(avatarAddress, roundId);

            foreach (var existOpponent in existOpponents)
            {
                existOpponent.DeletedAt = DateTime.UtcNow;

                _context.AvailableOpponents.Update(existOpponent);
            }

            var newOpponents = new List<AvailableOpponent>();

            foreach (var opponent in opponents)
            {
                var newOpponent = new AvailableOpponent
                {
                    AvatarAddress = avatarAddress,
                    SeasonId = seasonId,
                    RoundId = roundId,
                    GroupId = opponent.Item2,
                    OpponentAvatarAddress = opponent.Item1,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                newOpponents.Add(newOpponent);
            }

            await _context.AvailableOpponents.AddRangeAsync(newOpponents);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return newOpponents;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
