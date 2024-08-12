namespace ArenaService;

public class StandaloneSchema : GraphQL.Types.Schema
{
    public StandaloneSchema(IServiceProvider serviceProvider)
        : base(serviceProvider)
    {
        Query = serviceProvider.GetRequiredService<StandaloneQuery>();
    }
}
