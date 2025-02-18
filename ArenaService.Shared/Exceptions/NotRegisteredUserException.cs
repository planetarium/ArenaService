namespace ArenaService.Shared.Exceptions;

public class NotRegisteredUserException : Exception
{
    public NotRegisteredUserException(string message)
        : base(message) { }
}
