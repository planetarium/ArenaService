using System.Security.Claims;
using ArenaService.Controllers;
using ArenaService.Dtos;
using ArenaService.Models;
using ArenaService.Repositories;
using ArenaService.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace ArenaService.Tests.Controllers;

public class BattleControllerTests
{
    private readonly BattleController _controller;
    private Mock<IAvailableOpponentRepository> _availableOpponentRepositoryMock;
    private AvailableOpponentService _availableOpponentService;
    private Mock<IParticipantRepository> _participantRepositoryMock;
    private Mock<IBattleLogRepository> _battleLogRepositoryMock;
    private ParticipantService _participantService;

    public BattleControllerTests()
    {
        var availableOpponentRepositoryMock = new Mock<IAvailableOpponentRepository>();
        _availableOpponentRepositoryMock = availableOpponentRepositoryMock;
        _availableOpponentService = new AvailableOpponentService(
            _availableOpponentRepositoryMock.Object
        );
        var participantRepositoryMock = new Mock<IParticipantRepository>();
        _participantRepositoryMock = participantRepositoryMock;
        _participantService = new ParticipantService(_participantRepositoryMock.Object);
        var battleLogRepositoryMock = new Mock<IBattleLogRepository>();
        _battleLogRepositoryMock = battleLogRepositoryMock;
        _controller = new BattleController(
            _availableOpponentService,
            _participantService,
            _battleLogRepositoryMock.Object
        );
    }

    [Fact]
    public async Task GetBattleToken_WithValidHeader_ReturnsOk()
    {
        var avatarAddress = "DDF1472fD5a79B8F46C28e7643eDEF045e36BD3d";

        _participantRepositoryMock
            .Setup(repo => repo.GetParticipantByAvatarAddressAsync(1, avatarAddress))
            .ReturnsAsync(
                new Participant
                {
                    Id = 1,
                    AvatarAddress = avatarAddress,
                    NameWithHash = "test",
                    PortraitId = 1
                }
            );

        _availableOpponentRepositoryMock
            .Setup(repo => repo.GetAvailableOpponents(1))
            .ReturnsAsync(
                [
                    new AvailableOpponent
                    {
                        Id = 1,
                        ParticipantId = 1,
                        OpponentId = 2,
                        Opponent = new Participant
                        {
                            Id = 2,
                            AvatarAddress = "test",
                            NameWithHash = "opponent1",
                            PortraitId = 1
                        },
                        RefillBlockIndex = 1
                    },
                    new AvailableOpponent
                    {
                        Id = 2,
                        ParticipantId = 1,
                        OpponentId = 3,
                        Opponent = new Participant
                        {
                            Id = 3,
                            AvatarAddress = "test",
                            NameWithHash = "opponent2",
                            PortraitId = 1
                        },
                        RefillBlockIndex = 1
                    }
                ]
            );

        _battleLogRepositoryMock
            .Setup(repo => repo.AddBattleLogAsync(1, 1, 1, "token"))
            .ReturnsAsync(
                new BattleLog
                {
                    Id = 1,
                    ParticipantId = 1,
                    OpponentId = 1,
                    SeasonId = 1,
                    Token = "token"
                }
            );

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(
            new ClaimsIdentity([new Claim("avatar", avatarAddress)])
        );

        var result = await _controller.CreateBattleToken(1, 1);

        var okResult = Assert.IsType<Ok<string>>(result.Result);
    }
}
