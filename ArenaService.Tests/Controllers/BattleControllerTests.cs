// using System.Security.Claims;
// using ArenaService.Controllers;
// using ArenaService.Models;
// using ArenaService.Repositories;
// using ArenaService.Tests.Utils;
// using Microsoft.AspNetCore.Http;
// using Microsoft.AspNetCore.Http.HttpResults;
// using Microsoft.AspNetCore.Mvc;
// using Moq;

// namespace ArenaService.Tests.Controllers;

// public class BattleControllerTests
// {
//     private readonly BattleController _controller;
//     private Mock<IAvailableOpponentRepository> _availableOpponentRepoMock;
//     private Mock<IParticipantRepository> _participantRepoMock;
//     private Mock<IBattleRepository> _battleRepoMock;

//     public BattleControllerTests()
//     {
//         var availableOpponentRepoMock = new Mock<IAvailableOpponentRepository>();
//         _availableOpponentRepoMock = availableOpponentRepoMock;
//         var participantRepoMock = new Mock<IParticipantRepository>();
//         _participantRepoMock = participantRepoMock;
//         var battleRepoMock = new Mock<IBattleRepository>();
//         _battleRepoMock = battleRepoMock;
//         _controller = new BattleController(
//             _availableOpponentRepoMock.Object,
//             _participantRepoMock.Object,
//             _battleRepoMock.Object
//         );
//     }

//     [Fact]
//     public async Task GetBattleToken_WithValidHeader_ReturnsOk()
//     {
//         var avatarAddress = "DDF1472fD5a79B8F46C28e7643eDEF045e36BD3d";
//         ControllerTestUtils.ConfigureMockHttpContextWithAuth(_controller, avatarAddress);

//         _participantRepoMock
//             .Setup(repo => repo.GetParticipantByAvatarAddressAsync(1, avatarAddress))
//             .ReturnsAsync(
//                 new Participant
//                 {
//                     Id = 1,
//                     AvatarAddress = avatarAddress,
//                     NameWithHash = "test",
//                     PortraitId = 1
//                 }
//             );

//         _availableOpponentRepoMock
//             .Setup(repo => repo.GetAvailableOpponents(1))
//             .ReturnsAsync(
//                 [
//                     new AvailableOpponent
//                     {
//                         Id = 1,
//                         ParticipantId = 1,
//                         OpponentId = 2,
//                         Opponent = new Participant
//                         {
//                             Id = 2,
//                             AvatarAddress = "test",
//                             NameWithHash = "opponent1",
//                             PortraitId = 1
//                         },
//                         RefillBlockIndex = 1
//                     },
//                     new AvailableOpponent
//                     {
//                         Id = 2,
//                         ParticipantId = 1,
//                         OpponentId = 3,
//                         Opponent = new Participant
//                         {
//                             Id = 3,
//                             AvatarAddress = "test",
//                             NameWithHash = "opponent2",
//                             PortraitId = 1
//                         },
//                         RefillBlockIndex = 1
//                     }
//                 ]
//             );

//         _battleRepoMock
//             .Setup(repo => repo.AddBattleAsync(1, 1, 1, "token"))
//             .ReturnsAsync(
//                 new Battle
//                 {
//                     Id = 1,
//                     ParticipantId = 1,
//                     OpponentId = 1,
//                     SeasonId = 1,
//                     Token = "token"
//                 }
//             );

//         var result = await _controller.CreateBattleToken(1, 1);

//         var okResult = Assert.IsType<Ok<string>>(result.Result);
//     }
// }
