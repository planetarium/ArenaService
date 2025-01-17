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
    Task<RefreshRequest> AddRefreshRequest(
        int seasonId,
        int roundId,
        Address avatarAddress,
        int refreshPriceDetailId,
        TxStatus? txStatus,
        TxId? txId,
        List<Address> opponentAvatarAddresses
    );
    Task<List<RefreshRequest>> GetRefreshRequests(Address avatarAddress, int roundId);
    Task<int> GetRefreshRequestCount(Address avatarAddress, int roundId);
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
        var relations = new List<AvailableOpponentsRefreshRequest>();

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

        foreach (var newOpponent in newOpponents)
        {
            var refreshRequest = new AvailableOpponentsRefreshRequest
            {
                AvailableOpponentId = newOpponent.Id,
                RefreshRequestId = requestId,
                CreatedAt = DateTime.UtcNow
            };

            relations.Add(refreshRequest);
        }

        await _context.AvailableOpponentsRefreshRequests.AddRangeAsync(relations);
        await _context.SaveChangesAsync();
    }

    public async Task<RefreshRequest> AddRefreshRequest(
        int seasonId,
        int roundId,
        Address avatarAddress,
        int refreshPriceDetailId,
        TxStatus? txStatus,
        TxId? txId,
        List<Address> opponentAvatarAddresses
    )
    {
        var refreshRequest = new RefreshRequest
        {
            RoundId = roundId,
            AvatarAddress = avatarAddress.ToHex(),
            SeasonId = seasonId,
            TxStatus = txStatus,
            TxId = txId?.ToString(),
            RefreshPriceDetailId = refreshPriceDetailId,
            SpecifiedOpponentAvatarAddresses = opponentAvatarAddresses
                .Select(oaa => oaa.ToHex())
                .ToList(),
        };

        await _context.AddAsync(refreshRequest);
        await _context.SaveChangesAsync();

        return refreshRequest;
    }

    public async Task<List<RefreshRequest>> GetRefreshRequests(Address avatarAddress, int roundId)
    {
        var refreshRequests = await _context
            .RefreshRequests.Where(ao =>
                ao.AvatarAddress == avatarAddress.ToHex() && ao.RoundId == roundId
            )
            .ToListAsync();

        return refreshRequests;
    }

    public async Task<int> GetRefreshRequestCount(Address avatarAddress, int roundId)
    {
        return await _context.RefreshRequests.CountAsync(rr =>
            rr.AvatarAddress == avatarAddress.ToHex() && rr.RoundId == roundId
        );
    }
}
