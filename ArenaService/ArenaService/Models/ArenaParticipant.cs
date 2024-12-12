using Libplanet.Crypto;

namespace ArenaService.Models;

public class ArenaParticipant
{
    public Address AvatarAddr { get; set; }
    public int Score { get; set; }
    public int Rank { get; set; }
    public int WinScore { get; set; }
    public int LoseScore { get; set; }
    public int Cp { get; set; }
    public int PortraitId { get; set; }
    public string NameWithHash { get; set; } = "";
    public int Level { get; set; }

    public ArenaParticipant()
    {
    }

    public ArenaParticipant(
        Address avatarAddr,
        int score,
        int rank,
        string avatarNameWithHash,
        int avatarLevel,
        int portraitId,
        int winScore,
        int loseScore,
        int cp)
    {
        AvatarAddr = avatarAddr;
        Score = score;
        Rank = rank;
        WinScore = winScore;
        LoseScore = loseScore;
        Cp = cp;
        PortraitId = portraitId;
        NameWithHash = avatarNameWithHash;
        Level = avatarLevel;
    }

    public void Update(int winScore, int loseScore)
    {
        WinScore = winScore;
        LoseScore = loseScore;
    }
}
