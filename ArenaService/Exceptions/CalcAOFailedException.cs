namespace ArenaService.Exceptions;

public class CalcAOFailedException : Exception
{
    public CalcAOFailedException(string message)
        : base(message) { }
}
