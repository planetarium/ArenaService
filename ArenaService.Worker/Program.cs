using ArenaService.Worker;
using Lib9c.Renderers;
using Lib9c.Formatters;
using MessagePack;
using MessagePack.Resolvers;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();

builder.Services.AddHostedService<RpcNodeCheckService>().AddSingleton<RpcNodeHealthCheck>();
builder.Services.AddSingleton<RpcClient>();
builder.Services.AddSingleton<Receiver>();
builder.Services.AddHostedService<RpcService>();
builder.Services.AddSingleton<ActionRenderer>();

var resolver = MessagePack.Resolvers.CompositeResolver.Create(
    NineChroniclesResolver.Instance,
    StandardResolver.Instance
);
var options = MessagePackSerializerOptions.Standard.WithResolver(resolver);
MessagePackSerializer.DefaultOptions = options;

var host = builder.Build();
host.Run();
