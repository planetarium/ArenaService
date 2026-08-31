using System.Text;
using ArenaService.BackOffice.Authentication;
using ArenaService.BackOffice.Models;
using ArenaService.Client;
using ArenaService.Options;
using ArenaService.Shared.Constants;
using ArenaService.Shared.Models.Enums;
using ArenaService.Shared.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ArenaService.BackOffice.Controllers;

/// <summary>
/// Completed season leaderboards and the settlement CSVs. Mirrors the Leaderboard Blazor page.
/// </summary>
[ApiController]
[Route("api/leaderboard")]
[Authorize(AuthenticationSchemes = ApiKeyAuthenticationDefaults.AuthenticationScheme)]
public class LeaderboardController : ControllerBase
{
    private const int StakeStateChunkSize = 100;

    private readonly ILeaderboardRepository _leaderboardRepository;
    private readonly ISeasonRepository _seasonRepository;
    private readonly IHeadlessClient _headlessClient;
    private readonly IOptions<HeadlessOptions> _headlessOptions;
    private readonly ILogger<LeaderboardController> _logger;

    public LeaderboardController(
        ILeaderboardRepository leaderboardRepository,
        ISeasonRepository seasonRepository,
        IHeadlessClient headlessClient,
        IOptions<HeadlessOptions> headlessOptions,
        ILogger<LeaderboardController> logger
    )
    {
        _leaderboardRepository = leaderboardRepository;
        _seasonRepository = seasonRepository;
        _headlessClient = headlessClient;
        _headlessOptions = headlessOptions;
        _logger = logger;
    }

    /// <summary>
    /// Lists seasons whose leaderboard is final: non off-season, already ended at the current tip.
    /// </summary>
    [HttpGet("seasons")]
    [Produces("application/json")]
    public async Task<ActionResult<ApiResponse<List<SeasonDto>>>> GetCompletedSeasons()
    {
        var tipIndex = await GetTipIndexAsync();
        if (tipIndex is null)
        {
            return StatusCode(
                502,
                ApiResponse<List<SeasonDto>>.Error("Failed to read the tip index from headless.")
            );
        }

        var seasons = (await _seasonRepository.GetAllSeasonsAsync())
            .Where(s => s.ArenaType != ArenaType.OFF_SEASON)
            .Where(s => s.EndBlock < tipIndex)
            .OrderByDescending(s => s.Id)
            .Select(s => SeasonDto.From(s, includeRounds: false))
            .ToList();

        return Ok(ApiResponse<List<SeasonDto>>.Ok(seasons));
    }

    /// <summary>Gets the final leaderboard of a season.</summary>
    [HttpGet("{seasonId:int}")]
    [Produces("application/json")]
    public async Task<ActionResult<ApiResponse<List<LeaderboardEntryDto>>>> GetLeaderboard(int seasonId)
    {
        var leaderboard = await _leaderboardRepository.GetLeaderboardAsync(seasonId);
        var entries = leaderboard
            .Select(item => new LeaderboardEntryDto(
                item.Rank,
                item.Score,
                item.Participant.AvatarAddress.ToString(),
                item.Participant.User.AgentAddress.ToString(),
                item.Participant.User.Level,
                item.Participant.User.Cp,
                item.Participant.User.PortraitId,
                item.Participant.TotalWin,
                item.Participant.TotalLose
            ))
            .ToList();

        return Ok(ApiResponse<List<LeaderboardEntryDto>>.Ok(entries));
    }

    /// <summary>Downloads the leaderboard CSV used for settlement.</summary>
    [HttpGet("{seasonId:int}/csv")]
    [Produces("text/csv")]
    public async Task<IActionResult> GetLeaderboardCsv(int seasonId)
    {
        var season = await _seasonRepository.GetSeasonAsync(seasonId);
        var csv = await _leaderboardRepository.GenerateLeaderboardCsvAsync(seasonId);
        var fileName =
            $"{_headlessOptions.Value.Planet}_leaderboard_group_{season.SeasonGroupId}_{season.StartBlock}_{season.EndBlock}.csv";

        return File(csv, "text/csv", fileName);
    }

    /// <summary>
    /// Downloads the staking CSV of everyone on the season's leaderboard, read from headless
    /// in chunks of 100 agent addresses.
    /// </summary>
    [HttpGet("{seasonId:int}/staking-csv")]
    [Produces("text/csv")]
    public async Task<IActionResult> GetStakingCsv(int seasonId)
    {
        var season = await _seasonRepository.GetSeasonAsync(seasonId);
        var leaderboard = await _leaderboardRepository.GetLeaderboardAsync(seasonId);

        var csv = new StringBuilder();
        csv.AppendLine(
            "BlockIndex,StakeVersion,AgentAddress,StakingAmount,StartedBlockIndex,ReceivedBlockIndex,CancellableBlockIndex,TimeStamp"
        );

        for (var offset = 0; offset < leaderboard.Count; offset += StakeStateChunkSize)
        {
            var agentAddresses = leaderboard
                .Skip(offset)
                .Take(StakeStateChunkSize)
                .Select(item => item.Participant.User.AgentAddress.ToHex().ToLower())
                .ToList();

            var result = await _headlessClient.GetStakeState.ExecuteAsync(agentAddresses);
            if (result.Data is null)
            {
                _logger.LogWarning(
                    "GetStakeState returned no data for season {SeasonId} at offset {Offset}.",
                    seasonId,
                    offset
                );
                continue;
            }

            var blockIndex = result.Data.NodeStatus.Tip.Index;
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

            foreach (var (stakeState, index) in result.Data.StateQuery.StakeStates.Select((s, i) => (s, i)))
            {
                if (stakeState is null)
                {
                    continue;
                }

                csv.AppendLine(
                    $"{blockIndex},V3,0x{agentAddresses[index]},{stakeState.Deposit},"
                        + $"{stakeState.StartedBlockIndex},{stakeState.ReceivedBlockIndex},"
                        + $"{stakeState.CancellableBlockIndex},{timestamp}"
                );
            }
        }

        var fileName =
            $"{_headlessOptions.Value.Planet}_staking_data_group_{season.SeasonGroupId}_{season.StartBlock}_{season.EndBlock}.csv";

        return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", fileName);
    }

    private async Task<long?> GetTipIndexAsync()
    {
        try
        {
            var result = await _headlessClient.GetTipIndex.ExecuteAsync();
            return result.Data?.NodeStatus.Tip.Index;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read the tip index from headless.");
            return null;
        }
    }
}
