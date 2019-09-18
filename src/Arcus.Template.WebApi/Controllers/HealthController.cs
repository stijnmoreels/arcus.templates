﻿using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using GuardNet;
using Swashbuckle.AspNetCore.Annotations;

namespace Arcus.Template.WebApi.Controllers
{
    /// <summary>
    /// API endpoint to check the health of the application.
    /// </summary>
    [ApiController]
    [Route("api/v1/[controller]")]
    public class HealthController : ControllerBase
    {
        private readonly HealthCheckService _healthCheckService;

        /// <summary>
        /// Initializes a new instance of the <see cref="HealthController"/> class.
        /// </summary>
        /// <param name="healthCheckService">The service to provide the health of the API application.</param>
        public HealthController(HealthCheckService healthCheckService)
        {
            Guard.NotNull(healthCheckService, nameof(healthCheckService));

            _healthCheckService = healthCheckService;
        }

        /// <summary>
        ///     Get Health
        /// </summary>
        /// <remarks>Provides an indication about the health of the API.</remarks>
        /// <response code="200">API is healthy</response>
        /// <response code="503">API is unhealthy or in degraded state</response>
        [HttpGet]
        [ProducesResponseType(typeof(HealthReport), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(HealthReport), StatusCodes.Status503ServiceUnavailable)]
        [SwaggerOperation(OperationId = "Health_Get")]
        public async Task<IActionResult> Get()
        {
            HealthReport healthReport = await _healthCheckService.CheckHealthAsync();
            
            if (healthReport?.Status == HealthStatus.Healthy)
            {
                return Ok(healthReport);
            }
            else
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, healthReport);
            }
        }
    }
}