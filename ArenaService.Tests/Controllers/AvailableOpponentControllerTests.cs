// using System.Security.Claims;
// using ArenaService.Controllers;
// using ArenaService.Shared.Dtos;
// using ArenaService.Shared.Models;
// using ArenaService.Shared.Repositories;
// using ArenaService.Tests.Utils;
// using Microsoft.AspNetCore.Http;
// using Microsoft.AspNetCore.Http.HttpResults;
// using Microsoft.AspNetCore.Mvc;
// using Moq;

// namespace ArenaService.Tests.Controllers;

// public class AvailableOpponentControllerTests
// {
//     private readonly AvailableOpponentController _controller;
//     private Mock<IAvailableOpponentRepository> _availableOpponentRepoMock;
//     private Mock<IParticipantRepository> _participantRepoMock;

//     public AvailableOpponentControllerTests()
//     {
//         var availableOpponentRepoMock = new Mock<IAvailableOpponentRepository>();
//         _availableOpponentRepoMock = availableOpponentRepoMock;
//         var participantRepoMock = new Mock<IParticipantRepository>();
//         _participantRepoMock = participantRepoMock;
//         _controller = new AvailableOpponentController(
//             _availableOpponentRepoMock.Object,
//             _participantRepoMock.Object
//         );
//     }

//     [Fact]
//     public async Task GetAvailableOpponents_WithValidHeader_ReturnsOk()
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
//                     new AvailableOpponents
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
//                     new AvailableOpponents
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

//         var result = await _controller.GetAvailableOpponents(1);

//         var okResult = Assert.IsType<Ok<AvailableOpponentsResponse>>(result.Result);
//         Assert.Equal(2, okResult.Value?.AvailableOpponents.Count);
//     }
// }
