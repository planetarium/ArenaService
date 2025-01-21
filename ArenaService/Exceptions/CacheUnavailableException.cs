namespace ArenaService.Exceptions;

public class CacheUnavailableException : Exception
{
    public CacheUnavailableException(string message)
        : base(message) { }
}
