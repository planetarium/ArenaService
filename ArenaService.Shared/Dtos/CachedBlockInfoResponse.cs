namespace ArenaService.Shared.Dtos;

public record CachedBlockInfoResponse(
    long CurrentBlockIndex,
    SeasonInfo Season,
    RoundInfo Round,
    long BattleTxTrackerBlockIndex
);

public record SeasonInfo(int Id, long StartBlock, long EndBlock);

public record RoundInfo(int Id, long StartBlock, long EndBlock); 