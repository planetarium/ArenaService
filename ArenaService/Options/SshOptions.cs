namespace ArenaService.Options;

public class SshOptions
{
    public const string SectionName = "Ssh";
    public bool Enabled { get; set; } = false;
    public string Host { get; set; } = "";
    public int Port { get; set; } = 22;
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
}
