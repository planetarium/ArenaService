namespace ArenaService.Repositories;

using ArenaService.Constants;
using ArenaService.Data;
using ArenaService.Models;
using ArenaService.Models.Enums;
using Libplanet.Crypto;
using Microsoft.EntityFrameworkCore;

public interface IBattleRepository
{
    Task<Battle> AddBattleAsync(
        int seasonId,
        Address attackerAvatarAddress,
        Address defenderAvatarAddress,
        string token
    );
    Task<Battle> UpdateTxIdAsync(int battleId, string txId);
    Task<Battle> UpdateTxStatusAsync(int battleId, TxStatus txStatus);
    Task<Battle> UpdateBattleResultAsync(
        int battleId,
        bool isVictory,
        int participantScoreChange,
        int OpponentScoreChange,
        long blockIndex
    );
    Task<Battle?> GetBattleAsync(int battleId);
}

public class BattleRepository : IBattleRepository
{
    private readonly ArenaDbContext _context;

    public BattleRepository(ArenaDbContext context)
    {
        _context = context;
    }

    public async Task<Battle> AddBattleAsync(
        int seasonId,
        Address attackerAvatarAddress,
        Address defenderAvatarAddress,
        string token
    )
    {
        var battle = await _context.Battles.AddAsync(
            new Battle
            {
                Token = token
            }
        );
        _context.SaveChanges();
        return battle.Entity;
    }

    public async Task<Battle> UpdateTxStatusAsync(int battleId, TxStatus txStatus)
    {
        var battle = await _context.Battles.FindAsync(battleId);
        if (battle == null)
        {
            throw new KeyNotFoundException($"Battle with ID {battleId} not found.");
        }

        battle.TxStatus = txStatus;

        _context.Battles.Update(battle);
        await _context.SaveChangesAsync();

        return battle;
    }

    public async Task<Battle> UpdateTxIdAsync(int battleId, string txId)
    {
        var battle = await _context.Battles.FindAsync(battleId);
        if (battle == null)
        {
            throw new KeyNotFoundException($"Battle with ID {battleId} not found.");
        }

        battle.TxId = txId;

        _context.Battles.Update(battle);
        await _context.SaveChangesAsync();

        return battle;
    }

    public async Task<Battle> UpdateBattleResultAsync(
        int battleId,
        bool isVictory,
        int participantScoreChange,
        int opponentScoreChange,
        long blockIndex
    )
    {
        var battle = await _context.Battles.FindAsync(battleId);
        if (battle == null)
        {
            throw new KeyNotFoundException($"Battle with ID {battleId} not found.");
        }

        battle.IsVictory = isVictory;
        battle.OpponentScoreChange = opponentScoreChange;

        _context.Battles.Update(battle);
        await _context.SaveChangesAsync();

        return battle;
    }

    public async Task<Battle?> GetBattleAsync(int battleId)
    {
        var battle = await _context
            .Battles
            .FirstOrDefaultAsync(b => b.Id == battleId);
        return battle;
    }
}
