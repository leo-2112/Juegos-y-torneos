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
    public class JuegosController : ControllerBase
    {
        private readonly IJuegosService _juegosService;
        private readonly ILogger<JuegosController> _logger;

        public JuegosController(IJuegosService juegosService, ILogger<JuegosController> logger)
        {
            _juegosService = juegosService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> CreateGame([FromBody] CreateGameDto createDto)
        {
            try
            {
                var userRole = User.FindFirstValue("role");
                if (userRole != "admin") 
                {
                    return StatusCode(403, new { message = "Solo administradores pueden crear juegos." });
                }

                var game = await _juegosService.CreateGame(createDto);
                return StatusCode(201, game);
            }
            catch (ArgumentException ex) 
            { 
                return BadRequest(new { message = ex.Message }); 
            }
            catch (InvalidOperationException ex) 
            { 
                return BadRequest(new { message = ex.Message }); 
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creando juego: {ex.Message}");
                return StatusCode(500, new { message = "Error interno del servidor. " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetGames([FromQuery] string? genre, [FromQuery] string? platform, [FromQuery] string? developer)
        {
            try
            {
                var games = await _juegosService.GetGames(genre, platform, developer);
                return Ok(games);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error obteniendo juegos: {ex.Message}");
                return StatusCode(500, new { message = "Error interno del servidor al consultar juegos." });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateGame(string id, [FromBody] UpdateGameDto updateDto)
        {
            try
            {
                var userRole = User.FindFirstValue("role");
                if (userRole != "admin") 
                {
                    return StatusCode(403, new { message = "Solo administradores pueden modificar juegos." });
                }

                var updatedGame = await _juegosService.UpdateGame(id, updateDto);
                return Ok(updatedGame);
            }
            catch (ArgumentException ex) 
            { 
                return BadRequest(new { message = ex.Message }); 
            }
            catch (InvalidOperationException ex) 
            { 
                return NotFound(new { message = ex.Message }); 
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error actualizando juego: {ex.Message}");
                return StatusCode(500, new { message = "Error interno del servidor. " + ex.Message });
            }
        }

        [HttpGet("{id}/estadisticas")]
        public async Task<IActionResult> GetGameStats(string id)
        {
            try
            {
                var stats = await _juegosService.GetGameStats(id);
                return Ok(stats);
            }
            catch (InvalidOperationException ex) 
            { 
                return NotFound(new { message = ex.Message }); 
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error obteniendo estadísticas: {ex.Message}");
                return StatusCode(500, new { message = "Error interno del servidor." });
            }
        }
    }
}
