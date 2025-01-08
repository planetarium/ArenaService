namespace ArenaService.Options;

public class RedisOptions
{
    public const string SectionName = "Redis";
    public string Host { get; set; } = "127.0.0.1";
    public string Port { get; set; } = "6379";
    public string HangfirePrefix { get; set; } = "arena_hangfire:";
    public string HangfireDbNumber { get; set; } = "0";
    public string RankingDbNumber { get; set; } = "1";
}
