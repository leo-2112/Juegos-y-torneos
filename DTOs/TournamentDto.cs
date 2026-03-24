namespace Juegos.API.DTOs;

public class TournamentDto
{
    public string Id { get; set; } = string.Empty;
    public string TournamentName { get; set; } = string.Empty;
    public string Game { get; set; } = string.Empty;
    public string Organizer { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string TournamentStatus { get; set; } = string.Empty;
    public string TournamentFormat { get; set; } = string.Empty;
    public int MaxParticipants { get; set; }
    public int CurrentParticipants { get; set; }
    public int SignupFee { get; set; }
    public int TotalPrize { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime SignupDueDate { get; set; }
    public int MinLevel { get; set; }
    public int MaxLevel { get; set; }
    public bool IsTeamRequired { get; set; }
    public int TeamSize { get; set; }
    public DateTime CreationDate { get; set; }
    public bool ModifiedRules { get; set; }
}

public class CreateTournamentDto
{
    public string TournamentName { get; set; } = string.Empty;
    public string Game { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string TournamentFormat { get; set; } = string.Empty;
    public int MaxParticipants { get; set; }
    public int SignupFee { get; set; }
    public int TotalPrize { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime SignupDueDate { get; set; }
    public int MinLevel { get; set; }
    public int MaxLevel { get; set; }
    public bool IsTeamRequired { get; set; }
    public int TeamSize { get; set; }
}

public class UpdateTournamentDto
{
    public string TournamentName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int MaxParticipants { get; set; }
    public int SignupFee { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime SignupDueDate { get; set; }
    public int MinLevel { get; set; }
    public int MaxLevel { get; set; }
}

public class ChangeStatusDto
{
    public string TournamentStatus { get; set; } = string.Empty;
}
