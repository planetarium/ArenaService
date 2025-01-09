namespace ArenaService.Repositories;

using ArenaService.Data;
using ArenaService.Models;
using Libplanet.Crypto;
using Microsoft.EntityFrameworkCore;

public interface IAvailableOpponentRepository
{
    Task<AvailableOpponent?> GetAvailableOpponents(
        Address participantAvatarAddress,
        int seasonId,
        int arenaIntervalId
    );
    Task<AvailableOpponent> AddAvailableOpponents(
        Address participantAvatarAddress,
        int seasonId,
        int arenaIntervalId,
        List<Address> opponentAvatarAddresses
    );
}

public class AvailableOpponentRepository : IAvailableOpponentRepository
{
    private readonly ArenaDbContext _context;

    public AvailableOpponentRepository(ArenaDbContext context)
    {
        _context = context;
    }

    public async Task<AvailableOpponent?> GetAvailableOpponents(
        Address participantAvatarAddress,
        int seasonId,
        int arenaIntervalId
    )
    {
        var availableOpponents = await _context
            .AvailableOpponents.Where(ao =>
                ao.SeasonId == seasonId
                && ao.ParticipantAvatarAddress == participantAvatarAddress.ToHex()
                && ao.IntervalId == arenaIntervalId
            )
            .FirstOrDefaultAsync();

        return availableOpponents;
    }

    public async Task<AvailableOpponent> AddAvailableOpponents(
        Address participantAvatarAddress,
        int seasonId,
        int arenaIntervalId,
        List<Address> opponentAvatarAddresses
    )
    {
        var ao = await _context.AvailableOpponents.AddAsync(
            new AvailableOpponent
            {
                SeasonId = seasonId,
                ParticipantAvatarAddress = participantAvatarAddress.ToHex(),
                IntervalId = arenaIntervalId,
                OpponentAvatarAddresses = opponentAvatarAddresses
                    .Select(oaa => oaa.ToHex())
                    .ToList(),
                UpdateSource = UpdateSource.FREE,
                CostPaid = "0",
            }
        );
        _context.SaveChanges();
        return ao.Entity;
    }
}
