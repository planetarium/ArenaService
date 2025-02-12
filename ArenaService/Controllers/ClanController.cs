namespace ArenaService.Controllers;

using ArenaService.Dtos;
using ArenaService.Extensions;
using ArenaService.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;

[Route("clans")]
[ApiController]
public class ClanController : ControllerBase
{
    private readonly IAllClanRankingRepository _clanRankingRepository;
    private readonly ISeasonCacheRepository _seasonCacheRepo;
    private readonly IClanRepository _clanRepo;
    private readonly IUserRepository _userRepo;

    public ClanController(
        IAllClanRankingRepository clanRankingRepository,
        ISeasonCacheRepository seasonCacheRepo,
        IClanRepository clanRepo,
        IUserRepository userRepo
    )
    {
        _clanRankingRepository = clanRankingRepository;
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
    public async Task<ActionResult<ClanLeaderboardResponse>> GetClanLeaderboard()
    {
        var avatarAddress = HttpContext.User.RequireAvatarAddress();

        var cachedSeason = await _seasonCacheRepo.GetSeasonAsync();
        var cachedRound = await _seasonCacheRepo.GetRoundAsync();

        var user = await _userRepo.GetUserAsync(avatarAddress, q => q.Include(u => u.Clan));

        ClanResponse? myClanResponse = null;
        if (user is not null & user!.Clan is not null)
        {
            var myClanRank = await _clanRankingRepository.GetRankAsync(
                user.ClanId!.Value,
                cachedSeason.Id,
                cachedRound.Id
            );
            var myClanScore = await _clanRankingRepository.GetScoreAsync(
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

        var clans = await _clanRankingRepository.GetTopClansAsync(
            cachedSeason.Id,
            cachedRound.Id,
            100
        );

        var clanResponses = new List<ClanResponse>();
        foreach (var clanRank in clans)
        {
            var clan = await _clanRepo.GetClan(clanRank.ClanId);

            clanResponses.Add(
                new ClanResponse
                {
                    ImageURL = clan!.ImageURL,
                    Name = clan.Name,
                    Rank = clanRank.Rank,
                    Score = clanRank.Score
                }
            );
        }

        var response = new ClanLeaderboardResponse
        {
            Leaderboard = clanResponses,
            MyClan = myClanResponse
        };

        return Ok(response);
    }
}
