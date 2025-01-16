namespace ArenaService.Constants;

public enum UpdateSource
{
    FREE,
    CRYSTAL,
    NCG
}

public static class OpponentRefreshCosts
{
    public class RefreshCostPolicy
    {
        public UpdateSource Source { get; init; }
        public decimal Cost { get; init; }
    }

    public static readonly List<RefreshCostPolicy> RefreshPolicies =
        new()
        {
            new RefreshCostPolicy { Source = UpdateSource.FREE, Cost = 0 },
            new RefreshCostPolicy { Source = UpdateSource.CRYSTAL, Cost = 10000 },
            new RefreshCostPolicy { Source = UpdateSource.NCG, Cost = 1.0m }
        };

    public const int FreeRefreshLimitPerInterval = 1;

    public static RefreshCostPolicy GetPolicy(int refreshCount)
    {
        return refreshCount switch
        {
            <= FreeRefreshLimitPerInterval => RefreshPolicies[0],
            2 => RefreshPolicies[1],
            _ => RefreshPolicies[2]
        };
    }
}
