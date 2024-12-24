namespace ArenaService.Exceptions;

public class SeasonNotActivatedException : Exception
{
    public SeasonNotActivatedException(int seasonId)
        : base($"This Season is not activated, id: {seasonId}") { }
}
