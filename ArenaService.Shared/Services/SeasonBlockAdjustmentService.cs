using ArenaService.Shared.Data;
using ArenaService.Shared.Models;
using ArenaService.Shared.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ArenaService.Shared.Services;

public interface ISeasonBlockAdjustmentService
{
    Task<Season> AdjustSeasonEndBlockAsync(int seasonId, long newEndBlock);
}

public class SeasonBlockAdjustmentService : ISeasonBlockAdjustmentService
{
    private readonly ArenaDbContext _context;
    private readonly ISeasonRepository _seasonRepository;
    private readonly IRoundRepository _roundRepository;

    public SeasonBlockAdjustmentService(
        ArenaDbContext context,
        ISeasonRepository seasonRepository,
        IRoundRepository roundRepository)
    {
        _context = context;
        _seasonRepository = seasonRepository;
        _roundRepository = roundRepository;
    }

    public async Task<Season> AdjustSeasonEndBlockAsync(int seasonId, long newEndBlock)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        
        try
        {
            var season = await _context.Seasons
                .Include(s => s.Rounds.OrderBy(r => r.StartBlock))
                .SingleAsync(s => s.Id == seasonId);

            var oldEndBlock = season.EndBlock;
            var blockDifference = newEndBlock - oldEndBlock;

            if (blockDifference == 0)
            {
                return season;
            }

            season.EndBlock = newEndBlock;
            await _context.SaveChangesAsync();

            await AdjustRoundsForSeason(season, oldEndBlock, newEndBlock);
            await AdjustOtherSeasonsAndRounds(seasonId, blockDifference);

            await transaction.CommitAsync();
            return season;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private async Task AdjustRoundsForSeason(Season season, long oldEndBlock, long newEndBlock)
    {
        var rounds = season.Rounds.OrderBy(r => r.StartBlock).ToList();
        var blockDifference = newEndBlock - oldEndBlock;

        if (blockDifference > 0)
        {
            await HandleEndBlockIncrease(season, rounds, blockDifference);
        }
        else
        {
            await HandleEndBlockDecrease(season, rounds, Math.Abs(blockDifference));
        }
    }

    private async Task HandleEndBlockIncrease(Season season, List<Round> rounds, long blockDifference)
    {
        var lastRound = rounds.LastOrDefault();
        if (lastRound == null) return;

        var currentStartBlock = lastRound.EndBlock + 1;
        var newRounds = new List<Round>();
        var nextRoundIndex = rounds.Max(r => r.RoundIndex) + 1;

        while (currentStartBlock <= season.EndBlock)
        {
            var currentEndBlock = Math.Min(currentStartBlock + season.RoundInterval - 1, season.EndBlock);
            
            if (currentEndBlock >= currentStartBlock)
            {
                var newRound = new Round
                {
                    SeasonId = season.Id,
                    StartBlock = currentStartBlock,
                    EndBlock = currentEndBlock,
                    RoundIndex = nextRoundIndex++
                };

                newRounds.Add(newRound);
            }

            currentStartBlock = currentEndBlock + 1;
        }

        if (newRounds.Any())
        {
            _context.Rounds.AddRange(newRounds);
            await _context.SaveChangesAsync();
        }
    }

    private async Task HandleEndBlockDecrease(Season season, List<Round> rounds, long blockDifference)
    {
        var roundsToRemove = new List<Round>();
        var roundsToAdjust = new List<Round>();

        foreach (var round in rounds.OrderByDescending(r => r.StartBlock))
        {
            if (round.EndBlock > season.EndBlock)
            {
                if (round.StartBlock > season.EndBlock)
                {
                    roundsToRemove.Add(round);
                }
                else
                {
                    round.EndBlock = season.EndBlock;
                    roundsToAdjust.Add(round);
                }
            }
        }

        if (roundsToRemove.Any())
        {
            _context.Rounds.RemoveRange(roundsToRemove);
        }

        if (roundsToAdjust.Any())
        {
            _context.Rounds.UpdateRange(roundsToAdjust);
        }

        await _context.SaveChangesAsync();
    }

    private async Task AdjustOtherSeasonsAndRounds(int adjustedSeasonId, long blockDifference)
    {
        var subsequentSeasons = await _context.Seasons
            .Where(s => s.Id > adjustedSeasonId)
            .OrderBy(s => s.Id)
            .ToListAsync();

        foreach (var season in subsequentSeasons)
        {
            season.StartBlock += blockDifference;
            season.EndBlock += blockDifference;
        }

        var subsequentRounds = await _context.Rounds
            .Where(r => r.SeasonId > adjustedSeasonId)
            .ToListAsync();

        foreach (var round in subsequentRounds)
        {
            round.StartBlock += blockDifference;
            round.EndBlock += blockDifference;
        }

        await _context.SaveChangesAsync();
    }
} 