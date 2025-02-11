namespace ArenaService.Exceptions;

public class NotEnoughMedalException : Exception
{
    public NotEnoughMedalException(string message)
        : base(message) { }
}
