namespace ArenaService.Options;

public class OpsConfigOptions
{
    public const string SectionName = "OpsConfig";

    public required string RecipientAddress { get; init; }
}
