using System.ComponentModel.DataAnnotations;
using ArenaService.Shared.Constants;

namespace ArenaService.Shared.Dtos;

public class CreateSeasonRequest
{
    [Required]
    public long StartBlock { get; set; }

    [Required]
    [Range(1, int.MaxValue)]
    public int RoundInterval { get; set; }

    [Required]
    [Range(1, int.MaxValue)]
    public int RoundCount { get; set; }

    [Required]
    public int SeasonGroupId { get; set; }

    [Required]
    public ArenaType ArenaType { get; set; }

    [Range(0, int.MaxValue)]
    public int RequiredMedalCount { get; set; }

    [Range(0, int.MaxValue)]
    public int TotalPrize { get; set; }

    [Required]
    [Range(1, int.MaxValue)]
    public int BattleTicketPolicyId { get; set; }

    [Required]
    [Range(1, int.MaxValue)]
    public int RefreshTicketPolicyId { get; set; }
}

public class UpdateSeasonRequest
{
    [Required]
    public int SeasonGroupId { get; set; }

    [Required]
    public ArenaType ArenaType { get; set; }

    [Required]
    [Range(1, int.MaxValue)]
    public int RoundInterval { get; set; }

    [Range(0, int.MaxValue)]
    public int RequiredMedalCount { get; set; }

    [Range(0, int.MaxValue)]
    public int TotalPrize { get; set; }

    [Required]
    [Range(1, int.MaxValue)]
    public int BattleTicketPolicyId { get; set; }

    [Required]
    [Range(1, int.MaxValue)]
    public int RefreshTicketPolicyId { get; set; }
}

public class AdjustEndBlockRequest
{
    [Required]
    public long NewEndBlock { get; set; }
}

public class CreateBattleTicketPolicyRequest
{
    [Required]
    public required string Name { get; set; }

    [Range(0, int.MaxValue)]
    public int DefaultTicketsPerRound { get; set; }

    [Range(0, int.MaxValue)]
    public int MaxPurchasableTicketsPerRound { get; set; }

    [Range(0, int.MaxValue)]
    public int MaxPurchasableTicketsPerSeason { get; set; }

    [Required]
    public List<decimal> PurchasePrices { get; set; } = new();
}

public class CreateRefreshTicketPolicyRequest
{
    [Required]
    public required string Name { get; set; }

    [Range(0, int.MaxValue)]
    public int DefaultTicketsPerRound { get; set; }

    [Range(0, int.MaxValue)]
    public int MaxPurchasableTicketsPerRound { get; set; }

    [Required]
    public List<decimal> PurchasePrices { get; set; } = new();
}

public class InitializeSeasonRequest
{
    [Required]
    public long BlockIndex { get; set; }
}

public class PrepareNextRoundRequest
{
    [Required]
    public long BlockIndex { get; set; }
}

public class InitializeRankingCacheRequest
{
    [Required]
    [Range(1, int.MaxValue)]
    public int SeasonId { get; set; }

    [Required]
    [Range(1, int.MaxValue)]
    public int RoundId { get; set; }
}
