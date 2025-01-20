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
    Task<Battle> UpdateTxIdAsync(int battleLogId, string txId);
    Task<Battle> UpdateTxStatusAsync(int battleLogId, TxStatus txStatus);
    Task<Battle> UpdateBattleResultAsync(
        int battleLogId,
        bool isVictory,
        int participantScoreChange,
        int OpponentScoreChange,
        long blockIndex
    );
    Task<Battle?> GetBattleAsync(int battleLogId);
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
        var battleLog = await _context.Battles.AddAsync(
            new Battle
            {
                Token = token
            }
        );
        _context.SaveChanges();
        return battleLog.Entity;
    }

    public async Task<Battle> UpdateTxStatusAsync(int battleLogId, TxStatus txStatus)
    {
        var battleLog = await _context.Battles.FindAsync(battleLogId);
        if (battleLog == null)
        {
            throw new KeyNotFoundException($"Battle with ID {battleLogId} not found.");
        }

        battleLog.TxStatus = txStatus;

        _context.Battles.Update(battleLog);
        await _context.SaveChangesAsync();

        return battleLog;
    }

    public async Task<Battle> UpdateTxIdAsync(int battleLogId, string txId)
    {
        var battleLog = await _context.Battles.FindAsync(battleLogId);
        if (battleLog == null)
        {
            throw new KeyNotFoundException($"Battle with ID {battleLogId} not found.");
        }

        battleLog.TxId = txId;

        _context.Battles.Update(battleLog);
        await _context.SaveChangesAsync();

        return battleLog;
    }

    public async Task<Battle> UpdateBattleResultAsync(
        int battleLogId,
        bool isVictory,
        int participantScoreChange,
        int opponentScoreChange,
        long blockIndex
    )
    {
        var battleLog = await _context.Battles.FindAsync(battleLogId);
        if (battleLog == null)
        {
            throw new KeyNotFoundException($"Battle with ID {battleLogId} not found.");
        }

        battleLog.IsVictory = isVictory;
        battleLog.OpponentScoreChange = opponentScoreChange;

        _context.Battles.Update(battleLog);
        await _context.SaveChangesAsync();

        return battleLog;
    }

    public async Task<Battle?> GetBattleAsync(int battleLogId)
    {
        var battleLog = await _context
            .Battles
            .FirstOrDefaultAsync(b => b.Id == battleLogId);
        return battleLog;
    }
}
