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

public class AvailableOpponentControllerTests
{
    private readonly AvailableOpponentController _controller;
    private Mock<IAvailableOpponentRepository> _availableOpponentRepositoryMock;
    private AvailableOpponentService _availableOpponentService;
    private Mock<IParticipantRepository> _participantRepositoryMock;
    private ParticipantService _participantService;

    public AvailableOpponentControllerTests()
    {
        var availableOpponentRepositoryMock = new Mock<IAvailableOpponentRepository>();
        _availableOpponentRepositoryMock = availableOpponentRepositoryMock;
        _availableOpponentService = new AvailableOpponentService(
            _availableOpponentRepositoryMock.Object
        );
        var participantRepositoryMock = new Mock<IParticipantRepository>();
        _participantRepositoryMock = participantRepositoryMock;
        _participantService = new ParticipantService(_participantRepositoryMock.Object);
        _controller = new AvailableOpponentController(
            _availableOpponentService,
            _participantService
        );
    }

    [Fact]
    public async Task GetAvailableOpponents_WithValidHeader_ReturnsOk()
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

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(
            new ClaimsIdentity([new Claim("avatar", avatarAddress)])
        );

        var result = await _controller.GetAvailableOpponents(1);

        var okResult = Assert.IsType<Ok<AvailableOpponentsResponse>>(result.Result);
        Assert.Equal(2, okResult.Value?.AvailableOpponents.Count);
    }
}
