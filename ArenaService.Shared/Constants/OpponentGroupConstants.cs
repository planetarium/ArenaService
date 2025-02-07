namespace ArenaService.Shared.Constants;

using System.Collections.Generic;

public static class OpponentGroupConstants
{
    public static readonly Dictionary<
        int,
        (decimal MinRange, decimal MaxRange, int WinScore, int LoseScore)
    > Groups =
        new()
        {
            { 1, (0.2m, 0.4m, 24, -1) }, // 상위1 그룹: 등수의 20~40%
            { 2, (0.4m, 0.8m, 22, -2) }, // 상위2 그룹: 등수의 40~80%
            { 3, (0.8m, 1.2m, 20, -3) }, // 동위 그룹: 등수의 80~120%
            { 4, (1.2m, 1.8m, 18, -4) }, // 하위1 그룹: 등수의 120~180%
            { 5, (1.8m, 3.0m, 16, -5) } // 하위2 그룹: 등수의 180~300%
        };
}
