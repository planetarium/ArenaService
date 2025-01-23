using ArenaService.Constants;
using ArenaService.Models;
using Libplanet.Crypto;
using Newtonsoft.Json;

namespace ArenaService.Dtos;

public class AvailableOpponentResponse
{
    public required Address AvatarAddress { get; set; }

    [JsonProperty(Required = Required.DisallowNull)]
    public required string NameWithHash { get; set; }

    public required int PortraitId { get; set; }
    public required long Cp { get; set; }
    public required int Level { get; set; }
    public required int SeasonId { get; set; }
    public required int Score { get; set; }
    public required int Rank { get; set; }
    public required int GroupId { get; set; }
    public required bool IsAttacked { get; set; }
    public required int ScoreGainOnWin { get; set; }
    public required int ScoreLossOnLose { get; set; }
    public bool? IsVictory { get; set; } = null;
    public ClanResponse? ClanInfo { get; set; } = null;

    public static AvailableOpponentResponse FromAvailableOpponent(
        AvailableOpponent availableOpponent,
        int opponentRank,
        int opponentScore
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
            GroupId = availableOpponent.GroupId,
            Score = opponentScore,
            Rank = opponentRank,
            IsAttacked = availableOpponent.SuccessBattleId is not null,
            ScoreGainOnWin = OpponentGroupConstants.Groups[availableOpponent.GroupId].WinScore,
            ScoreLossOnLose = OpponentGroupConstants.Groups[availableOpponent.GroupId].LoseScore,
            IsVictory = availableOpponent.SuccessBattle?.IsVictory,
            ClanInfo = null
        };
    }
}
