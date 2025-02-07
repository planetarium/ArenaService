namespace ArenaService.Filter;

using ArenaService.Shared.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

public class CacheExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is CacheUnavailableException)
        {
            context.Result = new ObjectResult(new { error = "Cache service is unavailable." })
            {
                StatusCode = StatusCodes.Status503ServiceUnavailable
            };
            context.ExceptionHandled = true;
        }
    }
}
