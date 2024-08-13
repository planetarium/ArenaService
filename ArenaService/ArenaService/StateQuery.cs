using Bencodex;
using Bencodex.Types;
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
        private readonly RpcClient _rpcClient;
        private readonly IRedisArenaParticipantsService _redisArenaParticipantsService;

        public StateQuery(RpcClient rpcClient, IRedisArenaParticipantsService redisArenaParticipantsService)
        {
            _rpcClient = rpcClient;
            _redisArenaParticipantsService = redisArenaParticipantsService;
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
                    List<ArenaParticipant> result = new();
                    string cacheKey;
                    try
                    {
                        cacheKey = await _redisArenaParticipantsService.GetSeasonKeyAsync();
                    }
                    catch (KeyNotFoundException)
                    {
                        // return empty list because cache not yet
                        return result;
                    }

                    var scores = await _redisArenaParticipantsService.GetAvatarAddrAndScoresWithRank($"{cacheKey}_score");
                    var avatarScore = scores.FirstOrDefault(r => r.avatarAddr == currentAvatarAddr);
                    if (avatarScore.score > 0)
                    {
                        playerScore = avatarScore.score;
                    }
                    result = await _redisArenaParticipantsService.GetArenaParticipantsAsync(cacheKey);
                    foreach (var arenaParticipant in result)
                    {
                        var (win, lose, _) = ArenaHelper.GetScores(playerScore, arenaParticipant.Score);
                        arenaParticipant.WinScore = win;
                        arenaParticipant.LoseScore = lose;
                    }

                    if (filterBounds)
                    {
                        result = GetBoundsWithPlayerScore(result, ArenaType.Championship, playerScore);
                    }

                    return result;
                }
            );
        }

        public static List<ArenaParticipant> GetBoundsWithPlayerScore(
            List<ArenaParticipant> arenaInformation,
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
