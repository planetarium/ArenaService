namespace ArenaService.Exceptions;

public class NotRankedException : Exception
{
    public NotRankedException(string message)
        : base(message) { }
}
