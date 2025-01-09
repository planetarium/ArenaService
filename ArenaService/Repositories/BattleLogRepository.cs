namespace ArenaService.Repositories;

using ArenaService.Data;
using ArenaService.Models;
using Libplanet.Crypto;
using Microsoft.EntityFrameworkCore;

public interface IBattleLogRepository
{
    Task<BattleLog> AddBattleLogAsync(
        int seasonId,
        Address attackerAvatarAddress,
        Address defenderAvatarAddress,
        string token
    );
    Task<BattleLog> UpdateBattleResultAsync(
        int battleLogId,
        bool isVictory,
        int participantScoreChange,
        int OpponentScoreChange,
        long blockIndex
    );
    Task<BattleLog?> GetBattleLogAsync(int battleLogId);
}

public class BattleLogRepository : IBattleLogRepository
{
    private readonly ArenaDbContext _context;

    public BattleLogRepository(ArenaDbContext context)
    {
        _context = context;
    }

    public async Task<BattleLog> AddBattleLogAsync(
        int seasonId,
        Address attackerAvatarAddress,
        Address defenderAvatarAddress,
        string token
    )
    {
        var battleLog = await _context.BattleLogs.AddAsync(
            new BattleLog
            {
                SeasonId = seasonId,
                AttackerAvatarAddress = attackerAvatarAddress.ToHex(),
                DefenderAvatarAddress = defenderAvatarAddress.ToHex(),
                Token = token
            }
        );
        _context.SaveChanges();
        return battleLog.Entity;
    }

    public async Task<BattleLog> UpdateBattleResultAsync(
        int battleLogId,
        bool isVictory,
        int participantScoreChange,
        int opponentScoreChange,
        long blockIndex
    )
    {
        var battleLog = await _context.BattleLogs.FindAsync(battleLogId);
        if (battleLog == null)
        {
            throw new KeyNotFoundException($"BattleLog with ID {battleLogId} not found.");
        }

        battleLog.IsVictory = isVictory;
        battleLog.ParticipantScoreChange = participantScoreChange;
        battleLog.OpponentScoreChange = opponentScoreChange;
        battleLog.BattleBlockIndex = blockIndex;

        _context.BattleLogs.Update(battleLog);
        await _context.SaveChangesAsync();

        return battleLog;
    }

    public async Task<BattleLog?> GetBattleLogAsync(int battleLogId)
    {
        var battleLog = await _context
            .BattleLogs.Include(b => b.Attacker)
            .Include(b => b.Defender)
            .FirstOrDefaultAsync(b => b.Id == battleLogId);
        return battleLog;
    }
}
