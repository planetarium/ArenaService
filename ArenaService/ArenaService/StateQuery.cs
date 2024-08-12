using Bencodex;
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
        private readonly Codec _codec = new Codec();
        private readonly RpcClient _rpcClient;
        private readonly IRedisArenaParticipantsService _redisArenaParticipantsService;

        public StateQuery(RpcClient rpcClient, IRedisArenaParticipantsService redisArenaParticipantsService)
        {
            _rpcClient = rpcClient;
            _redisArenaParticipantsService = redisArenaParticipantsService;
            Name = "StateQuery";
            Field<NonNullGraphType<ListGraphType<ArenaParticipantType>>>(
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
                resolve: context =>
                {
                    // Copy from NineChronicles RxProps.Arena
                    // https://github.com/planetarium/NineChronicles/blob/80.0.1/nekoyume/Assets/_Scripts/State/RxProps.Arena.cs#L279
                    var blockIndex = _rpcClient.Tip.Index;
                    var currentAvatarAddr = context.GetArgument<Address>("avatarAddress");
                    var filterBounds = context.GetArgument<bool>("filterBounds");
                    // var currentRoundData = context.Source.WorldState.GetSheet<ArenaSheet>().GetRoundByBlockIndex(blockIndex);
                    int playerScore = ArenaScore.ArenaScoreDefault;
                    // var cacheKey = $"{currentRoundData.ChampionshipId}_{currentRoundData.Round}";
                    var cacheKey = "10_2";
                    List<ArenaParticipant> result = new();
                    // var scoreAddr = ArenaScore.DeriveAddress(currentAvatarAddr, currentRoundData.ChampionshipId, currentRoundData.Round);
                    // var scoreState = context.Source.WorldState.GetLegacyState(scoreAddr);
                    // if (scoreState is List scores)
                    // {
                    //     playerScore = (Integer)scores[1];
                    // }
                    result = _redisArenaParticipantsService.GetValueAsync(cacheKey).Result;
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
