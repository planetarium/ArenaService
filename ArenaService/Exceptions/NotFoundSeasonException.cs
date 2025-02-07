namespace ArenaService.Shared.Exceptions;

public class NotFoundSeasonException : Exception
{
    public NotFoundSeasonException(string message)
        : base(message) { }
}
