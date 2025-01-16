namespace ArenaService.Repositories;

using ArenaService.Data;
using ArenaService.Models;
using Libplanet.Crypto;
using Microsoft.EntityFrameworkCore;

public interface IAvailableOpponentRepository
{
    Task<AvailableOpponents?> GetAvailableOpponents(Address avatarAddress, int roundId);
    Task<AvailableOpponents> AddAvailableOpponents(
        Address avatarAddress,
        int roundId,
        List<Address> opponentAvatarAddresses
    );
    Task<List<AvailableOpponentsRequest>> GetAvailableOpponentsRequests(
        Address avatarAddress,
        int roundId
    );
}

public class AvailableOpponentRepository : IAvailableOpponentRepository
{
    private readonly ArenaDbContext _context;

    public AvailableOpponentRepository(ArenaDbContext context)
    {
        _context = context;
    }

    public async Task<AvailableOpponents?> GetAvailableOpponents(Address avatarAddress, int roundId)
    {
        var availableOpponents = await _context
            .AvailableOpponents.Where(ao =>
                ao.AvatarAddress == avatarAddress.ToHex() && ao.RoundId == roundId
            )
            .FirstOrDefaultAsync();

        return availableOpponents;
    }

    public async Task<AvailableOpponents> AddAvailableOpponents(
        Address avatarAddress,
        int roundId,
        List<Address> opponentAvatarAddresses
    )
    {
        var ao = await _context.AvailableOpponents.AddAsync(
            new AvailableOpponents
            {
                AvatarAddress = avatarAddress.ToHex(),
                RoundId = roundId,
                OpponentAvatarAddresses = opponentAvatarAddresses
                    .Select(oaa => oaa.ToHex())
                    .ToList(),
            }
        );
        _context.SaveChanges();
        return ao.Entity;
    }

    public async Task<List<AvailableOpponentsRequest>> GetAvailableOpponentsRequests(
        Address avatarAddress,
        int roundId
    )
    {
        var availableOpponentsRequests = await _context
            .AvailableOpponentsRequests.Where(ao =>
                ao.AvatarAddress == avatarAddress.ToHex() && ao.RoundId == roundId
            )
            .ToListAsync();

        return availableOpponentsRequests;
    }
}
