using ArenaService.Shared.Dtos;
using ArenaService.Shared.Repositories;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace ArenaService.Controllers;

[Route("cached-block-info")]
[ApiController]
public class CachedBlockInfoController : ControllerBase
{
    private readonly ISeasonCacheRepository _seasonCacheRepo;
    private readonly IBlockTrackerRepository _blockTrackerRepo;

    public CachedBlockInfoController(
        ISeasonCacheRepository seasonCacheRepo,
        IBlockTrackerRepository blockTrackerRepo
    )
    {
        _seasonCacheRepo = seasonCacheRepo;
        _blockTrackerRepo = blockTrackerRepo;
    }

    [HttpGet]
    [SwaggerResponse(StatusCodes.Status200OK, "CachedBlockInfoResponse", typeof(CachedBlockInfoResponse))]
    public async Task<ActionResult<CachedBlockInfoResponse>> GetCachedBlockInfo()
    {
        var currentBlockIndex = await _seasonCacheRepo.GetBlockIndexAsync();
        var season = await _seasonCacheRepo.GetSeasonAsync();
        var round = await _seasonCacheRepo.GetRoundAsync();
        var battleTxTrackerBlock = await _blockTrackerRepo.GetBattleTxTrackerBlockIndexAsync();

        return Ok(
            new CachedBlockInfoResponse(
                CurrentBlockIndex: currentBlockIndex,
                Season: new SeasonInfo(
                    Id: season.Id,
                    StartBlock: season.StartBlock,
                    EndBlock: season.EndBlock
                ),
                Round: new RoundInfo(
                    Id: round.Id,
                    StartBlock: round.StartBlock,
                    EndBlock: round.EndBlock
                ),
                BattleTxTrackerBlockIndex: battleTxTrackerBlock
            )
        );
    }
} 