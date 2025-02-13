namespace ArenaService.Exceptions;

public class NotFoundSeasonException : Exception
{
    public NotFoundSeasonException(string message)
        : base(message) { }
}
