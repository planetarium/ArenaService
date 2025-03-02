using ArenaService.Shared.Dtos;
using ArenaService.Shared.Models;
using ArenaService.Shared.Models.BattleTicket;
using Xunit;

namespace ArenaService.Tests.Dtos;

public class BattleTicketStatusResponseTests
{
    private static BattleTicketPolicy CreateDefaultPolicy(
        int maxPurchasableTicketsPerSeason = 24,
        int maxPurchasableTicketsPerRound = 4,
        int defaultTicketsPerRound = 5
    )
    {
        return new BattleTicketPolicy
        {
            Name = "TestPolicy",
            MaxPurchasableTicketsPerSeason = maxPurchasableTicketsPerSeason,
            MaxPurchasableTicketsPerRound = maxPurchasableTicketsPerRound,
            DefaultTicketsPerRound = defaultTicketsPerRound,
            PurchasePrices = Enumerable.Range(1, 24)
                .Select(i => i * 0.1m)
                .ToList()
        };
    }

    private static Season CreateDefaultSeason()
    {
        return new Season
        {
            BattleTicketPolicy = CreateDefaultPolicy()
        };
    }

    public class FromBattleStatusModelsTests
    {
        [Fact]
        public void WhenBothSeasonAndRoundLimitsAreAvailable_ShouldReturnCorrectValues()
        {
            // Arrange
            var policy = CreateDefaultPolicy();
            var seasonStatus = new BattleTicketStatusPerSeason
            {
                PurchaseCount = 5,
                UsedCount = 3,
                BattleTicketPolicy = policy
            };
            var roundStatus = new BattleTicketStatusPerRound
            {
                PurchaseCount = 2,
                UsedCount = 1,
                RemainingCount = 6,
                BattleTicketPolicy = policy
            };

            // Act
            var result = BattleTicketStatusResponse.FromBattleStatusModels(seasonStatus, roundStatus);

            // Assert
            Assert.Equal(5, result.TicketsPurchasedPerSeason);
            Assert.Equal(3, result.TicketsUsedPerSeason);
            Assert.Equal(19, result.RemainingPurchasableTicketsPerSeason); // 24 - 5
            Assert.Equal(2, result.TicketsPurchasedPerRound);
            Assert.Equal(1, result.TicketsUsedPerRound);
            Assert.Equal(6, result.RemainingTicketsPerRound);
            Assert.Equal(2, result.RemainingPurchasableTicketsPerRound); // min(19, 4-2)
            Assert.False(result.IsUnused);
            Assert.Equal(
                Enumerable.Range(6, 19).Select(i => i * 0.1m).ToList(),
                result.NextNCGCosts
            );
        }
    }

    public class CreateBattleTicketDefaultTests
    {
        [Fact]
        public void WhenCreatingNewSeasonDefault_ShouldSetCorrectInitialValues()
        {
            // Arrange
            var season = CreateDefaultSeason();

            // Act
            var result = BattleTicketStatusResponse.CreateBattleTicketDefault(season);

            // Assert
            Assert.Equal(0, result.TicketsPurchasedPerSeason);
            Assert.Equal(0, result.TicketsUsedPerSeason);
            Assert.Equal(24, result.RemainingPurchasableTicketsPerSeason);
            Assert.Equal(0, result.TicketsPurchasedPerRound);
            Assert.Equal(0, result.TicketsUsedPerRound);
            Assert.Equal(5, result.RemainingTicketsPerRound);
            Assert.Equal(4, result.RemainingPurchasableTicketsPerRound);
            Assert.True(result.IsUnused);
            Assert.Equal(24, result.NextNCGCosts.Count);
        }

        [Fact]
        public void WhenCreatingNewRoundWithExistingSeasonStatus_ShouldConsiderSeasonLimits()
        {
            // Arrange
            var season = CreateDefaultSeason();
            var seasonStatus = new BattleTicketStatusPerSeason
            {
                PurchaseCount = 22,
                UsedCount = 20,
                BattleTicketPolicy = CreateDefaultPolicy()
            };

            // Act
            var result = BattleTicketStatusResponse.CreateBattleTicketDefault(season, seasonStatus);

            // Assert
            Assert.Equal(22, result.TicketsPurchasedPerSeason);
            Assert.Equal(20, result.TicketsUsedPerSeason);
            Assert.Equal(2, result.RemainingPurchasableTicketsPerSeason);
            Assert.Equal(0, result.TicketsPurchasedPerRound);
            Assert.Equal(0, result.TicketsUsedPerRound);
            Assert.Equal(5, result.RemainingTicketsPerRound);
            Assert.Equal(2, result.RemainingPurchasableTicketsPerRound); // Should be limited by season remaining (2) not round max (4)
            Assert.True(result.IsUnused);
            Assert.Equal(2, result.NextNCGCosts.Count);
        }
    }

    public class CalculateRemainingPurchasableTicketsPerRoundTests
    {
        [Theory]
        [InlineData(0, 23, 4, 24, 1)]
        [InlineData(3, 21, 4, 24, 1)]
        [InlineData(4, 10, 4, 24, 0)] // 라운드 한도 초과
        [InlineData(2, 24, 4, 24, 0)] // 시즌 한도 초과
        [InlineData(1, 22, 4, 24, 2)] // 시즌 남은 수량이 더 적은 경우
        [InlineData(2, 10, 4, 24, 2)] // 라운드 남은 수량이 더 적은 경우
        [InlineData(0, 0, 4, 24, 4)] // 초기 상태
        public void CalculateRemainingPurchasableTickets_ShouldReturnCorrectValue(
            int roundPurchaseCount,
            int seasonPurchaseCount,
            int maxPurchasableTicketsPerRound,
            int maxPurchasableTicketsPerSeason,
            int expectedResult
        )
        {
            // Arrange
            var policy = CreateDefaultPolicy(
                maxPurchasableTicketsPerSeason,
                maxPurchasableTicketsPerRound
            );
            var seasonStatus = new BattleTicketStatusPerSeason
            {
                PurchaseCount = seasonPurchaseCount,
                BattleTicketPolicy = policy
            };
            var roundStatus = new BattleTicketStatusPerRound
            {
                PurchaseCount = roundPurchaseCount,
                BattleTicketPolicy = policy
            };

            // Act
            var result = BattleTicketStatusResponse.FromBattleStatusModels(seasonStatus, roundStatus);

            // Assert
            Assert.Equal(expectedResult, result.RemainingPurchasableTicketsPerRound);
        }
    }
}
