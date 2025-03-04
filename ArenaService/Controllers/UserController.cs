namespace ArenaService.Controllers;

using System.Globalization;
using ArenaService.Shared.Dtos;
using ArenaService.Shared.Extensions;
using ArenaService.Shared.Services;
using ArenaService.Shared.Constants;
using ArenaService.Shared.Exceptions;
using ArenaService.Shared.Repositories;
using ArenaService.Utils;
using Libplanet.Crypto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

[Route("users")]
[ApiController]
public class UserController : ControllerBase
{
    private readonly IUserRepository _userRepo;
    private readonly ISeasonRepository _seasonRepo;
    private readonly IMedalRepository _medalRepo;
    private readonly ISeasonService _seasonService;
    private readonly IParticipateService _participateService;
    private readonly ISeasonCacheRepository _seasonCacheRepo;

    public UserController(
        IUserRepository userRepo,
        ISeasonService seasonService,
        ISeasonRepository seasonRepo,
        IMedalRepository medalRepo,
        IParticipateService participateService,
        ISeasonCacheRepository seasonCacheRepo
    )
    {
        _seasonRepo = seasonRepo;
        _seasonService = seasonService;
        _userRepo = userRepo;
        _medalRepo = medalRepo;
        _participateService = participateService;
        _seasonCacheRepo = seasonCacheRepo;
    }

    [HttpPost]
    [Authorize(Roles = "User", AuthenticationSchemes = "ES256K")]
    [SwaggerResponse(StatusCodes.Status201Created, "Ok")]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Status400BadRequest", typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Status401Unauthorized", typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status409Conflict, "Status409Conflict", typeof(ErrorResponse))]
    public async Task<ActionResult<string>> Register(
        [FromBody] UserRegisterRequest userRegisterRequest
    )
    {
        var avatarAddress = HttpContext.User.RequireAvatarAddress();
        var agentAddress = HttpContext.User.RequireAgentAddress();

        if (!AvatarAddressValidator.CheckSignerContainsAvatar(agentAddress, avatarAddress))
        {
            return BadRequest(new ErrorResponse("INVALID_ADDRESS", "Invalid address provided"));
        }

        var user = await _userRepo.GetUserAsync(avatarAddress);
        if (user == null)
        {
            user = await _userRepo.AddUserAsync(
                agentAddress,
                avatarAddress,
                userRegisterRequest.NameWithHash,
                userRegisterRequest.PortraitId,
                userRegisterRequest.Cp,
                userRegisterRequest.Level
            );

            var locationUri = Url.Action(
                nameof(GetUser),
                new { avatarAddress = user.AvatarAddress }
            );

            return Created(locationUri, user.AvatarAddress);
        }

        return Conflict(new ErrorResponse("USER_EXISTS", "User is already registered"));
    }

    [HttpGet("{avatarAddress}")]
    [Authorize(Roles = "User", AuthenticationSchemes = "ES256K")]
    [SwaggerResponse(StatusCodes.Status200OK, "UserResponse", typeof(UserResponse))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Status401Unauthorized", typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Status404NotFound", typeof(ErrorResponse))]
    public async Task<ActionResult<UserResponse>> GetUser(Address avatarAddress)
    {
        var user = await _userRepo.GetUserAsync(avatarAddress);
        if (user == null)
        {
            return NotFound(new ErrorResponse("USER_NOT_FOUND", "User not found"));
        }
        return Ok(new UserResponse
        {
            AvatarAddress = user.AvatarAddress,
            NameWithHash = user.NameWithHash,
            PortraitId = user.PortraitId,
            Cp = user.Cp,
            Level = user.Level
        });
    }

    [HttpGet("classify-by-championship/medals/{blockIndex}")]
    [Authorize(Roles = "User", AuthenticationSchemes = "ES256K")]
    [SwaggerResponse(
        StatusCodes.Status200OK,
        "ClassifyByBlockMedalsResponse",
        typeof(ClassifyByBlockMedalsResponse)
    )]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Status401Unauthorized", typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Status404NotFound", typeof(ErrorResponse))]
    public async Task<IActionResult> GetMedals(long blockIndex)
    {
        var avatarAddress = HttpContext.User.RequireAvatarAddress();

        var classifiedSeasons = await _seasonService.ClassifyByChampionship(blockIndex);

        if (!classifiedSeasons.Any())
        {
            return NotFound(new ErrorResponse("NO_SEASONS_FOUND", "No seasons found for the given block index"));
        }

        var medals = new List<MedalResponse>();
        var totalMedalCount = 0;
        foreach (var season in classifiedSeasons)
        {
            if (season.ArenaType == ArenaType.SEASON)
            {
                var medal = await _medalRepo.GetMedalAsync(season.Id, avatarAddress);

                medals.Add(
                    new MedalResponse
                    {
                        SeasonId = season.Id,
                        MedalCount = medal is null ? 0 : medal.MedalCount
                    }
                );

                if (medal is not null)
                {
                    totalMedalCount += medal.MedalCount;
                }
            }
        }

        return Ok(
            new ClassifyByBlockMedalsResponse
            {
                Medals = medals,
                TotalMedalCountForThisChampionship = totalMedalCount
            }
        );
    }
}
