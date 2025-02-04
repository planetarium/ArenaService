namespace ArenaService.Controllers;

using ArenaService.Dtos;
using ArenaService.Extensions;
using ArenaService.Models;
using ArenaService.Repositories;
using ArenaService.Services;
using Libplanet.Crypto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
    [ProducesResponseType(typeof(string), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(string), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<string>> Register(
        [FromBody] UserRegisterRequest userRegisterRequest
    )
    {
        var avatarAddress = HttpContext.User.RequireAvatarAddress();
        var agentAddress = HttpContext.User.RequireAgentAddress();

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
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserResponse>> GetUser(Address avatarAddress)
    {
        return Ok();
    }

    [HttpGet("classify-by-championship/medals/{blockIndex}")]
    [Authorize(Roles = "User", AuthenticationSchemes = "ES256K")]
    [ProducesResponseType(typeof(ClassifyByBlockMedalsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
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
}
