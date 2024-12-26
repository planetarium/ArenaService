namespace ArenaService.Extensions;

using ArenaService.Dtos;
using ArenaService.Models;

public static class LeaderboardExtensions
{
    public static LeaderboardEntryResponse ToResponse(this LeaderboardEntry leaderboardEntry)
    {
        return new LeaderboardEntryResponse
        {
            AvatarAddress = leaderboardEntry.Participant.AvatarAddress,
            NameWithHash = leaderboardEntry.Participant.NameWithHash,
            PortraitId = leaderboardEntry.Participant.PortraitId,
            Rank = leaderboardEntry.Rank,
            TotalScore = leaderboardEntry.TotalScore
        };
    }

    public static List<LeaderboardEntryResponse> ToResponse(this List<LeaderboardEntry> leaderboard)
    {
        return leaderboard.Select(lbe => lbe.ToResponse()).ToList();
    }
}
