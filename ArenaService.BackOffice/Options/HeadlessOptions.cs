namespace ArenaService.Options;

public class HeadlessOptions
{
    public const string SectionName = "Headless";

    public required Uri HeadlessEndpoint { get; init; }
    public required string Planet { get; init; }

    public string? JwtIssuer { get; init; }

    public string? JwtSecretKey { get; init; }
}
