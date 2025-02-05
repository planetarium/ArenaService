namespace ArenaService.Repositories;

using ArenaService.Constants;
using ArenaService.Data;
using ArenaService.Models;
using ArenaService.Models.Enums;
using Libplanet.Crypto;
using Libplanet.Types.Tx;
using Microsoft.EntityFrameworkCore;

public interface IBattleRepository
{
    Task<Battle> AddBattleAsync(
        Address avatarAddress,
        int seasonId,
        int roundId,
        int availableOpponentId,
        string token
    );
    Task<Battle> UpdateBattle(int battleId, Action<Battle> updateFields);

    Task<Battle> UpdateBattle(Battle battle, Action<Battle> updateFields);
    Task<Battle?> GetBattleAsync(
        int battleId,
        Func<IQueryable<Battle>, IQueryable<Battle>>? includeQuery = null
    );
    Task<Battle?> GetBattleByTxId(TxId txId, int battleId);
    Task<List<Battle>> GetInProgressBattles(Address avatarAddress, int seasonId, int roundId);
}

public class BattleRepository : IBattleRepository
{
    private readonly ArenaDbContext _context;

    public BattleRepository(ArenaDbContext context)
    {
        _context = context;
    }

    public async Task<Battle> AddBattleAsync(
        Address avatarAddress,
        int seasonId,
        int roundId,
        int availableOpponentId,
        string token
    )
    {
        var battle = await _context.Battles.AddAsync(
            new Battle
            {
                Token = token,
                SeasonId = seasonId,
                RoundId = roundId,
                AvatarAddress = avatarAddress,
                AvailableOpponentId = availableOpponentId,
                BattleStatus = BattleStatus.TOKEN_ISSUED
            }
        );
        _context.SaveChanges();
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
        int seasonId,
        int roundId
    )
    {
        var battles = await _context
            .Battles.Where(b =>
                b.AvatarAddress == avatarAddress
                && b.SeasonId == seasonId
                && b.RoundId == roundId
                && (
                    b.BattleStatus == BattleStatus.TOKEN_ISSUED
                    || b.BattleStatus == BattleStatus.TRACKING
                )
            )
            .ToListAsync();

        return battles;
    }
}
