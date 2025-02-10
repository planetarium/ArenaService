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
            { 1, (0m, 0.05m, 24, -1) }, // 상위1 그룹: 전체 0~5
            { 2, (0.05m, 0.2m, 22, -2) }, // 상위2 그룹: 전체 5~20
            { 3, (0.2m, 0.6m, 20, -3) }, // 동위 그룹: 전체 20~60
            { 4, (0.6m, 0.8m, 18, -4) }, // 하위1 그룹: 전체 60~80
            { 5, (0.8m, 1m, 16, -5) } // 하위2 그룹: 전체 80~100
        };
}
