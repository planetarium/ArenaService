namespace ArenaService.Exceptions;

public class SeasonNotFoundException : Exception
{
    public SeasonNotFoundException(int seasonId)
        : base($"NotFound season for id: {seasonId}") { }
}
