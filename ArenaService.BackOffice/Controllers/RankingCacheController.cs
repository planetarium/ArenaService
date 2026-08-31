using ArenaService.BackOffice.Authentication;
using ArenaService.BackOffice.Models;
using ArenaService.Shared.Repositories;
using ArenaService.Shared.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ArenaService.BackOffice.Controllers;

/// <summary>
/// Season / round cache preparation. Mirrors the RankingCache and CacheInitialization Blazor pages.
/// </summary>
[ApiController]
[Route("api/ranking-cache")]
[Authorize(AuthenticationSchemes = ApiKeyAuthenticationDefaults.AuthenticationScheme)]
[Produces("application/json")]
public class RankingCacheController : ControllerBase
{
    private readonly ISeasonRepository _seasonRepository;
    private readonly ISeasonCacheRepository _seasonCacheRepository;
    private readonly IRankingRepository _rankingRepository;
    private readonly IRoundRepository _roundRepository;
    private readonly ISeasonPreparationService _seasonPreparationService;
    private readonly IRoundPreparationService _roundPreparationService;
    private readonly ICacheInitializationService _cacheInitializationService;
    private readonly ILogger<RankingCacheController> _logger;

    public RankingCacheController(
        ISeasonRepository seasonRepository,
        ISeasonCacheRepository seasonCacheRepository,
        IRankingRepository rankingRepository,
        IRoundRepository roundRepository,
        ISeasonPreparationService seasonPreparationService,
        IRoundPreparationService roundPreparationService,
        ICacheInitializationService cacheInitializationService,
        ILogger<RankingCacheController> logger
    )
    {
        _seasonRepository = seasonRepository;
        _seasonCacheRepository = seasonCacheRepository;
        _rankingRepository = rankingRepository;
        _roundRepository = roundRepository;
        _seasonPreparationService = seasonPreparationService;
        _roundPreparationService = roundPreparationService;
        _cacheInitializationService = cacheInitializationService;
        _logger = logger;
    }

    /// <summary>
    /// Current cached block index, season and round, plus the ranking entry count of the
    /// previous, current and next round.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<RankingCacheStatusDto>>> GetStatus()
    {
        try
        {
            var blockIndex = await _seasonCacheRepository.GetBlockIndexAsync();
            var season = await _seasonCacheRepository.GetSeasonAsync();
            var round = await _seasonCacheRepository.GetRoundAsync();

            var counts = new List<RankingCountDto>();
            for (var offset = -1; offset <= 1; offset++)
            {
                var roundToCheck = round.RoundIndex + offset;
                if (roundToCheck < 1)
                {
                    continue;
                }

                counts.Add(
                    new RankingCountDto(
                        roundToCheck,
                        await _rankingRepository.GetRankingCountAsync(season.Id, roundToCheck)
                    )
                );
            }

            return Ok(
                ApiResponse<RankingCacheStatusDto>.Ok(
                    new RankingCacheStatusDto(
                        blockIndex,
                        new CachedSeasonDto(season.Id, season.StartBlock, season.EndBlock),
                        new CachedRoundDto(round.Id, round.RoundIndex, round.StartBlock, round.EndBlock),
                        counts
                    )
                )
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read the ranking cache status.");
            return StatusCode(
                500,
                ApiResponse<RankingCacheStatusDto>.Error($"Failed to read cache status: {ex.Message}")
            );
        }
    }

    /// <summary>Prepares the cache for a season using its first round.</summary>
    [HttpPost("seasons/{seasonId:int}/prepare")]
    public async Task<ActionResult<ApiResponse>> PrepareSeason(int seasonId)
    {
        try
        {
            var season = await _seasonRepository.GetSeasonAsync(seasonId, q => q.Include(s => s.Rounds));
            if (season is null)
            {
                return NotFound(ApiResponse.Error($"Season {seasonId} not found."));
            }

            var firstRound = season.Rounds.OrderBy(r => r.RoundIndex).FirstOrDefault();
            if (firstRound is null)
            {
                return BadRequest(ApiResponse.Error($"Season {seasonId} has no rounds."));
            }

            await _seasonPreparationService.PrepareSeasonAsync((season, firstRound));
            return Ok(ApiResponse.Ok($"Season {seasonId} initialized."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to prepare season {SeasonId}.", seasonId);
            return StatusCode(500, ApiResponse.Error($"Failed to prepare season: {ex.Message}"));
        }
    }

    /// <summary>Prepares the round following <paramref name="roundId"/>, taking a ranking snapshot.</summary>
    [HttpPost("rounds/{roundId:int}/prepare-next")]
    public async Task<ActionResult<ApiResponse>> PrepareNextRound(int roundId)
    {
        try
        {
            var round = await _roundRepository.GetRoundAsync(roundId, q => q.Include(r => r.Season));
            if (round is null)
            {
                return NotFound(ApiResponse.Error($"Round {roundId} not found."));
            }

            var season = await _seasonRepository.GetSeasonAsync(
                round.SeasonId,
                q => q.Include(s => s.Rounds)
            );

            await _roundPreparationService.PrepareNextRoundWithSnapshotAsync((season, round));
            return Ok(ApiResponse.Ok($"Next round after {roundId} prepared."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to prepare the round after {RoundId}.", roundId);
            return StatusCode(500, ApiResponse.Error($"Failed to prepare next round: {ex.Message}"));
        }
    }

    /// <summary>
    /// Rebuilds the ranking cache for the currently cached season and round.
    /// </summary>
    /// <remarks>
    /// Passes the cached round's <c>Id</c> as the service's round argument, exactly as the
    /// CacheInitialization Blazor page does.
    /// </remarks>
    [HttpPost("initialize")]
    public async Task<ActionResult<ApiResponse>> InitializeRankingCache()
    {
        try
        {
            var season = await _seasonCacheRepository.GetSeasonAsync();
            var round = await _seasonCacheRepository.GetRoundAsync();

            var result = await _cacheInitializationService.InitializeRankingCacheAsync(season.Id, round.Id);
            return result
                ? Ok(ApiResponse.Ok("Ranking cache initialized."))
                : StatusCode(500, ApiResponse.Error("Ranking cache initialization failed."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize the ranking cache.");
            return StatusCode(
                500,
                ApiResponse.Error($"Failed to initialize ranking cache: {ex.Message}")
            );
        }
    }
}
