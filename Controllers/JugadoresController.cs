using Juegos.API.DTOs;
using Juegos.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Juegos.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class JugadoresController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<JugadoresController> _logger;

        public JugadoresController(IAuthService authService, ILogger<JugadoresController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        /// <summary>
        /// PUT /api/jugadores/{id}/perfil
        /// 
        /// Endpoint para actualizar el perfil de un jugador.
        /// Solo puede ser modificado por el propio jugador o por un administrador.
        /// No permite modificar correo ni nombre de usuario.
        /// </summary>
        [HttpPut("{id}/perfil")]
        public async Task<IActionResult> UpdateProfile(string id, [FromBody] UpdateProfileDto updateDto)
        {
            try
            {
                // Obtenemos el ID y Role del token JWT
                var userId = User.FindFirstValue("sub");
                var userRole = User.FindFirstValue("role");

                // Validamos autorización (dueño de la cuenta o admin)
                if (userId != id && userRole != "admin")
                {
                    return StatusCode(403, new { message = "No tienes permisos para modificar este perfil." });
                }

                if (updateDto == null)
                {
                    return BadRequest(new { message = "El cuerpo de la peticion no puede estar cacio" });
                }

                var updatedPlayer = await _authService.UpdateProfile(id, updateDto);

                _logger.LogInformation($"Perfil de jugador actualizado: {updatedPlayer.Email}");

                // Devolvemos el DTO actualizado
                var responseDto = new PlayerDto
                {
                    Id = updatedPlayer.Id,
                    FirstName = updatedPlayer.FirstName,
                    LastName = updatedPlayer.LastName,
                    Email = updatedPlayer.Email,
                    Username = updatedPlayer.Username,
                    Age = updatedPlayer.Age,
                    Country = updatedPlayer.Country,
                    Role = updatedPlayer.Role,
                    TotalPoints = updatedPlayer.TotalPoints,
                    WonTournaments = updatedPlayer.WonTournaments
                };

                return Ok(responseDto);
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
                _logger.LogError($"Error en UpdateProfile: {ex.Message}");
                return StatusCode(500, new { message = "Error del servidor al actualizar el perfil" });
            }
        }
    }
}
