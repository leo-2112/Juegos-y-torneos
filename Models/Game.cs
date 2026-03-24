namespace Juegos.API.Models;

public class Game
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Developer { get; set; } = string.Empty;
    public string Genre { get; set; } = string.Empty;
    public List<string> Platform { get; set; } = new List<string>();
    public DateTime LaunchDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public int ActivePlayers { get; set; }
    public int ActiveTournaments { get; set; }
    public string CurrentStatus { get; set; } = string.Empty;
    public double AverageRating { get; set; }
    public DateTime AddedOn { get; set; }
}
