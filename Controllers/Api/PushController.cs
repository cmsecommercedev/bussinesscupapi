using BussinessCupApi.Attributes;
using BussinessCupApi.Managers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace BussinessCupApi.Controllers.Api
{
    [ApiKeyAuth]
    [Route("api/[controller]")]
    [ApiController]
    public class PushController : ControllerBase
    {
        private readonly NotificationManager _notificationManager;
        private readonly ILogger<PushController> _logger;

        public PushController(NotificationManager notificationManager, ILogger<PushController> logger)
        {
            _notificationManager = notificationManager;
            _logger = logger;
        }

        [HttpPost("send")]
        public async Task<IActionResult> Send([FromBody] NotificationViewModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _notificationManager.SendNotificationToAllUsers(model);
            if (result.success)
                return Ok(new { success = true, message = result.message });

            _logger.LogWarning("Push send failed: {Message}", result.message);
            return StatusCode(500, new { success = false, message = result.message });
        }
    }
}


