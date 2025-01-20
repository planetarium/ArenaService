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

    public TicketController(
    )
    {
    }


    [HttpGet()]
    [Authorize(Roles = "User", AuthenticationSchemes = "ES256K")]
    [SwaggerOperation(Summary = "", Description = "")]
    [SwaggerResponse(
        StatusCodes.Status200OK,
        "TicketStatus",
        typeof(TicketStatusResponse)
    )]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "")]
    [SwaggerResponse(StatusCodes.Status503ServiceUnavailable, "")]
    public async Task<
        Results<
            NotFound<string>,
            StatusCodeHttpResult,
            Ok
        >
    > GetTicketStatus(
        [FromQuery] TicketType ticketType
    )
    {
        return TypedResults.Ok();
    }

    [HttpPost("purchase")]
    [Authorize(Roles = "User", AuthenticationSchemes = "ES256K")]
    [SwaggerOperation(Summary = "", Description = "")]
    [SwaggerResponse(
        StatusCodes.Status200OK,
        "Purchase Log Id",
        typeof(int)
    )]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "")]
    [SwaggerResponse(StatusCodes.Status503ServiceUnavailable, "")]
    public async Task<
        Results<
            NotFound<string>,
            StatusCodeHttpResult,
            Ok
        >
    > PurchaseTicket(
        [FromBody] PurchaseTicketRequest request
    )
    {
        return TypedResults.Ok();
    }

    [HttpGet("purchase-logs/{logId}")]
    [Authorize(Roles = "User", AuthenticationSchemes = "ES256K")]
    [SwaggerOperation(Summary = "", Description = "")]
    [SwaggerResponse(
        StatusCodes.Status200OK,
        "Purchase Log Id",
        typeof(TicketPurchaseLogResponse)
    )]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "")]
    [SwaggerResponse(StatusCodes.Status503ServiceUnavailable, "")]
    public async Task<
        Results<
            NotFound<string>,
            StatusCodeHttpResult,
            Ok
        >
    > GetPurchaseTicketLog(
        int logId
    )
    {
        return TypedResults.Ok();
    }
}
