using ArenaService.Shared.Models.Enums;
using Libplanet.Types.Tx;

namespace ArenaService.Shared.Dtos;

public class AdminBattleReviewResponse
{
    public int Id { get; set; }
    public string AvatarAddress { get; set; } = null!;
    public int SeasonId { get; set; }
    public int RoundId { get; set; }
    public BattleStatus BattleStatus { get; set; }
    public TxId? TxId { get; set; }
    public TxStatus? TxStatus { get; set; }
    public string? ExceptionNames { get; set; }
    public bool? Reviewed { get; set; }
}

public class AdminTicketPurchaseReviewResponse
{
    public int Id { get; set; }
    public string AvatarAddress { get; set; } = null!;
    public int SeasonId { get; set; }
    public int RoundId { get; set; }
    public PurchaseStatus PurchaseStatus { get; set; }
    public TxId? TxId { get; set; }
    public TxStatus? TxStatus { get; set; }
    public string? ExceptionNames { get; set; }
    public bool? Reviewed { get; set; }
}

public class AdminTicketPurchaseReviewListResponse
{
    public List<AdminTicketPurchaseReviewResponse> BattleTicketPurchases { get; set; } = new();
    public List<AdminTicketPurchaseReviewResponse> RefreshTicketPurchases { get; set; } = new();
}

public class AdminPreparationResponse
{
    public string Message { get; set; } = null!;
    public int? SeasonId { get; set; }
    public int? RoundId { get; set; }
}

public class AdminLeaderboardEntryResponse
{
    public string AvatarAddress { get; set; } = null!;
    public string AgentAddress { get; set; } = null!;
    public string NameWithHash { get; set; } = null!;
    public int Rank { get; set; }
    public int Score { get; set; }
    public int TotalWin { get; set; }
    public int TotalLose { get; set; }
    public int Level { get; set; }
}
