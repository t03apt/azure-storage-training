using Microsoft.AspNetCore.Mvc;

namespace AzureStorageSample.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DemoController : ControllerBase
    {
        private readonly ILogger _logger;

        public DemoController(ILogger<DemoController> logger)
        {
            _logger = logger;
        }

        [HttpGet(Name = nameof(Demo))]
        public IActionResult Demo()
        {
            return Ok();
        }
    }
}
