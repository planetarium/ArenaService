using ArenaService.Constants;
using ArenaService.Models;
using Libplanet.Crypto;

namespace ArenaService.Dtos;

public class AvailableOpponentResponse
{
    public Address AvatarAddress { get; set; }

    public required string NameWithHash { get; set; }

    public int PortraitId { get; set; }
    public long Cp { get; set; }
    public int Level { get; set; }
    public int SeasonId { get; set; }
    public int Score { get; set; }
    public int Rank { get; set; }
    public bool IsAttacked { get; set; }
    public int ScoreGainOnWin { get; set; }
    public int ScoreLossOnLose { get; set; }
    public bool? IsVictory { get; set; } = null;
    public required string ClanImageURL { get; set; }

    public static AvailableOpponentResponse FromAvailableOpponent(
        AvailableOpponent availableOpponent,
        int opponentRank
    )
    {
        return new AvailableOpponentResponse
        {
            AvatarAddress = availableOpponent.Opponent.AvatarAddress,
            NameWithHash = availableOpponent.Opponent.User.NameWithHash,
            PortraitId = availableOpponent.Opponent.User.PortraitId,
            Cp = availableOpponent.Opponent.User.Cp,
            Level = availableOpponent.Opponent.User.Level,
            SeasonId = availableOpponent.Opponent.SeasonId,
            Score = availableOpponent.Opponent.Score,
            Rank = opponentRank,
            IsAttacked = availableOpponent.SuccessBattleId is not null,
            ScoreGainOnWin = OpponentGroupConstants.Groups[availableOpponent.GroupId].WinScore,
            ScoreLossOnLose = OpponentGroupConstants.Groups[availableOpponent.GroupId].LoseScore,
            IsVictory = availableOpponent.SuccessBattle?.IsVictory,
            ClanImageURL = ""
        };
    }
}
