using AuxiliumSoftware.AuxiliumServices.API.Mappers;
using AuxiliumSoftware.AuxiliumServices.API.Models;
using AuxiliumSoftware.AuxiliumServices.API.Models.DataEnumerator;
using AuxiliumSoftware.AuxiliumServices.Common.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuxiliumSoftware.AuxiliumServices.API.Controllers
{
    [AllowAnonymous]
    [ApiController]
    [Route("/api/v3/data-enumerators")]
    [Tags("Data Enumerators")]
    public class DataEnumeratorsController : ControllerBase
    {
        private readonly ILogger<DataEnumeratorsController> _logger;
        private readonly IDataEnumeratorService _dataEnumeratorService;

        public DataEnumeratorsController(
            ILogger<DataEnumeratorsController> logger,
            IDataEnumeratorService dataEnumeratorService
        )
        {
            _logger = logger;
            _dataEnumeratorService = dataEnumeratorService;
        }

        [HttpGet("")]
        [ProducesResponseType(typeof(List<DataEnumeratorResponseModel>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<DataEnumeratorResponseModel>>> GetAllEnumerators(
            [FromQuery] string? locale = null,
            CancellationToken ct = default)
        {
            try
            {
                var enumerators = await _dataEnumeratorService.GetAllEnumeratorsAsync(
                    includeInactive: false,
                    ct
                );

                var response = enumerators
                    .Select(e => DataEnumeratorMapper.ToResponseModel(e, locale))
                    .ToList();

                return StatusCode(StatusCodes.Status200OK, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get enumerators");
                return StatusCode(StatusCodes.Status500InternalServerError, new FailureResponseModel
                {
                    Detail = "An unexpected error occurred."
                });
            }
        }

        [HttpGet("by-name/{name}")]
        [ProducesResponseType(typeof(DataEnumeratorResponseModel), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(FailureResponseModel), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<DataEnumeratorResponseModel>> GetEnumeratorByName(
            [FromRoute] string name,
            [FromQuery] string? locale = null,
            CancellationToken ct = default)
        {
            try
            {
                var enumerator = await _dataEnumeratorService.GetEnumeratorByNameAsync(
                    name,
                    activeValuesOnly: true,
                    ct
                );

                if (enumerator == null)
                    return StatusCode(StatusCodes.Status404NotFound, new FailureResponseModel
                    {
                        Detail = "Enumerator not found."
                    });

                return StatusCode(StatusCodes.Status200OK, DataEnumeratorMapper.ToResponseModel(enumerator, locale));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get enumerator by name {Name}", name);
                return StatusCode(StatusCodes.Status500InternalServerError, new FailureResponseModel
                {
                    Detail = "An unexpected error occurred."
                });
            }
        }
    }
}
