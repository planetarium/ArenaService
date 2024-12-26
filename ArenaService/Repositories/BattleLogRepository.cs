namespace ArenaService.Repositories;

using ArenaService.Data;
using ArenaService.Models;

public interface IBattleLogRepository
{
    Task<BattleLog> AddBattleLogAsync(
        int participantId,
        int opponentId,
        int seasonId,
        string token
    );
}

public class BattleLogRepository : IBattleLogRepository
{
    private readonly ArenaDbContext _context;

    public BattleLogRepository(ArenaDbContext context)
    {
        _context = context;
    }

    public async Task<BattleLog> AddBattleLogAsync(
        int participantId,
        int opponentId,
        int seasonId,
        string token
    )
    {
        var battleLog = await _context.BattleLogs.AddAsync(
            new BattleLog
            {
                ParticipantId = participantId,
                OpponentId = opponentId,
                SeasonId = seasonId,
                Token = token
            }
        );
        _context.SaveChanges();
        return battleLog.Entity;
    }
}
