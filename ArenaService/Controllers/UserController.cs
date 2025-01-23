namespace ArenaService.Controllers;

using ArenaService.Dtos;
using ArenaService.Extensions;
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
    private readonly IParticipateService _participateService;
    private readonly ISeasonCacheRepository _seasonCacheRepo;

    public UserController(
        IUserRepository userRepo,
        ISeasonRepository seasonRepo,
        IParticipateService participateService,
        ISeasonCacheRepository seasonCacheRepo
    )
    {
        _seasonRepo = seasonRepo;
        _userRepo = userRepo;
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
                avatarAddress
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
        // var avatarAddress = HttpContext.User.RequireAvatarAddress();

        // var seasons = await _seasonRepo.GetAllSeasonsAsync(q =>
        //     q.Include(s => s.Rounds)
        //         .Include(s => s.BattleTicketPolicy)
        //         .Include(s => s.RefreshTicketPolicy)
        // );

        // var currentSeason = seasons.FirstOrDefault(s =>
        //     s.StartBlock <= blockIndex && s.EndBlock >= blockIndex
        // );

        // if (currentSeason == null)
        // {
        //     return Ok(
        //         new ClassifyByBlockMedalsResponse
        //         {
        //             Medals = new(),
        //             TotalMedalCountForThisChampionship = 0
        //         }
        //     );
        // }

        // var filteredSeasons = seasons
        //     .OrderBy(s => s.StartBlock)
        //     .SkipWhile(s => s.StartBlock < currentSeason.StartBlock)
        //     .ToList();

        // var championshipIndex = filteredSeasons.FindIndex(s =>
        //     s.ArenaType == ArenaType.CHAMPIONSHIP
        // );
        // if (championshipIndex != -1)
        // {
        //     filteredSeasons = filteredSeasons.Take(championshipIndex + 1).ToList();
        // }

        // var medals = filteredSeasons.Select(s=> s.Id)

        return Ok(
            new ClassifyByBlockMedalsResponse
            {
                Medals = new(),
                TotalMedalCountForThisChampionship = 1000
            }
        );
    }
}
