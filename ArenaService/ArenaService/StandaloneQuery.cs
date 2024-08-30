using GraphQL.Types;

namespace ArenaService;

public class StandaloneQuery : ObjectGraphType
{
    public StandaloneQuery(IRedisArenaParticipantsService redisArenaParticipantsService)
    {
        Field<NonNullGraphType<StateQuery>>(name: "stateQuery", resolve: _ => new StateQuery(redisArenaParticipantsService));
    }
}
