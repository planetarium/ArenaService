namespace ArenaService.Shared.Exceptions;

public class CalcAOFailedException : Exception
{
    public CalcAOFailedException(string message)
        : base(message) { }
}
