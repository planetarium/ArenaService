using System.Security.Claims;
using ArenaService.Controllers;
using ArenaService.Dtos;
using ArenaService.Repositories;
using ArenaService.Services;
using Libplanet.Crypto;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

public class UserControllerTest
{
    private readonly Mock<IUserRepository> _mockUserRepo;
    private readonly Mock<ISeasonRepository> _mockSeasonRepo;
    private readonly Mock<IMedalRepository> _mockMedalRepo;
    private readonly Mock<ISeasonService> _mockSeasonService;
    private readonly Mock<IParticipateService> _mockParticipateService;
    private readonly Mock<ISeasonCacheRepository> _mockSeasonCacheRepo;
    private readonly UserController _controller;

    public UserControllerTest()
    {
        _mockUserRepo = new Mock<IUserRepository>();
        _mockSeasonRepo = new Mock<ISeasonRepository>();
        _mockMedalRepo = new Mock<IMedalRepository>();
        _mockSeasonService = new Mock<ISeasonService>();
        _mockParticipateService = new Mock<IParticipateService>();
        _mockSeasonCacheRepo = new Mock<ISeasonCacheRepository>();

        _controller = new UserController(
            _mockUserRepo.Object,
            _mockSeasonService.Object,
            _mockSeasonRepo.Object,
            _mockMedalRepo.Object,
            _mockParticipateService.Object,
            _mockSeasonCacheRepo.Object
        );

        // Mock HttpContext for the controller
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("avatarAddress", "0x123"),
            new Claim("agentAddress", "0x456")
        }));
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    [Fact]
    public async Task Register_ReturnsBadRequest_WhenInvalidAddress()
    {
        // Arrange
        var userRequest = new UserRegisterRequest
        {
            NameWithHash = "TestUser#1234",
            PortraitId = 1,
            Cp = 100,
            Level = 10
        };

        _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("avatar_address", new PrivateKey().Address.ToString()), // Different address to simulate invalid case
            new Claim("address", new PrivateKey().Address.ToString())
        }));

        // Act
        var result = await _controller.Register(userRequest);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("invalid address.", badRequestResult.Value);
    }
}
