using System.ComponentModel.DataAnnotations;
using ArenaService.Shared.Constants;

namespace ArenaService.Shared.Dtos;

public class CreateSeasonRequest
{
    [Range(typeof(long), "0", "9223372036854775807")]
    public long StartBlock { get; set; }

    [Range(1, int.MaxValue)]
    public int RoundInterval { get; set; }

    [Range(1, int.MaxValue)]
    public int RoundCount { get; set; }

    [Range(1, int.MaxValue)]
    public int SeasonGroupId { get; set; }

    [EnumDataType(typeof(ArenaType))]
    public ArenaType ArenaType { get; set; }

    [Range(0, int.MaxValue)]
    public int RequiredMedalCount { get; set; }

    [Range(0, int.MaxValue)]
    public int TotalPrize { get; set; }

    [Range(1, int.MaxValue)]
    public int BattleTicketPolicyId { get; set; }

    [Range(1, int.MaxValue)]
    public int RefreshTicketPolicyId { get; set; }
}

public class UpdateSeasonRequest
{
    [Range(1, int.MaxValue)]
    public int SeasonGroupId { get; set; }

    [EnumDataType(typeof(ArenaType))]
    public ArenaType ArenaType { get; set; }

    [Range(1, int.MaxValue)]
    public int RoundInterval { get; set; }

    [Range(0, int.MaxValue)]
    public int RequiredMedalCount { get; set; }

    [Range(0, int.MaxValue)]
    public int TotalPrize { get; set; }

    [Range(1, int.MaxValue)]
    public int BattleTicketPolicyId { get; set; }

    [Range(1, int.MaxValue)]
    public int RefreshTicketPolicyId { get; set; }
}

public class AdjustEndBlockRequest
{
    [Range(typeof(long), "0", "9223372036854775807")]
    public long NewEndBlock { get; set; }
}

public class CreateBattleTicketPolicyRequest
{
    [Required]
    public required string Name { get; set; }

    [Range(0, int.MaxValue)]
    public int DefaultTicketsPerRound { get; set; }

    [Range(1, int.MaxValue)]
    public int MaxPurchasableTicketsPerRound { get; set; }

    [Range(1, int.MaxValue)]
    public int MaxPurchasableTicketsPerSeason { get; set; }

    [Required]
    [MinLength(1)]
    public List<decimal> PurchasePrices { get; set; } = new();
}

public class CreateRefreshTicketPolicyRequest
{
    [Required]
    public required string Name { get; set; }

    [Range(0, int.MaxValue)]
    public int DefaultTicketsPerRound { get; set; }

    [Range(1, int.MaxValue)]
    public int MaxPurchasableTicketsPerRound { get; set; }

    [Required]
    [MinLength(1)]
    public List<decimal> PurchasePrices { get; set; } = new();
}

public class InitializeSeasonRequest
{
    [Range(typeof(long), "0", "9223372036854775807")]
    public long BlockIndex { get; set; }
}

public class PrepareNextRoundRequest
{
    [Range(typeof(long), "0", "9223372036854775807")]
    public long BlockIndex { get; set; }
}

public class InitializeRankingCacheRequest
{
    [Range(1, int.MaxValue)]
    public int SeasonId { get; set; }

    [Range(1, int.MaxValue)]
    public int RoundId { get; set; }
}
