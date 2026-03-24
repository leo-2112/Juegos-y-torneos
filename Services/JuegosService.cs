using Juegos.API.DTOs;
using Juegos.API.Models;
using Google.Cloud.Firestore;

namespace Juegos.API.Services
{
    public class JuegosService : IJuegosService
    {
        private readonly FirebaseService _firebaseService;
        private readonly ILogger<JuegosService> _logger;

        public JuegosService(FirebaseService firebaseService, ILogger<JuegosService> logger)
        {
            _firebaseService = firebaseService;
            _logger = logger;
        }

        public async Task<Game> CreateGame(CreateGameDto createDto)
        {
            if (createDto == null) throw new ArgumentNullException(nameof(createDto));
            if (string.IsNullOrWhiteSpace(createDto.Title)) throw new ArgumentException("El título es requerido");
            if (string.IsNullOrWhiteSpace(createDto.Description) || createDto.Description.Length < 20)
                throw new ArgumentException("La descripción debe tener al menos 20 caracteres");
            
            var validPlatforms = new HashSet<string> { "PC", "PS5", "Xbox", "Switch" };
            if (createDto.Platform == null || !createDto.Platform.Any())
                throw new ArgumentException("Debe especificar al menos una plataforma");
            
            foreach (var p in createDto.Platform)
            {
                if (!validPlatforms.Contains(p))
                    throw new ArgumentException($"Plataforma inválida: {p}");
            }

            var collection = _firebaseService.GetCollection("juegos");
            
            var query = await collection.WhereEqualTo("Title", createDto.Title).GetSnapshotAsync();
            if (query.Count > 0) throw new InvalidOperationException("El título ya existe");

            var game = new Game
            {
                Id = Guid.NewGuid().ToString(),
                Title = createDto.Title,
                Developer = createDto.Developer,
                Genre = createDto.Genre,
                Platform = createDto.Platform,
                LaunchDate = createDto.LaunchDate,
                Description = createDto.Description,
                ActivePlayers = 0,
                ActiveTournaments = 0,
                CurrentStatus = "disponible",
                AverageRating = 0,
                AddedOn = DateTime.UtcNow
            };

            var gameDict = new Dictionary<string, object>
            {
                { "Id", game.Id },
                { "Title", game.Title },
                { "Developer", game.Developer },
                { "Genre", game.Genre },
                { "Platform", game.Platform },
                { "LaunchDate", game.LaunchDate },
                { "Description", game.Description },
                { "ActivePlayers", game.ActivePlayers },
                { "ActiveTournaments", game.ActiveTournaments },
                { "CurrentStatus", game.CurrentStatus },
                { "AverageRating", game.AverageRating },
                { "AddedOn", game.AddedOn }
            };

            await collection.Document(game.Id).SetAsync(gameDict);
            return game;
        }

        public async Task<IEnumerable<Game>> GetGames(string? genre, string? platform, string? developer)
        {
            var collection = _firebaseService.GetCollection("juegos");
            var query = collection.WhereEqualTo("CurrentStatus", "disponible");

            if (!string.IsNullOrEmpty(genre))
                query = query.WhereEqualTo("Genre", genre);
            if (!string.IsNullOrEmpty(developer))
                query = query.WhereEqualTo("Developer", developer);
            
            if (!string.IsNullOrEmpty(platform))
                query = query.WhereArrayContains("Platform", platform);

            var snapshot = await query.GetSnapshotAsync();
            var games = new List<Game>();

            foreach (var doc in snapshot.Documents)
            {
                var dict = doc.ToDictionary();
                games.Add(MapToGame(dict));
            }
            return games;
        }

        public async Task<Game> UpdateGame(string id, UpdateGameDto updateDto)
        {
            if (updateDto == null) throw new ArgumentNullException(nameof(updateDto));

            var validStatuses = new HashSet<string> { "disponible", "mantenimiento", "descontinuado" };
            if (!validStatuses.Contains(updateDto.CurrentStatus))
            {
                throw new ArgumentException("Estado inválido. Debe ser 'disponible', 'mantenimiento' o 'descontinuado'");
            }

            var collection = _firebaseService.GetCollection("juegos");
            var docRef = collection.Document(id);
            var snapshot = await docRef.GetSnapshotAsync();

            if (!snapshot.Exists) throw new InvalidOperationException("Juego no encontrado");

            var updates = new Dictionary<string, object>
            {
                { "Description", updateDto.Description },
                { "AverageRating", updateDto.AverageRating },
                { "CurrentStatus", updateDto.CurrentStatus }
            };

            await docRef.UpdateAsync(updates);

            var updatedSnapshot = await docRef.GetSnapshotAsync();
            return MapToGame(updatedSnapshot.ToDictionary());
        }

        public async Task<GameStatsDto> GetGameStats(string id)
        {
            var collection = _firebaseService.GetCollection("juegos");
            var docRef = collection.Document(id);
            var snapshot = await docRef.GetSnapshotAsync();

            if (!snapshot.Exists) throw new InvalidOperationException("Juego no encontrado");

            var dict = snapshot.ToDictionary();
            return new GameStatsDto
            {
                ActivePlayers = dict.ContainsKey("ActivePlayers") ? (int)(long)dict["ActivePlayers"] : 0,
                ActiveTournaments = dict.ContainsKey("ActiveTournaments") ? (int)(long)dict["ActiveTournaments"] : 0,
                AverageRating = dict.ContainsKey("AverageRating") ? Convert.ToDouble(dict["AverageRating"]) : 0
            };
        }

        private Game MapToGame(Dictionary<string, object> dict)
        {
            return new Game
            {
                Id = dict["Id"].ToString(),
                Title = dict["Title"].ToString(),
                Developer = dict["Developer"].ToString(),
                Genre = dict["Genre"].ToString(),
                Platform = ((IEnumerable<object>)dict["Platform"]).Select(x => x.ToString()).ToList(),
                LaunchDate = ((Timestamp)dict["LaunchDate"]).ToDateTime(),
                Description = dict["Description"].ToString(),
                ActivePlayers = (int)(long)dict["ActivePlayers"],
                ActiveTournaments = (int)(long)dict["ActiveTournaments"],
                CurrentStatus = dict["CurrentStatus"].ToString(),
                AverageRating = Convert.ToDouble(dict["AverageRating"]),
                AddedOn = ((Timestamp)dict["AddedOn"]).ToDateTime()
            };
        }
    }
}
