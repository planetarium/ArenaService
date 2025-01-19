namespace ArenaService.Repositories;

using ArenaService.Constants;
using ArenaService.Data;
using ArenaService.Models;
using Libplanet.Crypto;
using Libplanet.Types.Tx;
using Microsoft.EntityFrameworkCore;

public interface IRefreshRequestRepository
{
    Task<RefreshRequest> AddRefreshRequest(
        int seasonId,
        int roundId,
        Address avatarAddress,
        int refreshPriceDetailId,
        bool isCostPaid,
        RefreshStatus refreshStatus,
        TxId? txId,
        TxStatus? txStatus,
        List<Address>? opponentAvatarAddresses
    );
    Task<RefreshRequest?> GetRefreshRequestByIdAsync(
        int refreshRequestId,
        Func<IQueryable<RefreshRequest>, IQueryable<RefreshRequest>>? includeQuery = null
    );
    Task<RefreshRequest?> GetRefreshRequestByTxIdAsync(
        TxId txId,
        Func<IQueryable<RefreshRequest>, IQueryable<RefreshRequest>>? includeQuery = null
    );

    Task<List<RefreshRequest>> GetRefreshRequests(Address avatarAddress, int roundId);

    Task<int> GetRefreshRequestCount(Address avatarAddress, int roundId);

    Task<RefreshRequest> UpdateRefreshRequestAsync(
        int refreshRequestId,
        Action<RefreshRequest> updateFields
    );
    Task<RefreshRequest> UpdateRefreshRequestAsync(
        RefreshRequest refreshRequest,
        Action<RefreshRequest> updateFields
    );
}

public class RefreshRequestRepository : IRefreshRequestRepository
{
    private readonly ArenaDbContext _context;

    public RefreshRequestRepository(ArenaDbContext context)
    {
        _context = context;
    }

    public async Task<RefreshRequest> AddRefreshRequest(
        int seasonId,
        int roundId,
        Address avatarAddress,
        int refreshPriceDetailId,
        bool isCostPaid,
        RefreshStatus refreshStatus,
        TxId? txId,
        TxStatus? txStatus,
        List<Address>? opponentAvatarAddresses
    )
    {
        var refreshRequest = new RefreshRequest
        {
            RoundId = roundId,
            AvatarAddress = avatarAddress.ToHex(),
            SeasonId = seasonId,
            TxStatus = txStatus,
            RefreshStatus = refreshStatus,
            TxId = txId?.ToString(),
            IsCostPaid = isCostPaid,
            RefreshPriceDetailId = refreshPriceDetailId,
            SpecifiedOpponentAvatarAddresses = opponentAvatarAddresses is not null
                ? opponentAvatarAddresses.Select(oaa => oaa.ToHex()).ToList()
                : null,
        };

        await _context.AddAsync(refreshRequest);
        await _context.SaveChangesAsync();

        return refreshRequest;
    }

    public async Task<RefreshRequest?> GetRefreshRequestByIdAsync(
        int refreshRequestId,
        Func<IQueryable<RefreshRequest>, IQueryable<RefreshRequest>>? includeQuery = null
    )
    {
        var query = _context.RefreshRequests.AsQueryable();

        if (includeQuery != null)
        {
            query = includeQuery(query);
        }

        return await query.SingleOrDefaultAsync(r => r.Id == refreshRequestId);
    }

    public async Task<RefreshRequest?> GetRefreshRequestByTxIdAsync(
        TxId txId,
        Func<IQueryable<RefreshRequest>, IQueryable<RefreshRequest>>? includeQuery = null
    )
    {
        var query = _context.RefreshRequests.AsQueryable();

        if (includeQuery != null)
        {
            query = includeQuery(query);
        }

        return await query.SingleOrDefaultAsync(r => r.TxId == txId.ToString());
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
            rr.AvatarAddress == avatarAddress.ToHex()
            && rr.RoundId == roundId
            && rr.IsCostPaid == true
        );
    }

    public async Task<RefreshRequest> UpdateRefreshRequestAsync(
        int refreshRequestId,
        Action<RefreshRequest> updateFields
    )
    {
        var refreshRequest = await GetRefreshRequestByIdAsync(refreshRequestId);

        return await UpdateRefreshRequestAsync(refreshRequest, updateFields);
    }

    public async Task<RefreshRequest> UpdateRefreshRequestAsync(
        RefreshRequest refreshRequest,
        Action<RefreshRequest> updateFields
    )
    {
        updateFields(refreshRequest);

        refreshRequest.UpdatedAt = DateTime.UtcNow;

        _context.RefreshRequests.Update(refreshRequest);
        await _context.SaveChangesAsync();

        return refreshRequest;
    }
}
