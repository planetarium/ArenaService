namespace ArenaService.Shared.Exceptions;

public class NotEnoughRankingCountException : Exception
{
    public NotEnoughRankingCountException(string message)
        : base(message) { }
}
