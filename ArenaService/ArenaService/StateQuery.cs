using GraphQL;
using GraphQL.Types;
using Libplanet.Crypto;
using Nekoyume.Arena;
using Nekoyume.Model.Arena;
using Nekoyume.Model.EnumType;

namespace ArenaService
{
    public class StateQuery : ObjectGraphType
    {
        public StateQuery(IRedisArenaParticipantsService redisArenaParticipantsService)
        {
            var redisArenaParticipantsService1 = redisArenaParticipantsService;
            Name = "StateQuery";
            FieldAsync<NonNullGraphType<ListGraphType<ArenaParticipantType>>>(
                "arenaParticipants",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<AddressType>>
                    {
                        Name = "avatarAddress"
                    },
                    new QueryArgument<NonNullGraphType<BooleanGraphType>>
                    {
                        Name = "filterBounds",
                        DefaultValue = true,
                    }
                ),
                resolve: async context =>
                {
                    // Copy from NineChronicles RxProps.Arena
                    // https://github.com/planetarium/NineChronicles/blob/80.0.1/nekoyume/Assets/_Scripts/State/RxProps.Arena.cs#L279
                    var currentAvatarAddr = context.GetArgument<Address>("avatarAddress");
                    var filterBounds = context.GetArgument<bool>("filterBounds");
                    int playerScore = ArenaScore.ArenaScoreDefault;
                    List<ArenaService.Models.ArenaParticipant> result = new();
                    string cacheKey;
                    try
                    {
                        cacheKey = await redisArenaParticipantsService1.GetSeasonKeyAsync();
                    }
                    catch (KeyNotFoundException)
                    {
                        // return empty list because cache not yet
                        return result;
                    }

                    var cached = await redisArenaParticipantsService1.GetArenaParticipantsAsync(cacheKey);
                    var avatarScore = cached.FirstOrDefault(r => r.AvatarAddr == currentAvatarAddr).Score;
                    if (avatarScore > 0)
                    {
                        playerScore = avatarScore;
                    }
                    foreach (var arenaParticipant in cached)
                    {
                        var (win, lose, _) = ArenaHelper.GetScores(playerScore, arenaParticipant.Score);
                        arenaParticipant.Update(win, lose);
                        result.Add(arenaParticipant);
                    }

                    if (filterBounds)
                    {
                        result = GetBoundsWithPlayerScore(result, ArenaType.Championship, playerScore);
                    }

                    return result;
                }
            );
        }

        public static List<ArenaService.Models.ArenaParticipant> GetBoundsWithPlayerScore(
            List<ArenaService.Models.ArenaParticipant> arenaInformation,
            ArenaType arenaType,
            int playerScore)
        {
            var bounds = ArenaHelper.ScoreLimits.ContainsKey(arenaType)
                ? ArenaHelper.ScoreLimits[arenaType]
                : ArenaHelper.ScoreLimits.First().Value;

            bounds = (bounds.upper + playerScore, bounds.lower + playerScore);
            return arenaInformation
                .Where(a => a.Score <= bounds.upper && a.Score >= bounds.lower)
                .ToList();
        }
    }
}
