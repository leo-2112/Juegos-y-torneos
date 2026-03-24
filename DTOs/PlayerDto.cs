namespace Juegos.API.DTOs;

public class PlayerDto
{
    /**
     * Id para identificar el jugador
     */
    public string Id { get; set; } = string.Empty;

    /**
     * FirstName y LastName nombre visible del jugador
     */
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    /**
     * Solo si es necesario
     */
    public string Email { get; set; } = string.Empty;

    // Usuario del jugador
    public string Username { get; set; } = string.Empty;

    // Edad del jugador
    public int Age { get; set; }

    // Pais del jugador
    public string Country { get; set; } = string.Empty;

    /**
     * Si es un admin o un regular user (mostrar que acceso tiene)
     */
    public string Role { get; set; } = "player";

    /**
     * TotalPoints cuantos puntos ha ganado en total
     */
    public int TotalPoints { get; set; }

    // WonTournaments cuantos torneos ha ganado
    public int WonTournaments { get; set; }
}

/// <summary>
/// RegisterDto es lo que recibira el backend cuando alguien se registra
/// </summary>
public class RegisterDto
{
    /**
     * Email correo para la cuenta
     */
    public string Email { get; set; } = string.Empty;

    /**
     * Password es la clave / contraseña (esto va encriptado por HTTPS)
     */

    public string Password { get; set; } = string.Empty;

    /**
     * FirstName y LastName nombre y apellido que aparecera en el perfil
     */
    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    public string Country { get; set; } = string.Empty;
}

public class LoginDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
/// <summary>
/// AuthResponse es la informacion que recibimos de nuestra peticion desde el FE
/// Contiene el token que el FE recibe para futuras peticiones
/// </summary>
public class AuthResponseDto
{
    /**
     * Bool para saber el login fue exitoso o no
     */
    public bool Success { get; set; }

    /**
     * Mensaje de exito o error
     */
    public string Message { get; set; } = string.Empty;

    /**
     * Token es manipulado / administrado por el JWT guarda y envia en cada peticion
     */
    public string Token { get; set; } = string.Empty;

    /**
     * User va extraer la informacion del usuario autenticado
     */
    public PlayerDto Player { get; set; } = new PlayerDto();
}
