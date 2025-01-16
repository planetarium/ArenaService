namespace ArenaService.Controllers;

using ArenaService.Dtos;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

[Route("info")]
[ApiController]
public class ArenaInfoController : ControllerBase
{
    public ArenaInfoController() { }

    [HttpGet]
    [SwaggerOperation(Summary = "", Description = "")]
    [SwaggerResponse(StatusCodes.Status200OK, "ArenaInfoResponse", typeof(ArenaInfoResponse))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Status401Unauthorized")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Status404NotFound")]
    public async Task<Results<NotFound<string>, Ok>> GetArenaInfo()
    {
        return TypedResults.Ok();
    }
}
