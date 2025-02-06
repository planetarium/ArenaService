namespace ArenaService.Controllers;

using System.Globalization;
using ArenaService.Dtos;
using ArenaService.Extensions;
using ArenaService.Models;
using ArenaService.Repositories;
using ArenaService.Services;
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
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Status400BadRequest")]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Status401Unauthorized")]
    [SwaggerResponse(StatusCodes.Status409Conflict, "Status409Conflict")]
    public async Task<ActionResult<string>> Register(
        [FromBody] UserRegisterRequest userRegisterRequest
    )
    {
        var avatarAddress = HttpContext.User.RequireAvatarAddress();
        var agentAddress = HttpContext.User.RequireAgentAddress();

        if (!CheckSignerContainsAvatar(agentAddress, avatarAddress))
        {
            return BadRequest("invalid address.");
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

            var cachedSeason = await _seasonCacheRepo.GetSeasonAsync();
            var cachedRound = await _seasonCacheRepo.GetRoundAsync();

            var participant = await _participateService.ParticipateAsync(
                cachedSeason.Id,
                cachedRound.Id,
                avatarAddress,
                (int)(cachedRound.EndBlock - cachedRound.StartBlock)
            );

            return Created(locationUri, user.AvatarAddress);
        }

        return Conflict("Already registered");
    }

    [HttpGet("{avatarAddress}")]
    [Authorize(Roles = "User", AuthenticationSchemes = "ES256K")]
    [SwaggerResponse(StatusCodes.Status200OK, "UserResponse", typeof(UserResponse))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Status401Unauthorized")]
    public async Task<ActionResult<UserResponse>> GetUser(Address avatarAddress)
    {
        return Ok();
    }

    [HttpGet("classify-by-championship/medals/{blockIndex}")]
    [Authorize(Roles = "User", AuthenticationSchemes = "ES256K")]
    [SwaggerResponse(
        StatusCodes.Status200OK,
        "ClassifyByBlockMedalsResponse",
        typeof(ClassifyByBlockMedalsResponse)
    )]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Status401Unauthorized")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Status404NotFound")]
    public async Task<IActionResult> GetMedals(long blockIndex)
    {
        var avatarAddress = HttpContext.User.RequireAvatarAddress();

        var classifiedSeasons = await _seasonService.ClassifyByChampionship(blockIndex);

        var medals = new List<MedalResponse>();
        var totalMedalCount = 0;
        foreach (var season in classifiedSeasons)
        {
            if (season.ArenaType == Constants.ArenaType.SEASON)
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

    private static bool CheckSignerContainsAvatar(Address signer, Address avatarAddress)
    {
        const string deriveFormat = "avatar-state-{0}";
        const int slotCount = 3;

        return Enumerable.Range(0, 3)
            .Select(index => GetAvatarAddress(signer, index))
            .Contains(avatarAddress);

        Address GetAvatarAddress(Address agentAddress, int index)
        {
            if (index < 0 ||
                index >= slotCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(index),
                    $"Index must be between 0 and 2.");
            }

            var deriveKey = string.Format(
                CultureInfo.InvariantCulture,
                deriveFormat,
                index);
            return agentAddress.Derive(deriveKey);
        }
    }
}
