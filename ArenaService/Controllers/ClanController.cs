namespace ArenaService.Controllers;

using ArenaService.Dtos;
using ArenaService.Extensions;
using ArenaService.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Route("clans")]
[ApiController]
public class ClanController : ControllerBase
{
    private readonly IClanRankingRepository _clanRankingRepository;
    private readonly ISeasonCacheRepository _seasonCacheRepo;
    private readonly IClanRepository _clanRepo;
    private readonly IUserRepository _userRepo;

    public ClanController(
        IClanRankingRepository clanRankingRepository,
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
    [ProducesResponseType(typeof(ClanLeaderboardResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClanLeaderboardResponse>> GetClanLeaderboard()
    {
        var avatarAddress = HttpContext.User.RequireAvatarAddress();

        var cachedSeason = await _seasonCacheRepo.GetSeasonAsync();
        var cachedRound = await _seasonCacheRepo.GetRoundAsync();

        var user = await _userRepo.GetUserAsync(avatarAddress, q => q.Include(u => u.Clan));

        if (user is null)
        {
            return NotFound($"Not found {avatarAddress}");
        }

        if (user.Clan is null)
        {
            return NotFound($"Not found clan");
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

        var response = new ClanLeaderboardResponse
        {
            Leaderboard = clanResponses,
            MyClan = new ClanResponse
            {
                ImageURL = user!.Clan!.ImageURL,
                Name = user!.Clan!.Name,
                Rank = myClanRank,
                Score = myClanScore,
            }
        };

        return Ok(response);
    }
}
