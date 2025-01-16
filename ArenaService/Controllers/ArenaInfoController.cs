namespace ArenaService.Controllers;

using ArenaService.Dtos;
using ArenaService.Extensions;
using ArenaService.Repositories;
using Libplanet.Crypto;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

[Route("info")]
[ApiController]
public class ArenaInfoController : ControllerBase
{
    public ArenaInfoController() { }

    [HttpGet()]
    [ProducesResponseType(typeof(ArenaInfoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<Results<NotFound<string>, Ok>> GetArenaInfo()
    {
        return TypedResults.Ok();
    }
}
