using AuxiliumSoftware.AuxiliumServices.API.Models;
using AuxiliumSoftware.AuxiliumServices.API.Models.Server;
using AuxiliumSoftware.AuxiliumServices.Common.Configuration;
using AuxiliumSoftware.AuxiliumServices.Common.Services;
using Microsoft.AspNetCore.Mvc;

namespace AuxiliumSoftware.AuxiliumServices.API.Controllers
{
    [ApiController]
    [Route("/api/v3/server")]
    [Tags("Server")]
    public class ServerController : ControllerBase
    {
        private readonly IWebApplicationFirewallService _wafService;
        private readonly ILogger<ServerController> _logger;

        public ServerController(
            IWebApplicationFirewallService waf,
            ILogger<ServerController> logger
            )
        {
            _wafService = waf;
            _logger = logger;
        }

        [HttpGet("ping")]
        [ProducesResponseType(typeof(PingResponseModel), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public ActionResult<PingResponseModel> Ping()
        {
            try
            {
                return StatusCode(StatusCodes.Status200OK, new PingResponseModel
                {
                    Response = "pong!"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to ping");
                return StatusCode(StatusCodes.Status500InternalServerError, new FailureResponseModel { Detail = "Internal server error" });
            }
        }
    }
}
