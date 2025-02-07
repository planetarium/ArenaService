// using System.Security.Claims;
// using ArenaService.Controllers;
// using ArenaService.Dtos;
// using ArenaService.Shared.Models;
// using ArenaService.Shared.Repositories;
// using ArenaService.Tests.Utils;
// using Microsoft.AspNetCore.Http;
// using Microsoft.AspNetCore.Http.HttpResults;
// using Microsoft.AspNetCore.Mvc;
// using Moq;

// namespace ArenaService.Tests.Controllers;

// public class ParticipantControllerTests
// {
//     private readonly ParticipantController _controller;
//     private Mock<ISeasonRepository> _seasonRepoMock;
//     private Mock<IParticipantRepository> _participantRepoMock;

//     public ParticipantControllerTests()
//     {
//         var seasonRepoMock = new Mock<ISeasonRepository>();
//         var participantRepoMock = new Mock<IParticipantRepository>();
//         _seasonRepoMock = seasonRepoMock;
//         _participantRepoMock = participantRepoMock;
//         _controller = new ParticipantController(
//             _participantRepoMock.Object,
//             _seasonRepoMock.Object
//         );
//     }

//     [Fact]
//     public async Task Join_ActivatedSeasonsExist_ReturnsOk()
//     {
//         ControllerTestUtils.ConfigureMockHttpContextWithAuth(_controller, "test-avatar-address");

//         var season = new Season
//         {
//             Id = 1,
//             StartBlockIndex = 700,
//             EndBlockIndex = 899,
//             TicketRefillInterval = 600,
//             IsActivated = true
//         };
//         var participant = new Participant
//         {
//             Id = 1,
//             AvatarAddress = "test",
//             NameWithHash = "test",
//             SeasonId = 1,
//             PortraitId = 1,
//             Season = season
//         };

//         _seasonRepoMock.Setup(repo => repo.GetSeasonAsync(season.Id)).ReturnsAsync(season);
//         _participantRepoMock
//             .Setup(repo =>
//                 repo.InsertParticipantToSpecificSeasonAsync(
//                     season.Id,
//                     participant.AvatarAddress,
//                     participant.NameWithHash,
//                     participant.PortraitId
//                 )
//             )
//             .ReturnsAsync(participant);

//         var result = await _controller.Join(
//             1,
//             new JoinRequest { NameWithHash = "test", PortraitId = 1 }
//         );

//         var okResult = Assert.IsType<Created>(result.Result);
//     }

//     [Fact]
//     public async Task Join_ActivatedSeasonsNotExist_ReturnsNotFound()
//     {
//         ControllerTestUtils.ConfigureMockHttpContextWithAuth(_controller, "test-avatar-address");
//         var season = new Season
//         {
//             Id = 1,
//             StartBlockIndex = 700,
//             EndBlockIndex = 899,
//             TicketRefillInterval = 600,
//             IsActivated = false
//         };
//         var participant = new Participant
//         {
//             Id = 1,
//             AvatarAddress = "test",
//             NameWithHash = "test",
//             SeasonId = 1,
//             PortraitId = 1,
//             Season = season
//         };

//         _seasonRepoMock.Setup(repo => repo.GetActivatedSeasonsAsync()).ReturnsAsync([season]);
//         _participantRepoMock
//             .Setup(repo =>
//                 repo.InsertParticipantToSpecificSeasonAsync(
//                     season.Id,
//                     participant.AvatarAddress,
//                     participant.NameWithHash,
//                     participant.PortraitId
//                 )
//             )
//             .ReturnsAsync(participant);

//         var result = await _controller.Join(
//             1,
//             new JoinRequest { NameWithHash = "test", PortraitId = 1 }
//         );

//         Assert.IsType<NotFound<string>>(result.Result);
//     }

//     [Fact]
//     public async Task Join_SeasonsNotExist_ReturnsNotFound()
//     {
//         ControllerTestUtils.ConfigureMockHttpContextWithAuth(_controller, "test-avatar-address");
//         var season = new Season
//         {
//             Id = 1,
//             StartBlockIndex = 700,
//             EndBlockIndex = 899,
//             TicketRefillInterval = 600,
//             IsActivated = true
//         };
//         var participant = new Participant
//         {
//             Id = 1,
//             AvatarAddress = "test",
//             NameWithHash = "test",
//             SeasonId = 1,
//             PortraitId = 1,
//             Season = season
//         };

//         _seasonRepoMock.Setup(repo => repo.GetActivatedSeasonsAsync()).ReturnsAsync([season]);
//         _participantRepoMock
//             .Setup(repo =>
//                 repo.InsertParticipantToSpecificSeasonAsync(
//                     season.Id,
//                     participant.AvatarAddress,
//                     participant.NameWithHash,
//                     participant.PortraitId
//                 )
//             )
//             .ReturnsAsync(participant);

//         var result = await _controller.Join(
//             2,
//             new JoinRequest { NameWithHash = "test", PortraitId = 1 }
//         );

//         var okResult = Assert.IsType<NotFound<string>>(result.Result);
//     }
// }
