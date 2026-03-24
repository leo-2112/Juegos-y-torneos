using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Juegos.API.Services;
using Juegos.API.DTOs;
using System.Security.Claims;

namespace Juegos.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TorneosController : ControllerBase
    {
        private readonly ITournamentService _tournamentService;
        private readonly ILogger<TorneosController> _logger;

        public TorneosController(ITournamentService tournamentService, ILogger<TorneosController> logger)
        {
            _tournamentService = tournamentService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> CreateTournament([FromBody] CreateTournamentDto createDto)
        {
            try
            {
                var userId = User.FindFirstValue("sub") ?? string.Empty;
                var userRole = User.FindFirstValue("role") ?? string.Empty;

                var tournament = await _tournamentService.CreateTournament(createDto, userId, userRole);
                return StatusCode(201, tournament);
            }
            catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
            catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError($"Error creando torneo: {ex.Message}");
                return StatusCode(500, new { message = "Error interno del servidor. " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetTournaments(
            [FromQuery] string? gameId, 
            [FromQuery] string? status, 
            [FromQuery] int? minFee, 
            [FromQuery] int? maxFee, 
            [FromQuery] int? minLevel, 
            [FromQuery] int? maxLevel, 
            [FromQuery] int page = 1, 
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var tournaments = await _tournamentService.GetTournaments(gameId, status, minFee, maxFee, minLevel, maxLevel, page, pageSize);
                return Ok(tournaments);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error obteniendo torneos: {ex.Message}");
                return StatusCode(500, new { message = "Error interno del servidor al consultar torneos." });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTournament(string id, [FromBody] UpdateTournamentDto updateDto)
        {
            try
            {
                var userId = User.FindFirstValue("sub") ?? string.Empty;
                var userRole = User.FindFirstValue("role") ?? string.Empty;

                var updatedTournament = await _tournamentService.UpdateTournament(id, updateDto, userId, userRole);
                return Ok(updatedTournament);
            }
            catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
            catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError($"Error actualizando torneo: {ex.Message}");
                return StatusCode(500, new { message = "Error interno del servidor. " + ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> CancelTournament(string id)
        {
            try
            {
                var userId = User.FindFirstValue("sub") ?? string.Empty;
                var userRole = User.FindFirstValue("role") ?? string.Empty;

                await _tournamentService.CancelTournament(id, userId, userRole);
                return Ok(new { message = "Torneo cancelado exitosamente." });
            }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
            catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError($"Error cancelando torneo: {ex.Message}");
                return StatusCode(500, new { message = "Error interno del servidor." });
            }
        }

        [HttpPatch("{id}/cambiar-estado")]
        public async Task<IActionResult> ChangeTournamentStatus(string id, [FromBody] ChangeStatusDto statusDto)
        {
            try
            {
                var userId = User.FindFirstValue("sub") ?? string.Empty;
                var userRole = User.FindFirstValue("role") ?? string.Empty;

                var updatedTournament = await _tournamentService.ChangeStatus(id, statusDto, userId, userRole);
                return Ok(updatedTournament);
            }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
            catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError($"Error cambiando estado del torneo: {ex.Message}");
                return StatusCode(500, new { message = "Error interno del servidor." });
            }
        }
    }
}
