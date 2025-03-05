namespace ArenaService.Controllers;

using ArenaService.Shared.Dtos;
using ArenaService.Shared.Extensions;
using ArenaService.Shared.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;

[Route("clans")]
[ApiController]
public class ClanController : ControllerBase
{
    private readonly IAllClanRankingRepository _allClanRankingRepo;
    private readonly ISeasonCacheRepository _seasonCacheRepo;
    private readonly IClanRepository _clanRepo;
    private readonly IUserRepository _userRepo;

    public ClanController(
        IAllClanRankingRepository allClanRankingRepo,
        ISeasonCacheRepository seasonCacheRepo,
        IClanRepository clanRepo,
        IUserRepository userRepo
    )
    {
        _allClanRankingRepo = allClanRankingRepo;
        _seasonCacheRepo = seasonCacheRepo;
        _userRepo = userRepo;
        _clanRepo = clanRepo;
    }

    [HttpGet("leaderboard")]
    [Authorize(Roles = "User", AuthenticationSchemes = "ES256K")]
    [SwaggerResponse(
        StatusCodes.Status200OK,
        "ClanLeaderboardResponse",
        typeof(ClanLeaderboardResponse)
    )]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Status401Unauthorized", typeof(string))]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Status404NotFound", typeof(string))]
    public async Task<ActionResult<ClanLeaderboardResponse>> GetClanLeaderboard()
    {
        var avatarAddress = HttpContext.User.RequireAvatarAddress();

        var cachedSeason = await _seasonCacheRepo.GetSeasonAsync();
        var cachedRound = await _seasonCacheRepo.GetRoundAsync();

        var user = await _userRepo.GetUserAsync(avatarAddress, q => q.Include(u => u.Clan));

        ClanResponse? myClanResponse = null;
        if (user is not null & user!.Clan is not null)
        {
            var myClanRank = await _allClanRankingRepo.GetRankAsync(
                user.ClanId!.Value,
                cachedSeason.Id,
                cachedRound.Id
            );
            var myClanScore = await _allClanRankingRepo.GetScoreAsync(
                user.ClanId!.Value,
                cachedSeason.Id,
                cachedRound.Id
            );
            myClanResponse = new ClanResponse
            {
                ImageURL = user!.Clan!.ImageURL,
                Name = user!.Clan!.Name,
                Rank = myClanRank,
                Score = myClanScore,
            };
        }

        var clans = await _allClanRankingRepo.GetTopClansAsync(
            cachedSeason.Id,
            cachedRound.Id,
            100
        );

        if (!clans.Any())
        {
            return NotFound("NO_CLANS_FOUND");
        }

        var clanResponses = new List<ClanResponse>();
        foreach (var clanRank in clans)
        {
            var clan = await _clanRepo.GetClan(clanRank.ClanId);
            if (clan == null)
            {
                continue;
            }

            clanResponses.Add(
                new ClanResponse
                {
                    ImageURL = clan.ImageURL,
                    Name = clan.Name,
                    Rank = clanRank.Rank,
                    Score = clanRank.Score
                }
            );
        }

        if (!clanResponses.Any())
        {
            return NotFound("NO_VALID_CLANS");
        }

        var response = new ClanLeaderboardResponse
        {
            Leaderboard = clanResponses,
            MyClan = myClanResponse
        };

        return Ok(response);
    }
}
