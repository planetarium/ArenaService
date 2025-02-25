namespace ArenaService.Shared.Repositories;

using ArenaService.Shared.Constants;
using ArenaService.Shared.Data;
using ArenaService.Shared.Models;
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
    Task<AvailableOpponent?> GetAvailableOpponent(
        Address avatarAddress,
        int roundId,
        Address opponentAvatarAddress,
        Func<IQueryable<AvailableOpponent>, IQueryable<AvailableOpponent>>? includeQuery = null
    );
    Task<List<AvailableOpponent>> RefreshAvailableOpponents(
        int seasonId,
        int roundId,
        Address avatarAddress,
        List<(Address, int)> opponentAvatarAddresses
    );
    Task<AvailableOpponent> UpdateAvailableOpponent(
        Address avatarAddress,
        int roundId,
        Address opponentAvatarAddress,
        Action<AvailableOpponent> updateFields
    );
    Task<AvailableOpponent> UpdateAvailableOpponent(
        AvailableOpponent availableOpponent,
        Action<AvailableOpponent> updateFields
    );
    Task<int?> GetSuccessBattleId(int availableOpponentId);
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
        var query = _context.AvailableOpponents.AsQueryable().AsNoTracking();

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

    public async Task<AvailableOpponent?> GetAvailableOpponent(
        Address avatarAddress,
        int roundId,
        Address opponentAvatarAddress,
        Func<IQueryable<AvailableOpponent>, IQueryable<AvailableOpponent>>? includeQuery = null
    )
    {
        var query = _context.AvailableOpponents.AsQueryable().AsNoTracking();

        if (includeQuery != null)
        {
            query = includeQuery(query);
        }

        return await query.FirstOrDefaultAsync(ao =>
            ao.RoundId == roundId
            && ao.AvatarAddress == avatarAddress
            && ao.OpponentAvatarAddress == opponentAvatarAddress
            && ao.SuccessBattleId == null
            && ao.DeletedAt == null
        );
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

    public async Task<AvailableOpponent> UpdateAvailableOpponent(
        Address avatarAddress,
        int roundId,
        Address opponentAvatarAddress,
        Action<AvailableOpponent> updateFields
    )
    {
        var availableOpponent = await GetAvailableOpponent(
            avatarAddress,
            roundId,
            opponentAvatarAddress
        );

        if (availableOpponent is null)
        {
            throw new ArgumentException("AvailableOpponent not found");
        }

        return await UpdateAvailableOpponent(availableOpponent, updateFields);
    }

    public async Task<AvailableOpponent> UpdateAvailableOpponent(
        AvailableOpponent availableOpponent,
        Action<AvailableOpponent> updateFields
    )
    {
        updateFields(availableOpponent);

        availableOpponent.UpdatedAt = DateTime.UtcNow;

        _context.AvailableOpponents.Update(availableOpponent);
        await _context.SaveChangesAsync();

        return availableOpponent;
    }

    public async Task<int?> GetSuccessBattleId(int availableOpponentId)
    {
        return await _context.AvailableOpponents
            .Where(ao => ao.Id == availableOpponentId)
            .Select(ao => ao.SuccessBattleId)
            .SingleOrDefaultAsync();
    }
}
