namespace ArenaService.Options;

public class SentryOptions
{
    public const string SectionName = "Sentry";
    
    public string? Dsn { get; set; }
    
    public bool Enabled { get; set; } = false;
    
    public string? Environment { get; set; }
    
    public bool Debug { get; set; } = false;
} 