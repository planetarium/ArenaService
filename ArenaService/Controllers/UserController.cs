namespace ArenaService.Controllers;

using ArenaService.Constants;
using ArenaService.Dtos;
using ArenaService.Extensions;
using ArenaService.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Route("users")]
[ApiController]
public class UserController : ControllerBase
{
    private readonly IUserRepository _userRepo;
    private readonly ISeasonRepository _seasonRepo;

    public UserController(IUserRepository userRepo, ISeasonRepository seasonRepo)
    {
        _seasonRepo = seasonRepo;
        _userRepo = userRepo;
    }

    [HttpPost]
    [Authorize(Roles = "User", AuthenticationSchemes = "ES256K")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(string), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] UserRegisterRequest userRegisterRequest)
    {
        var avatarAddress = HttpContext.User.RequireAvatarAddress();
        var agentAddress = HttpContext.User.RequireAgentAddress();

        await _userRepo.AddOrGetUserAsync(
            agentAddress,
            avatarAddress,
            userRegisterRequest.NameWithHash,
            userRegisterRequest.PortraitId,
            userRegisterRequest.Cp,
            userRegisterRequest.Level
        );

        return Created();
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
