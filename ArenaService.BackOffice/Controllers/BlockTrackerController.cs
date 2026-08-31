using ArenaService.BackOffice.Authentication;
using ArenaService.BackOffice.Models;
using ArenaService.Shared.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArenaService.BackOffice.Controllers;

/// <summary>
/// Battle transaction tracker progress. Mirrors the BlockTrackerStatus component in the layout.
/// </summary>
[ApiController]
[Route("api/block-tracker")]
[Authorize(AuthenticationSchemes = ApiKeyAuthenticationDefaults.AuthenticationScheme)]
[Produces("application/json")]
public class BlockTrackerController : ControllerBase
{
    private readonly IBlockTrackerRepository _blockTrackerRepository;

    public BlockTrackerController(IBlockTrackerRepository blockTrackerRepository)
    {
        _blockTrackerRepository = blockTrackerRepository;
    }

    /// <summary>Gets the block index the battle transaction tracker has processed up to.</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<long>>> GetBattleTxTrackerBlockIndex()
    {
        var blockIndex = await _blockTrackerRepository.GetBattleTxTrackerBlockIndexAsync();
        return Ok(ApiResponse<long>.Ok(blockIndex));
    }
}
