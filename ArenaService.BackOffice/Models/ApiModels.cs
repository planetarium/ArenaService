using System.ComponentModel.DataAnnotations;
using ArenaService.Shared.Constants;
using ArenaService.Shared.Models;
using ArenaService.Shared.Models.BattleTicket;
using ArenaService.Shared.Models.Enums;
using ArenaService.Shared.Models.RefreshTicket;
using ArenaService.Shared.Models.Ticket;

namespace ArenaService.BackOffice.Models;

// ── Responses ────────────────────────────────────────────────────────────────
// EF navigation properties are cyclic (Season <-> Round), so entities are never
// serialized directly. Every controller projects into the records below.

public record RoundDto(int Id, int SeasonId, int RoundIndex, long StartBlock, long EndBlock)
{
    public static RoundDto From(Round r) => new(r.Id, r.SeasonId, r.RoundIndex, r.StartBlock, r.EndBlock);
}

public record SeasonDto(
    int Id,
    int SeasonGroupId,
    long StartBlock,
    long EndBlock,
    ArenaType ArenaType,
    int RoundInterval,
    int RequiredMedalCount,
    int TotalPrize,
    string PrizeDetailUrl,
    int BattleTicketPolicyId,
    int RefreshTicketPolicyId,
    List<RoundDto>? Rounds,
    bool? Deletable
)
{
    public static SeasonDto From(Season s, bool? deletable = null, bool includeRounds = true) =>
        new(
            s.Id,
            s.SeasonGroupId,
            s.StartBlock,
            s.EndBlock,
            s.ArenaType,
            s.RoundInterval,
            s.RequiredMedalCount,
            s.TotalPrize,
            s.PrizeDetailUrl,
            s.BattleTicketPolicyId,
            s.RefreshTicketPolicyId,
            includeRounds && s.Rounds is not null
                ? s.Rounds.OrderBy(r => r.StartBlock).Select(RoundDto.From).ToList()
                : null,
            deletable
        );
}

public record PagedResult<T>(List<T> Items, int Page, int PageSize, int TotalCount, int TotalPages);

public record BattleTicketPolicyDto(
    int Id,
    string Name,
    int DefaultTicketsPerRound,
    int MaxPurchasableTicketsPerRound,
    int MaxPurchasableTicketsPerSeason,
    List<decimal> PurchasePrices
)
{
    public static BattleTicketPolicyDto From(BattleTicketPolicy p) =>
        new(
            p.Id,
            p.Name,
            p.DefaultTicketsPerRound,
            p.MaxPurchasableTicketsPerRound,
            p.MaxPurchasableTicketsPerSeason,
            p.PurchasePrices
        );
}

public record RefreshTicketPolicyDto(
    int Id,
    string Name,
    int DefaultTicketsPerRound,
    int MaxPurchasableTicketsPerRound,
    List<decimal> PurchasePrices
)
{
    public static RefreshTicketPolicyDto From(RefreshTicketPolicy p) =>
        new(p.Id, p.Name, p.DefaultTicketsPerRound, p.MaxPurchasableTicketsPerRound, p.PurchasePrices);
}

public record BattleDto(
    int Id,
    string AvatarAddress,
    int SeasonId,
    int RoundId,
    int AvailableOpponentId,
    BattleStatus BattleStatus,
    string? TxId,
    TxStatus? TxStatus,
    string? ExceptionNames,
    bool? Reviewed,
    bool? IsVictory,
    int? MyScoreChange,
    int? OpponentScoreChange,
    DateTime CreatedAt
)
{
    public static BattleDto From(Battle b) =>
        new(
            b.Id,
            b.AvatarAddress.ToString(),
            b.SeasonId,
            b.RoundId,
            b.AvailableOpponentId,
            b.BattleStatus,
            b.TxId?.ToString(),
            b.TxStatus,
            b.ExceptionNames,
            b.Reviewed,
            b.IsVictory,
            b.MyScoreChange,
            b.OpponentScoreChange,
            b.CreatedAt
        );
}

public record TicketPurchaseLogDto(
    int Id,
    string TicketType,
    string AvatarAddress,
    int SeasonId,
    int RoundId,
    decimal? AmountPaid,
    PurchaseStatus PurchaseStatus,
    int PurchaseCount,
    string TxId,
    TxStatus? TxStatus,
    string? ExceptionNames,
    bool? Reviewed,
    DateTime CreatedAt
)
{
    public static TicketPurchaseLogDto From(TicketPurchaseLog log) =>
        new(
            log.Id,
            log is BattleTicketPurchaseLog ? "battle" : "refresh",
            log.AvatarAddress.ToString(),
            log.SeasonId,
            log.RoundId,
            log.AmountPaid,
            log.PurchaseStatus,
            log.PurchaseCount,
            log.TxId.ToString(),
            log.TxStatus,
            log.ExceptionNames,
            log.Reviewed,
            log.CreatedAt
        );
}

public record LeaderboardEntryDto(
    int Rank,
    int Score,
    string AvatarAddress,
    string AgentAddress,
    int Level,
    long Cp,
    int PortraitId,
    int TotalWin,
    int TotalLose
);

public record CachedSeasonDto(int Id, long StartBlock, long EndBlock);

public record CachedRoundDto(int Id, int RoundIndex, long StartBlock, long EndBlock);

public record RankingCountDto(int RoundIndex, int RankingCount);

public record RankingCacheStatusDto(
    long BlockIndex,
    CachedSeasonDto? CurrentSeason,
    CachedRoundDto? CurrentRound,
    List<RankingCountDto> RankingCounts
);

// ── Requests ─────────────────────────────────────────────────────────────────

public class AddSeasonRequest
{
    [Required]
    public long StartBlock { get; set; }

    [Range(1, int.MaxValue)]
    public int RoundInterval { get; set; }

    [Range(1, int.MaxValue)]
    public int RoundCount { get; set; }

    public int SeasonGroupId { get; set; }

    public ArenaType ArenaType { get; set; } = ArenaType.SEASON;

    public int RequiredMedalCount { get; set; }

    public int TotalPrize { get; set; }

    [Range(1, int.MaxValue)]
    public int BattleTicketPolicyId { get; set; }

    [Range(1, int.MaxValue)]
    public int RefreshTicketPolicyId { get; set; }
}

public class UpdateSeasonRequest
{
    public int SeasonGroupId { get; set; }
    public ArenaType ArenaType { get; set; }

    [Range(1, int.MaxValue)]
    public int RoundInterval { get; set; }

    public int RequiredMedalCount { get; set; }
    public int TotalPrize { get; set; }

    [Range(1, int.MaxValue)]
    public int BattleTicketPolicyId { get; set; }

    [Range(1, int.MaxValue)]
    public int RefreshTicketPolicyId { get; set; }
}

public class AdjustEndBlockRequest
{
    [Required]
    public long NewEndBlock { get; set; }
}

public class AddBattleTicketPolicyRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;

    public int DefaultTicketsPerRound { get; set; }
    public int MaxPurchasableTicketsPerRound { get; set; }
    public int MaxPurchasableTicketsPerSeason { get; set; }

    /// <summary>Must contain exactly <see cref="MaxPurchasableTicketsPerSeason"/> entries.</summary>
    [Required]
    public List<decimal> PurchasePrices { get; set; } = new();
}

public class AddRefreshTicketPolicyRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;

    public int DefaultTicketsPerRound { get; set; }
    public int MaxPurchasableTicketsPerRound { get; set; }

    /// <summary>Must contain exactly <see cref="MaxPurchasableTicketsPerRound"/> entries.</summary>
    [Required]
    public List<decimal> PurchasePrices { get; set; } = new();
}
