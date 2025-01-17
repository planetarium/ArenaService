namespace ArenaService.Constants;

using System.Collections.Generic;

public static class OpponentGroupConstants
{
    public static readonly Dictionary<
        int,
        (double MinRange, double MaxRange, int WinScore, int LoseScore)
    > Groups =
        new()
        {
            { 1, (0.2, 0.4, 24, -1) }, // 상위1 그룹: 등수의 20~40%
            { 2, (0.4, 0.8, 22, -2) }, // 상위2 그룹: 등수의 40~80%
            { 3, (0.8, 1.2, 20, -3) }, // 동위 그룹: 등수의 80~120%
            { 4, (1.2, 1.8, 18, -4) }, // 하위1 그룹: 등수의 120~180%
            { 5, (1.8, 3.0, 16, -5) } // 하위2 그룹: 등수의 180~300%
        };
}
