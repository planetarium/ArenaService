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
        int refreshRequestId
    );
    Task AddAvailableOpponents(
        int seasonId,
        int roundId,
        Address avatarAddress,
        int requestId,
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
        int refreshRequestId
    )
    {
        var availableOpponents = await _context
            .AvailableOpponents.Where(ao =>
                ao.AvatarAddress == avatarAddress.ToHex()
                && ao.RoundId == roundId
                && ao.RefreshRequestId == refreshRequestId
            )
            .ToListAsync();

        return availableOpponents;
    }

    public async Task AddAvailableOpponents(
        int seasonId,
        int roundId,
        Address avatarAddress,
        int requestId,
        List<(Address, int)> opponents
    )
    {
        var newOpponents = new List<AvailableOpponent>();

        foreach (var opponent in opponents)
        {
            var newOpponent = new AvailableOpponent
            {
                AvatarAddress = avatarAddress.ToHex(),
                SeasonId = seasonId,
                RoundId = roundId,
                GroupId = opponent.Item2,
                RefreshRequestId = requestId,
                OpponentAvatarAddress = opponent.Item1.ToHex(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            newOpponents.Add(newOpponent);
        }

        await _context.AvailableOpponents.AddRangeAsync(newOpponents);
        await _context.SaveChangesAsync();
    }
}
