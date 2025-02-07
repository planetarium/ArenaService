namespace ArenaService.Shared.Exceptions;

public class CacheUnavailableException : Exception
{
    public CacheUnavailableException(string message)
        : base(message) { }
}
