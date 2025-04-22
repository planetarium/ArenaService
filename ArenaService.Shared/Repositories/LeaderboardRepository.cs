using System.Text;
using ArenaService.Shared.Data;
using ArenaService.Shared.Models;
using Libplanet.Crypto;
using Microsoft.EntityFrameworkCore;

namespace ArenaService.Shared.Repositories;

public interface ILeaderboardRepository
{
    Task<List<(Participant Participant, int Score, int Rank)>> GetLeaderboardAsync(int seasonId);
    Task<byte[]> GenerateLeaderboardCsvAsync(int seasonId);
}

public class LeaderboardRepository : ILeaderboardRepository
{
    private readonly ArenaDbContext _context;

    public LeaderboardRepository(ArenaDbContext context)
    {
        _context = context;
    }

    public async Task<List<(Participant Participant, int Score, int Rank)>> GetLeaderboardAsync(
        int seasonId
    )
    {
        // 시즌의 참가자를 Score 기준으로 내림차순 정렬
        var orderedParticipants = await _context
            .Participants.Where(p => p.SeasonId == seasonId)
            .Include(p => p.User)
            .OrderByDescending(p => p.Score)
            .ToListAsync();
        var result = new List<(Participant Participant, int Score, int Rank)>();
        
        // 같은 agent address를 가진 참가자 중 가장 높은 점수만 남김
        orderedParticipants = orderedParticipants
            .GroupBy(p => p.User.AgentAddress.ToHex().ToLower())
            .Select(g => g.OrderByDescending(p => p.Score).First())
            .OrderByDescending(p => p.Score)
            .ToList();

        // 동점자 처리를 위한 로직
        int processedCount = 0;

        foreach (var group in orderedParticipants.GroupBy(p => p.Score))
        {
            int lastRank = processedCount + group.Count();

            foreach (var participant in group)
            {
                result.Add((participant, participant.Score, lastRank));
            }

            processedCount += group.Count();
        }

        return result;
    }

    public async Task<byte[]> GenerateLeaderboardCsvAsync(int seasonId)
    {
        var leaderboardData = await GetLeaderboardAsync(seasonId);
        var season = await _context.Seasons.FindAsync(seasonId);

        StringBuilder csv = new StringBuilder();

        // CSV 헤더 추가
        csv.AppendLine(
            "avatar_address,agent_address,name_with_hash,ranking,score,total_win,total_lose,level"
        );

        // CSV 데이터 행 추가
        foreach (var item in leaderboardData)
        {
            csv.AppendLine(
                $"{item.Participant.User.AvatarAddress.ToString().ToLower()},"
                    + $"{item.Participant.User.AgentAddress.ToString().ToLower()},"
                    + $"{item.Participant.User.NameWithHash},"
                    + $"{item.Rank},"
                    + $"{item.Score},"
                    + $"{item.Participant.TotalWin},"
                    + $"{item.Participant.TotalLose},"
                    + $"{item.Participant.User.Level}"
            );
        }

        return Encoding.UTF8.GetBytes(csv.ToString());
    }
}
