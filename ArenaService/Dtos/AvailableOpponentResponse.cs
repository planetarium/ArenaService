namespace ArenaService.Dtos;

public class AvailableOpponentResponse
{
    public required string AvatarAddress { get; set; }

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
}
