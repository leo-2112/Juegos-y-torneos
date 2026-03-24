using Juegos.API.Models;
using Juegos.API.DTOs;

namespace Juegos.API.Services
{
    public interface ITournamentService
    {
        Task<Tournament> CreateTournament(CreateTournamentDto createDto, string organizerId, string role);
        Task<IEnumerable<Tournament>> GetTournaments(string? gameId, string? status, int? minFee, int? maxFee, int? minLevel, int? maxLevel, int page, int pageSize);
        Task<Tournament> UpdateTournament(string id, UpdateTournamentDto updateDto, string userId, string role);
        Task<bool> CancelTournament(string id, string userId, string role);
        Task<Tournament> ChangeStatus(string id, ChangeStatusDto statusDto, string userId, string role);
    }
}
