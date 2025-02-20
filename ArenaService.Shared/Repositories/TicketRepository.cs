namespace ArenaService.Shared.Repositories;

using ArenaService.Shared.Constants;
using ArenaService.Shared.Data;
using ArenaService.Shared.Models;
using ArenaService.Shared.Models.BattleTicket;
using ArenaService.Shared.Models.Enums;
using ArenaService.Shared.Models.RefreshTicket;
using Libplanet.Crypto;
using Libplanet.Types.Tx;
using Microsoft.EntityFrameworkCore;

public interface ITicketRepository
{
    Task<List<BattleTicketPurchaseLog>> GetUnReviewedBattleTicketPurchasesAsync();
    Task<List<RefreshTicketPurchaseLog>> GetUnReviewedRefreshTicketPurchasesAsync();
    Task<RefreshTicketStatusPerRound> AddRefreshTicketStatusPerRound(
        int seasonId,
        int roundId,
        Address avatarAddress,
        int refreshTicketPolicyId,
        int remainingCount,
        int usedCount,
        int purchaseCount
    );

    Task<RefreshTicketStatusPerRound?> GetRefreshTicketStatusPerRound(
        int roundId,
        Address avatarAddress
    );
    Task<BattleTicketStatusPerRound?> GetBattleTicketStatusPerRound(
        int roundId,
        Address avatarAddress
    );

    Task<BattleTicketStatusPerSeason?> GetBattleTicketStatusPerSeason(
        int seasonId,
        Address avatarAddress
    );

    Task<BattleTicketStatusPerRound> AddBattleTicketStatusPerRound(
        int seasonId,
        int roundId,
        Address avatarAddress,
        int battleTicketPolicyId,
        int remainingCount,
        int usedCount,
        int purchaseCount
    );

    Task<BattleTicketStatusPerSeason> AddBattleTicketStatusPerSeason(
        int seasonId,
        Address avatarAddress,
        int battleTicketPolicyId,
        int usedCount,
        int purchaseCount
    );

    Task<BattleTicketPurchaseLog?> GetBattleTicketPurchaseLogById(int purchaseLogId);
    Task<RefreshTicketPurchaseLog?> GetRefreshTicketPurchaseLogById(int purchaseLogId);
    Task<BattleTicketPurchaseLog?> GetBattleTicketPurchaseLogByTxId(TxId txId, int? purchaseLogId);
    Task<RefreshTicketPurchaseLog?> GetRefreshTicketPurchaseLogByTxId(
        TxId txId,
        int? purchaseLogId
    );

    Task<BattleTicketPurchaseLog> AddBattleTicketPurchaseLog(
        int seasonId,
        int roundId,
        Address avatarAddress,
        TxId txId,
        int purchaseCount
    );

    Task<RefreshTicketPurchaseLog> AddRefreshTicketPurchaseLog(
        int seasonId,
        int roundId,
        Address avatarAddress,
        TxId txId,
        int purchaseCount
    );

    Task<BattleTicketPurchaseLog> UpdateBattleTicketPurchaseLog(
        int purchaseLogId,
        Action<BattleTicketPurchaseLog> updateFields
    );

    Task<BattleTicketPurchaseLog> UpdateBattleTicketPurchaseLog(
        BattleTicketPurchaseLog purchaseLogId,
        Action<BattleTicketPurchaseLog> updateFields
    );

    Task<RefreshTicketPurchaseLog> UpdateRefreshTicketPurchaseLog(
        int purchaseLogId,
        Action<RefreshTicketPurchaseLog> updateFields
    );

    Task<RefreshTicketPurchaseLog> UpdateRefreshTicketPurchaseLog(
        RefreshTicketPurchaseLog purchaseLogId,
        Action<RefreshTicketPurchaseLog> updateFields
    );

    Task<RefreshTicketStatusPerRound> UpdateRefreshTicketStatusPerRound(
        int roundId,
        Address avatarAddress,
        Action<RefreshTicketStatusPerRound> updateFields
    );

    Task<RefreshTicketStatusPerRound> UpdateRefreshTicketStatusPerRound(
        RefreshTicketStatusPerRound refreshTicketStatusPerRound,
        Action<RefreshTicketStatusPerRound> updateFields
    );

    Task<BattleTicketStatusPerRound> UpdateBattleTicketStatusPerRound(
        int roundId,
        Address avatarAddress,
        Action<BattleTicketStatusPerRound> updateFields
    );

    Task<BattleTicketStatusPerSeason> UpdateBattleTicketStatusPerSeason(
        int seasonId,
        Address avatarAddress,
        Action<BattleTicketStatusPerSeason> updateFields
    );

    Task<BattleTicketStatusPerRound> UpdateBattleTicketStatusPerRound(
        BattleTicketStatusPerRound battleTicketStatusPerRound,
        Action<BattleTicketStatusPerRound> updateFields
    );

    Task<BattleTicketStatusPerSeason> UpdateBattleTicketStatusPerSeason(
        BattleTicketStatusPerSeason battleTicketStatusPerSeason,
        Action<BattleTicketStatusPerSeason> updateFields
    );

    Task<RefreshTicketUsageLog> AddRefreshTicketUsageLog(
        int refreshTicketStatusPerRoundId,
        List<int> specifiedOpponentIds
    );

    Task<BattleTicketUsageLog> AddBattleTicketUsageLog(
        int battleTicketStatusPerRoundId,
        int battleTicketStatusPerSeasonId,
        int battleId
    );

    Task<List<BattleTicketPurchaseLog>> GetInProgressBattleTicketPurchases(
        Address avatarAddress,
        int seasonId,
        int roundId
    );

    Task<List<RefreshTicketPurchaseLog>> GetInProgressRefreshTicketPurchases(
        Address avatarAddress,
        int seasonId,
        int roundId
    );
}

public class TicketRepository : ITicketRepository
{
    private readonly ArenaDbContext _context;

    public TicketRepository(ArenaDbContext context)
    {
        _context = context;
    }

    public async Task<BattleTicketStatusPerSeason?> GetBattleTicketStatusPerSeason(
        int seasonId,
        Address avatarAddress
    )
    {
        return await _context
            .BattleTicketStatusesPerSeason.Include(bts => bts.BattleTicketPolicy)
            .SingleOrDefaultAsync(bts =>
                bts.SeasonId == seasonId && bts.AvatarAddress == avatarAddress
            );
    }

    public async Task<BattleTicketStatusPerRound?> GetBattleTicketStatusPerRound(
        int roundId,
        Address avatarAddress
    )
    {
        return await _context
            .BattleTicketStatusesPerRound.Include(bts => bts.BattleTicketPolicy)
            .SingleOrDefaultAsync(bts =>
                bts.RoundId == roundId && bts.AvatarAddress == avatarAddress
            );
    }

    public async Task<RefreshTicketStatusPerRound?> GetRefreshTicketStatusPerRound(
        int roundId,
        Address avatarAddress
    )
    {
        return await _context
            .RefreshTicketStatusesPerRound.Include(bts => bts.RefreshTicketPolicy)
            .SingleOrDefaultAsync(bts =>
                bts.RoundId == roundId && bts.AvatarAddress == avatarAddress
            );
    }

    public async Task<BattleTicketPurchaseLog> AddBattleTicketPurchaseLog(
        int seasonId,
        int roundId,
        Address avatarAddress,
        TxId txId,
        int purchaseCount
    )
    {
        var battleTicketPurchaseLog = await _context.BattleTicketPurchaseLogs.AddAsync(
            new BattleTicketPurchaseLog
            {
                AvatarAddress = avatarAddress,
                SeasonId = seasonId,
                RoundId = roundId,
                PurchaseStatus = PurchaseStatus.PENDING,
                TxId = txId,
                TxStatus = null,
                PurchaseCount = purchaseCount,
            }
        );
        await _context.SaveChangesAsync();
        return battleTicketPurchaseLog.Entity;
    }

    public async Task<RefreshTicketPurchaseLog> AddRefreshTicketPurchaseLog(
        int seasonId,
        int roundId,
        Address avatarAddress,
        TxId txId,
        int purchaseCount
    )
    {
        var refreshTicketPurchaseLog = await _context.RefreshTicketPurchaseLogs.AddAsync(
            new RefreshTicketPurchaseLog
            {
                AvatarAddress = avatarAddress,
                SeasonId = seasonId,
                RoundId = roundId,
                PurchaseStatus = PurchaseStatus.PENDING,
                TxId = txId,
                TxStatus = null,
                PurchaseCount = purchaseCount,
            }
        );
        await _context.SaveChangesAsync();
        return refreshTicketPurchaseLog.Entity;
    }

    public async Task<BattleTicketPurchaseLog?> GetBattleTicketPurchaseLogById(int purchaseLogId)
    {
        return await _context.BattleTicketPurchaseLogs.SingleOrDefaultAsync(btpl =>
            btpl.Id == purchaseLogId
        );
    }

    public async Task<BattleTicketPurchaseLog?> GetBattleTicketPurchaseLogByTxId(
        TxId txId,
        int? purchaseLogId
    )
    {
        return await _context.BattleTicketPurchaseLogs.FirstOrDefaultAsync(btpl =>
            btpl.TxId == txId && (purchaseLogId == null || btpl.Id != purchaseLogId)
        );
    }

    public async Task<RefreshTicketPurchaseLog?> GetRefreshTicketPurchaseLogByTxId(
        TxId txId,
        int? purchaseLogId
    )
    {
        return await _context.RefreshTicketPurchaseLogs.FirstOrDefaultAsync(rtpl =>
            rtpl.TxId == txId && (purchaseLogId == null || rtpl.Id != purchaseLogId)
        );
    }

    public async Task<BattleTicketPurchaseLog> UpdateBattleTicketPurchaseLog(
        int purchaseLogId,
        Action<BattleTicketPurchaseLog> updateFields
    )
    {
        var battleTicketPurchaseLog = await GetBattleTicketPurchaseLogById(purchaseLogId);

        if (battleTicketPurchaseLog is null)
        {
            throw new ArgumentException($"Notfound battleTicketPurchaseLog {purchaseLogId}");
        }

        return await UpdateBattleTicketPurchaseLog(battleTicketPurchaseLog, updateFields);
    }

    public async Task<BattleTicketPurchaseLog> UpdateBattleTicketPurchaseLog(
        BattleTicketPurchaseLog purchaseLog,
        Action<BattleTicketPurchaseLog> updateFields
    )
    {
        updateFields(purchaseLog);

        purchaseLog.UpdatedAt = DateTime.UtcNow;

        _context.BattleTicketPurchaseLogs.Update(purchaseLog);
        await _context.SaveChangesAsync();

        return purchaseLog;
    }

    public async Task<RefreshTicketPurchaseLog> UpdateRefreshTicketPurchaseLog(
        int purchaseLogId,
        Action<RefreshTicketPurchaseLog> updateFields
    )
    {
        var refreshTicketPurchaseLog = await GetRefreshTicketPurchaseLogById(purchaseLogId);

        if (refreshTicketPurchaseLog is null)
        {
            throw new ArgumentException($"Notfound refreshTicketPurchaseLog {purchaseLogId}");
        }

        return await UpdateRefreshTicketPurchaseLog(refreshTicketPurchaseLog, updateFields);
    }

    public async Task<RefreshTicketPurchaseLog> UpdateRefreshTicketPurchaseLog(
        RefreshTicketPurchaseLog purchaseLog,
        Action<RefreshTicketPurchaseLog> updateFields
    )
    {
        updateFields(purchaseLog);

        purchaseLog.UpdatedAt = DateTime.UtcNow;

        _context.RefreshTicketPurchaseLogs.Update(purchaseLog);
        await _context.SaveChangesAsync();

        return purchaseLog;
    }

    public async Task<RefreshTicketStatusPerRound> AddRefreshTicketStatusPerRound(
        int seasonId,
        int roundId,
        Address avatarAddress,
        int refreshTicketPolicyId,
        int remainingCount,
        int usedCount,
        int purchaseCount
    )
    {
        var refreshTicketStatusPerRound = await _context.RefreshTicketStatusesPerRound.AddAsync(
            new RefreshTicketStatusPerRound
            {
                AvatarAddress = avatarAddress,
                SeasonId = seasonId,
                RoundId = roundId,
                RefreshTicketPolicyId = refreshTicketPolicyId,
                RemainingCount = remainingCount,
                UsedCount = usedCount,
                PurchaseCount = purchaseCount
            }
        );
        await _context.SaveChangesAsync();
        return refreshTicketStatusPerRound.Entity;
    }

    public async Task<BattleTicketStatusPerRound> AddBattleTicketStatusPerRound(
        int seasonId,
        int roundId,
        Address avatarAddress,
        int battleTicketPolicyId,
        int remainingCount,
        int usedCount,
        int purchaseCount
    )
    {
        var battleTicketStatusPerRound = await _context.BattleTicketStatusesPerRound.AddAsync(
            new BattleTicketStatusPerRound
            {
                AvatarAddress = avatarAddress,
                SeasonId = seasonId,
                RoundId = roundId,
                BattleTicketPolicyId = battleTicketPolicyId,
                RemainingCount = remainingCount,
                UsedCount = usedCount,
                PurchaseCount = purchaseCount
            }
        );
        await _context.SaveChangesAsync();
        return battleTicketStatusPerRound.Entity;
    }

    public async Task<BattleTicketStatusPerSeason> AddBattleTicketStatusPerSeason(
        int seasonId,
        Address avatarAddress,
        int battleTicketPolicyId,
        int usedCount,
        int purchaseCount
    )
    {
        var battleTicketStatusPerSeason = await _context.BattleTicketStatusesPerSeason.AddAsync(
            new BattleTicketStatusPerSeason
            {
                AvatarAddress = avatarAddress,
                SeasonId = seasonId,
                BattleTicketPolicyId = battleTicketPolicyId,
                UsedCount = usedCount,
                PurchaseCount = purchaseCount
            }
        );
        await _context.SaveChangesAsync();
        return battleTicketStatusPerSeason.Entity;
    }

    public async Task<RefreshTicketStatusPerRound> UpdateRefreshTicketStatusPerRound(
        int roundId,
        Address avatarAddress,
        Action<RefreshTicketStatusPerRound> updateFields
    )
    {
        var refreshTicketStatusPerRound = await GetRefreshTicketStatusPerRound(
            roundId,
            avatarAddress
        );

        if (refreshTicketStatusPerRound is null)
        {
            throw new ArgumentException(
                $"RefreshTicketStatusPerRound not found for roundId {roundId} and avatarAddress {avatarAddress}"
            );
        }

        return await UpdateRefreshTicketStatusPerRound(refreshTicketStatusPerRound, updateFields);
    }

    public async Task<RefreshTicketStatusPerRound> UpdateRefreshTicketStatusPerRound(
        RefreshTicketStatusPerRound refreshTicketStatusPerRound,
        Action<RefreshTicketStatusPerRound> updateFields
    )
    {
        updateFields(refreshTicketStatusPerRound);

        refreshTicketStatusPerRound.UpdatedAt = DateTime.UtcNow;

        _context.RefreshTicketStatusesPerRound.Update(refreshTicketStatusPerRound);
        await _context.SaveChangesAsync();

        return refreshTicketStatusPerRound;
    }

    public async Task<BattleTicketStatusPerRound> UpdateBattleTicketStatusPerRound(
        int roundId,
        Address avatarAddress,
        Action<BattleTicketStatusPerRound> updateFields
    )
    {
        var battleTicketStatusPerRound = await GetBattleTicketStatusPerRound(
            roundId,
            avatarAddress
        );

        if (battleTicketStatusPerRound is null)
        {
            throw new ArgumentException(
                $"BattleTicketStatusPerRound not found for roundId {roundId} and avatarAddress {avatarAddress}"
            );
        }

        return await UpdateBattleTicketStatusPerRound(battleTicketStatusPerRound, updateFields);
    }

    public async Task<BattleTicketStatusPerSeason> UpdateBattleTicketStatusPerSeason(
        int seasonId,
        Address avatarAddress,
        Action<BattleTicketStatusPerSeason> updateFields
    )
    {
        var battleTicketStatusPerSeason = await GetBattleTicketStatusPerSeason(
            seasonId,
            avatarAddress
        );

        if (battleTicketStatusPerSeason is null)
        {
            throw new ArgumentException(
                $"BattleTicketStatusPerSeason not found for seasonId {seasonId} and avatarAddress {avatarAddress}"
            );
        }

        return await UpdateBattleTicketStatusPerSeason(battleTicketStatusPerSeason, updateFields);
    }

    public async Task<RefreshTicketPurchaseLog?> GetRefreshTicketPurchaseLogById(int purchaseLogId)
    {
        return await _context.RefreshTicketPurchaseLogs.SingleOrDefaultAsync(rtpl =>
            rtpl.Id == purchaseLogId
        );
    }

    public async Task<RefreshTicketUsageLog> AddRefreshTicketUsageLog(
        int refreshTicketStatusPerRoundId,
        List<int> specifiedOpponentIds
    )
    {
        var refreshTicketUsageLog = await _context.RefreshTicketUsageLogs.AddAsync(
            new RefreshTicketUsageLog
            {
                RefreshTicketStatusPerRoundId = refreshTicketStatusPerRoundId,
                SpecifiedOpponentIds = specifiedOpponentIds
            }
        );
        await _context.SaveChangesAsync();
        return refreshTicketUsageLog.Entity;
    }

    public async Task<BattleTicketUsageLog> AddBattleTicketUsageLog(
        int battleTicketStatusPerRoundId,
        int battleTicketStatusPerSeasonId,
        int battleId
    )
    {
        var battleTicketUsageLog = await _context.BattleTicketUsageLogs.AddAsync(
            new BattleTicketUsageLog
            {
                BattleTicketStatusPerSeasonId = battleTicketStatusPerSeasonId,
                BattleTicketStatusPerRoundId = battleTicketStatusPerRoundId,
                BattleId = battleId
            }
        );
        await _context.SaveChangesAsync();
        return battleTicketUsageLog.Entity;
    }

    public async Task<BattleTicketStatusPerRound> UpdateBattleTicketStatusPerRound(
        BattleTicketStatusPerRound battleTicketStatusPerRound,
        Action<BattleTicketStatusPerRound> updateFields
    )
    {
        updateFields(battleTicketStatusPerRound);

        battleTicketStatusPerRound.UpdatedAt = DateTime.UtcNow;

        _context.BattleTicketStatusesPerRound.Update(battleTicketStatusPerRound);
        await _context.SaveChangesAsync();

        return battleTicketStatusPerRound;
    }

    public async Task<BattleTicketStatusPerSeason> UpdateBattleTicketStatusPerSeason(
        BattleTicketStatusPerSeason battleTicketStatusPerSeason,
        Action<BattleTicketStatusPerSeason> updateFields
    )
    {
        updateFields(battleTicketStatusPerSeason);

        battleTicketStatusPerSeason.UpdatedAt = DateTime.UtcNow;

        _context.BattleTicketStatusesPerSeason.Update(battleTicketStatusPerSeason);
        await _context.SaveChangesAsync();

        return battleTicketStatusPerSeason;
    }

    public async Task<List<BattleTicketPurchaseLog>> GetInProgressBattleTicketPurchases(
        Address avatarAddress,
        int seasonId,
        int roundId
    )
    {
        var fiveMinutesAgo = DateTime.UtcNow.AddMinutes(-5);

        var purchases = await _context
            .BattleTicketPurchaseLogs.Where(l =>
                l.AvatarAddress == avatarAddress
                && l.SeasonId == seasonId
                && l.RoundId == roundId
                && l.CreatedAt >= fiveMinutesAgo
                && (
                    l.PurchaseStatus == PurchaseStatus.PENDING
                    || l.PurchaseStatus == PurchaseStatus.TRACKING
                )
            )
            .ToListAsync();

        return purchases;
    }

    public async Task<List<RefreshTicketPurchaseLog>> GetInProgressRefreshTicketPurchases(
        Address avatarAddress,
        int seasonId,
        int roundId
    )
    {
        var fiveMinutesAgo = DateTime.UtcNow.AddMinutes(-5);

        var purchases = await _context
            .RefreshTicketPurchaseLogs.Where(l =>
                l.AvatarAddress == avatarAddress
                && l.SeasonId == seasonId
                && l.RoundId == roundId
                && l.CreatedAt >= fiveMinutesAgo
                && (
                    l.PurchaseStatus == PurchaseStatus.PENDING
                    || l.PurchaseStatus == PurchaseStatus.TRACKING
                )
            )
            .ToListAsync();

        return purchases;
    }

    public async Task<List<BattleTicketPurchaseLog>> GetUnReviewedBattleTicketPurchasesAsync()
    {
        return await _context
            .BattleTicketPurchaseLogs.Where(log =>
                log.PurchaseStatus != PurchaseStatus.PENDING
                && log.PurchaseStatus != PurchaseStatus.TRACKING
                && log.PurchaseStatus != PurchaseStatus.SUCCESS
                && log.Reviewed == null
            )
            .ToListAsync();
    }

    public async Task<List<RefreshTicketPurchaseLog>> GetUnReviewedRefreshTicketPurchasesAsync()
    {
        return await _context
            .RefreshTicketPurchaseLogs.Where(log =>
                log.PurchaseStatus != PurchaseStatus.PENDING
                && log.PurchaseStatus != PurchaseStatus.TRACKING
                && log.PurchaseStatus != PurchaseStatus.SUCCESS
                && log.Reviewed == null
            )
            .ToListAsync();
    }
}
