namespace ArenaService.Controllers;

using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("/")]
public class HealthCheckController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Index()
    {
        return Ok();
    }
}
