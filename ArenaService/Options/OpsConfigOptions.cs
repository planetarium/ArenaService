namespace ArenaService.Options;

public class OpsConfigOptions
{
    public const string SectionName = "OpsConfig";

    public required string JwtSecretKey { get; init; }
    public required string RecipientAddress { get; init; }
    public required string ArenaProviderName { get; init; }
    public required string HangfireUsername { get; init; }
    public required string HangfirePassword { get; init; }
}
