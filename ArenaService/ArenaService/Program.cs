using ArenaService;
using ArenaService.Services;
using GraphQL.Server;
using GraphQL.Server.Ui.Playground;
using GraphQL.Types;
using Libplanet.Crypto;
using StackExchange.Redis;
using AddressType = ArenaService.AddressType;

var builder = WebApplication.CreateBuilder(args);

var redisConnectionString = "localhost:6379";
var configurationOptions = new ConfigurationOptions
{
    EndPoints = { redisConnectionString },
    ConnectTimeout = 500,
    SyncTimeout = 500,
};

var redis = await ConnectionMultiplexer.ConnectAsync(configurationOptions);


// Add services to the container.
builder.Services
    .AddSingleton<RpcClient>()
    .AddSingleton(new PrivateKey())
    .AddSingleton<IConnectionMultiplexer>(_ => redis)
    .AddSingleton<IRedisArenaParticipantsService, RedisArenaParticipantsService>()
    // .AddHostedService<ArenaParticipantsWorker>()
    .AddScoped<ISchema, StandaloneSchema>()
    .AddGraphQL(options => options.EnableMetrics = true)
    .AddSystemTextJson()
    .AddGraphTypes(typeof(AddressType))
    .AddGraphTypes(typeof(StandaloneQuery));


var app = builder.Build();
app
    .UseRouting()
    .UseGraphQL<ISchema>()
    .UseGraphQLPlayground(new PlaygroundOptions
    {
        GraphQLEndPoint = "/graphql"
    });
app.Run();
