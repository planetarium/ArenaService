namespace ArenaService.Filter;

using ArenaService.Shared.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

public class CalcAOFailedExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is CalcAOFailedException)
        {
            context.Result = new ObjectResult(new { error = "Total ranking count is under 40." })
            {
                StatusCode = StatusCodes.Status423Locked,
            };
            context.ExceptionHandled = true;
        }
    }
}
