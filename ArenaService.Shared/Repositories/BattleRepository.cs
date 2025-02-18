namespace ArenaService.Shared.Repositories;

using ArenaService.Shared.Constants;
using ArenaService.Shared.Data;
using ArenaService.Shared.Jwt;
using ArenaService.Shared.Models;
using ArenaService.Shared.Models.Enums;
using Libplanet.Crypto;
using Libplanet.Types.Tx;
using Microsoft.EntityFrameworkCore;

public interface IBattleRepository
{
    Task<Battle> AddBattleAsync(
        Address avatarAddress,
        int seasonId,
        int roundId,
        int availableOpponentId
    );
    Task<Battle> UpdateBattle(int battleId, Action<Battle> updateFields);

    Task<Battle> UpdateBattle(Battle battle, Action<Battle> updateFields);
    Task<Battle?> GetBattleAsync(
        int battleId,
        Func<IQueryable<Battle>, IQueryable<Battle>>? includeQuery = null
    );
    Task<Battle?> GetBattleByTxId(TxId txId, int battleId);
    Task<List<Battle>> GetInProgressBattles(
        Address avatarAddress,
        Address opponentAvatarAddress,
        int seasonId,
        int roundId
    );
}

public class BattleRepository : IBattleRepository
{
    private readonly ArenaDbContext _context;
    private readonly BattleTokenGenerator _battleTokenGenerator;

    public BattleRepository(ArenaDbContext context, BattleTokenGenerator battleTokenGenerator)
    {
        _context = context;
        _battleTokenGenerator = battleTokenGenerator;
    }

    public async Task<Battle> AddBattleAsync(
        Address avatarAddress,
        int seasonId,
        int roundId,
        int availableOpponentId
    )
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        var battle = await _context.Battles.AddAsync(
            new Battle
            {
                Token = "temp token",
                SeasonId = seasonId,
                RoundId = roundId,
                AvatarAddress = avatarAddress,
                AvailableOpponentId = availableOpponentId,
                BattleStatus = BattleStatus.TOKEN_ISSUED
            }
        );
        await _context.SaveChangesAsync();

        battle.Entity.Token = _battleTokenGenerator.GenerateBattleToken(battle.Entity.Id);
        _context.Battles.Update(battle.Entity);

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return battle.Entity;
    }

    public async Task<Battle> UpdateBattle(int battleId, Action<Battle> updateFields)
    {
        var battle = await GetBattleAsync(battleId);

        if (battle is null)
        {
            throw new ArgumentException($"Battle not found for battleId {battleId}");
        }

        return await UpdateBattle(battle, updateFields);
    }

    public async Task<Battle> UpdateBattle(Battle battle, Action<Battle> updateFields)
    {
        updateFields(battle);

        battle.UpdatedAt = DateTime.UtcNow;

        _context.Battles.Update(battle);
        await _context.SaveChangesAsync();

        return battle;
    }

    public async Task<Battle?> GetBattleAsync(
        int battleId,
        Func<IQueryable<Battle>, IQueryable<Battle>>? includeQuery = null
    )
    {
        var query = _context.Battles.AsQueryable();

        if (includeQuery != null)
        {
            query = includeQuery(query);
        }

        return await query.SingleOrDefaultAsync(b => b.Id == battleId);
    }

    public async Task<Battle?> GetBattleByTxId(TxId txId, int battleId)
    {
        var battle = await _context.Battles.SingleOrDefaultAsync(b =>
            b.TxId == txId && b.Id != battleId
        );
        return battle;
    }

    public async Task<List<Battle>> GetInProgressBattles(
        Address avatarAddress,
        Address opponentAvatarAddress,
        int seasonId,
        int roundId
    )
    {
        var fiveMinutesAgo = DateTime.UtcNow.AddMinutes(-5);

        var battles = await _context
            .Battles.Where(b =>
                b.AvatarAddress == avatarAddress
                && b.SeasonId == seasonId
                && b.RoundId == roundId
                && b.CreatedAt >= fiveMinutesAgo
                && b.AvailableOpponent.OpponentAvatarAddress == opponentAvatarAddress
                && (
                    b.BattleStatus == BattleStatus.TOKEN_ISSUED
                    || b.BattleStatus == BattleStatus.TRACKING
                )
            )
            .Include(b => b.AvailableOpponent)
            .ToListAsync();

        return battles;
    }
}
