namespace ArenaService.BackOffice.Options;

public class RedisOptions
{
    public const string SectionName = "Redis";
    public string Host { get; set; } = "127.0.0.1";
    public string Port { get; set; } = "6379";
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public int RankingDbNumber { get; set; } = 1;
}
