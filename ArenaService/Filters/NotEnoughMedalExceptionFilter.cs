namespace ArenaService.Filter;

using ArenaService.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

public class NotEnoughMedalExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is NotRegisteredUserException)
        {
            context.Result = new ObjectResult(new { error = "Not enough medal." })
            {
                StatusCode = StatusCodes.Status404NotFound
            };
            context.ExceptionHandled = true;
        }
    }
}
