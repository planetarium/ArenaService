namespace ArenaService.Shared.Exceptions;

public class NotEnoughMedalException : Exception
{
    public NotEnoughMedalException(string message)
        : base(message) { }
}
