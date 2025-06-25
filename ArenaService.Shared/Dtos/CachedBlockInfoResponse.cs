namespace ArenaService.Shared.Dtos;

public record CachedBlockInfoResponse(
    long CurrentBlockIndex,
    SeasonInfo Season,
    RoundInfo Round,
    long BattleTxTrackerBlockIndex
);

public record SeasonInfo(int Id, long StartBlock, long EndBlock);

public record RoundInfo(int Id, int RoundIndex, long StartBlock, long EndBlock); 