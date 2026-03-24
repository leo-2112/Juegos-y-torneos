using Juegos.API.DTOs;
using Juegos.API.Models;
using Google.Cloud.Firestore;
using System.Security.Cryptography;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using BCrypt.Net;

namespace Juegos.API.Services;

/// <summary>
/// AuthService implementa la autenticación de usuarios
/// Gestiona registro, login y generación de tokens JWT
/// </summary>
public class AuthService : IAuthService
{
    private readonly FirebaseService _firebaseService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;

    /// <summary>
    /// Constructor: Recibe las dependencias inyectadas
    /// </summary>
    public AuthService(
        FirebaseService firebaseService,
        IConfiguration configuration,
        ILogger<AuthService> logger)
    {
        _firebaseService = firebaseService;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Register: Crea un nuevo jugador en la aplicación
    /// </summary>
    public async Task<Player> Register(RegisterDto registerDto)
    {
        try
        {
            // Validar que el DTO no es nulo
            if (registerDto == null)
            {
                throw new ArgumentException("El cuerpo de la petición es requerido");
            }

            // Validar que email y password no están vacíos
            if (string.IsNullOrWhiteSpace(registerDto.Email) ||
                string.IsNullOrWhiteSpace(registerDto.Password))
            {
                throw new ArgumentException("Email y contraseña son requeridos");
            }

            // Validar que la contraseña tenga longitud mínima
            if (registerDto.Password.Length < 6)
            {
                throw new ArgumentException("La contraseña debe tener al menos 6 caracteres");
            }

            // Obtener la colección de usuarios desde Firestore
            var playersCollection = _firebaseService.GetCollection("players");

            if (playersCollection == null)
            {
                throw new InvalidOperationException("No se pudo obtener la colección de usuarios");
            }

            // Verificar que el email no está registrado
            var query = await playersCollection
                .WhereEqualTo("Email", registerDto.Email)
                .GetSnapshotAsync();

            if (query.Count > 0)
            {
                throw new InvalidOperationException("El email ya está registrado");
            }

            // Hashear la contraseña con BCrypt
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password);

            // Crear nuevo usuario
            var newPlayer = new Player
            {
                Id = Guid.NewGuid().ToString(),
                FirstName = registerDto.FirstName,
                LastName = registerDto.LastName,
                Email = registerDto.Email,
                Username = registerDto.Username,
                Age = 0,
                Country = registerDto.Country,
                Role = "player",
                IsActive = true,
                TotalPoints = 0,
                WonTournaments = 0,
                CreatedAt = DateTime.UtcNow,
                IsOnline = true,
                LastLogin = DateTime.UtcNow,
                
            };

            // Guardar el jugador en Firestore usando Dictionary
            var userData = new Dictionary<string, object>
            {
                { "Id", newPlayer.Id },
                { "FirstName", newPlayer.FirstName},
                { "LastName", newPlayer.LastName},
                { "Email", newPlayer.Email },
                { "PasswordHash", passwordHash },  // Guardar hash, NO la contraseña
                { "Username", newPlayer.Username},
                { "Age", newPlayer.Age},
                { "Country", newPlayer.Country},
                { "Role", newPlayer.Role },
                { "IsActive", newPlayer.IsActive },
                { "TotalPoints", newPlayer.TotalPoints},
                { "WonTournaments", newPlayer.WonTournaments},
                { "CreatedAt", newPlayer.CreatedAt},
                { "IsOnline", newPlayer.IsOnline},
                { "LastLogin", newPlayer.LastLogin},
                

            };

            await playersCollection.Document(newPlayer.Id).SetAsync(userData);

            return newPlayer;
        }
        catch (ArgumentException ex)
        {
            _logger.LogError($"Error de validación en Register: {ex.Message}");
            throw;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError($"Error lógico en Register: {ex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error inesperado en Register: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Login: Autentica un jugador y devuelve un token JWT
    /// </summary>
    public async Task<(Player player, string token)> Login(LoginDto loginDto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(loginDto.Email) ||
                string.IsNullOrWhiteSpace(loginDto.Password))
            {
                throw new ArgumentException("Email y contraseña son requeridos");
            }

            var playersCollection = _firebaseService.GetCollection("players");

            if (playersCollection == null)
            {
                throw new InvalidOperationException("No se pudo obtener la colección de jugadores");
            }

            var query = await playersCollection
                .WhereEqualTo("Email", loginDto.Email)
                .GetSnapshotAsync();

            if (query.Count == 0)
            {
                throw new InvalidOperationException("Email o contraseña incorrectos");
            }

            var playerDoc = query.Documents[0];
            var playerDict = playerDoc.ToDictionary();

            // Obtener el hash de contraseña guardado
            var passwordHash = playerDict["PasswordHash"].ToString();

            // Validar la contraseña contra el hash con BCrypt
            if (!BCrypt.Net.BCrypt.Verify(loginDto.Password, passwordHash))
            {
                throw new InvalidOperationException("Email o contraseña incorrectos");
            }

            // Convertir el diccionario a objeto Player
            var player = new Player
            {
                Id = playerDict["Id"].ToString(),
                FirstName = playerDict["FirstName"].ToString(),
                LastName = playerDict["LastName"].ToString(),
                Email = playerDict["Email"].ToString(),
                Username = playerDict["Username"].ToString(),
                Age = (int)(long)playerDict["Age"],
                Country = playerDict["Country"].ToString(),
                Role = playerDict["Role"].ToString(),
                IsActive = (bool)playerDict["IsActive"],
                TotalPoints = (int)(long)playerDict["TotalPoints"],
                WonTournaments = (int)(long)playerDict["WonTournaments"],
                CreatedAt = ((Timestamp)playerDict["CreatedAt"]).ToDateTime(),
                IsOnline = (bool)playerDict["IsOnline"],
                LastLogin = ((Timestamp)playerDict["LastLogin"]).ToDateTime()
            };

            var token = GenerateJwtToken(player);

            // Actualizar LastLogin
            await playersCollection.Document(player.Id).UpdateAsync(
                new Dictionary<string, object>
                {
                    { "LastLogin", DateTime.UtcNow }
                }
            );

            return (player, token);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error en Login: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// ValidateToken: Verifica si un token JWT es válido
    /// </summary>
    public async Task<bool> ValidateToken(string token)
    {
        try
        {
            var secretKey = _configuration["Jwt:SecretKey"];
            if (string.IsNullOrEmpty(secretKey))
            {
                return false;
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(secretKey);

            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error validando token: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// GetPlayerById: Obtiene un jugador por su ID
    /// </summary>
    public async Task<Player?> GetPlayerById(string playerId)
    {
        try
        {
            var playersCollection = _firebaseService.GetCollection("players");
            var doc = await playersCollection.Document(playerId).GetSnapshotAsync();

            if (!doc.Exists)
            {
                return null;
            }

            var playerDict = doc.ToDictionary();

            var player = new Player
            {
                Id = playerDict["Id"].ToString(),
                FirstName = playerDict["FirstName"].ToString(),
                LastName = playerDict["LastName"].ToString(),
                Email = playerDict["Email"].ToString(),
                Username = playerDict["Username"].ToString(),
                Age = (int)(long)playerDict["Age"],
                Country = playerDict["Country"].ToString(),
                Role = playerDict["Role"].ToString(),
                IsActive = (bool)playerDict["IsActive"],
                TotalPoints = (int)(long)playerDict["TotalPoints"],
                WonTournaments = (int)(long)playerDict["WonTournaments"],
                CreatedAt = ((Timestamp)playerDict["CreatedAt"]).ToDateTime(),
                IsOnline = (bool)playerDict["IsOnline"],
                LastLogin = ((Timestamp)playerDict["LastLogin"]).ToDateTime()
            };

            return player;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error al obtener jugador: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// GenerateJwtToken: Crea un token JWT para un usuario
    /// </summary>
    public string GenerateJwtToken(Player player)
    {
        try
        {
            var secretKey = _configuration["Jwt:SecretKey"];
            var issuer = _configuration["Jwt:Issuer"];
            var audience = _configuration["Jwt:Audience"];

            if (string.IsNullOrEmpty(secretKey))
            {
                throw new InvalidOperationException("JWT SecretKey no configurado");
            }

            var key = Encoding.ASCII.GetBytes(secretKey);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim("sub", player.Id),
                    new Claim("email", player.Email),
                    new Claim("firstname", player.FirstName),
                    new Claim("role", player.Role)
                }),
                Expires = DateTime.UtcNow.AddHours(24),
                Issuer = issuer,
                Audience = audience,
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error al generar token: {ex.Message}");
            throw;
        }
    }
}
