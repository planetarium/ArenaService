using GraphQL.Types;

namespace ArenaService;

public class StandaloneQuery : ObjectGraphType
{
    public StandaloneQuery(RpcClient rpcClient, IRedisArenaParticipantsService redisArenaParticipantsService)
    {
        Field<NonNullGraphType<StateQuery>>(name: "stateQuery", resolve: _ => new StateQuery(redisArenaParticipantsService));
    }
}
