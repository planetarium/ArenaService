using System.Text.Json;
using ArenaService.Models;
using Microsoft.AspNetCore.Mvc;

namespace ArenaService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ArenaController : ControllerBase
    {
        private readonly StackExchange.Redis.IDatabase _db;

        public ArenaController(StackExchange.Redis.IConnectionMultiplexer redis)
        {
            _db = redis.GetDatabase();
        }

        [HttpGet("participant-list")]
        public async Task<List<ArenaParticipant>> GetParticipantList(string key)
        {
            var result = await _db.StringGetAsync(key);
            return result.IsNull
                ? new List<ArenaParticipant>()
                : JsonSerializer.Deserialize<List<ArenaParticipant>>(result.ToString())!;
        }
    }
}