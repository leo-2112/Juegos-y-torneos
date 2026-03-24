namespace Juegos.API.Models;

public class Player
{
    public string Id { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    public int Age { get; set; }

    public string Country { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public int TotalPoints { get; set; }

    public int WonTournaments { get; set; }

    public DateTime CreatedAt { get; set; }

    public bool IsOnline { get; set; } = true;

    public DateTime LastLogin { get; set; }

}
