using Juegos.API.Models;
using Juegos.API.DTOs;

namespace Juegos.API.Services
{
    public interface IJuegosService
    {
        Task<Game> CreateGame(CreateGameDto createDto);
        Task<IEnumerable<Game>> GetGames(string? genre, string? platform, string? developer);
        Task<Game> UpdateGame(string id, UpdateGameDto updateDto);
        Task<GameStatsDto> GetGameStats(string id);
    }
}
