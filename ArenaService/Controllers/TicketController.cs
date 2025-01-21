namespace ArenaService.Controllers;

using ArenaService.Dtos;
using ArenaService.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;

[Route("tickets")]
[ApiController]
public class TicketController : ControllerBase
{
    public TicketController() { }

    [HttpGet("battle")]
    [Authorize(Roles = "User", AuthenticationSchemes = "ES256K")]
    [SwaggerOperation(Summary = "", Description = "")]
    [SwaggerResponse(StatusCodes.Status200OK, "TicketStatus", typeof(TicketStatusResponse))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "")]
    [SwaggerResponse(StatusCodes.Status503ServiceUnavailable, "")]
    public async Task<Results<NotFound<string>, StatusCodeHttpResult, Ok>> GetBattleTicketStatus()
    {
        return TypedResults.Ok();
    }

    [HttpGet("refresh")]
    [Authorize(Roles = "User", AuthenticationSchemes = "ES256K")]
    [SwaggerOperation(Summary = "", Description = "")]
    [SwaggerResponse(StatusCodes.Status200OK, "TicketStatus", typeof(TicketStatusResponse))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "")]
    [SwaggerResponse(StatusCodes.Status503ServiceUnavailable, "")]
    public async Task<Results<NotFound<string>, StatusCodeHttpResult, Ok>> GetRefreshTicketStatus()
    {
        return TypedResults.Ok();
    }

    [HttpPost("battle/purchase")]
    [Authorize(Roles = "User", AuthenticationSchemes = "ES256K")]
    [SwaggerOperation(Summary = "", Description = "")]
    [SwaggerResponse(StatusCodes.Status200OK, "Purchase Log Id", typeof(int))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "")]
    [SwaggerResponse(StatusCodes.Status503ServiceUnavailable, "")]
    public async Task<Results<NotFound<string>, StatusCodeHttpResult, Ok>> PurchaseBattleTicket(
        [FromBody] PurchaseTicketRequest request
    )
    {
        return TypedResults.Ok();
    }

    [HttpPost("refresh/purchase")]
    [Authorize(Roles = "User", AuthenticationSchemes = "ES256K")]
    [SwaggerOperation(Summary = "", Description = "")]
    [SwaggerResponse(StatusCodes.Status200OK, "Purchase Log Id", typeof(int))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "")]
    [SwaggerResponse(StatusCodes.Status503ServiceUnavailable, "")]
    public async Task<Results<NotFound<string>, StatusCodeHttpResult, Ok>> PurchaseRefreshTicket(
        [FromBody] PurchaseTicketRequest request
    )
    {
        return TypedResults.Ok();
    }

    [HttpGet("battle/purchase-logs/{logId}")]
    [Authorize(Roles = "User", AuthenticationSchemes = "ES256K")]
    [SwaggerOperation(Summary = "", Description = "")]
    [SwaggerResponse(StatusCodes.Status200OK, "Purchase Log Id", typeof(TicketPurchaseLogResponse))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "")]
    [SwaggerResponse(StatusCodes.Status503ServiceUnavailable, "")]
    public async Task<
        Results<NotFound<string>, StatusCodeHttpResult, Ok>
    > GetPurchaseBattleTicketLog(int logId)
    {
        return TypedResults.Ok();
    }

    [HttpGet("refresh/purchase-logs/{logId}")]
    [Authorize(Roles = "User", AuthenticationSchemes = "ES256K")]
    [SwaggerOperation(Summary = "", Description = "")]
    [SwaggerResponse(StatusCodes.Status200OK, "Purchase Log Id", typeof(TicketPurchaseLogResponse))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "")]
    [SwaggerResponse(StatusCodes.Status503ServiceUnavailable, "")]
    public async Task<
        Results<NotFound<string>, StatusCodeHttpResult, Ok>
    > GetPurchaseRefreshTicketLog(int logId)
    {
        return TypedResults.Ok();
    }
}
