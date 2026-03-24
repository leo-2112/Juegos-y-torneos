using Juegos.API.DTOs;
using Juegos.API.Models;
using Google.Cloud.Firestore;

namespace Juegos.API.Services
{
    public class TournamentService : ITournamentService
    {
        private readonly FirebaseService _firebaseService;
        private readonly ILogger<TournamentService> _logger;

        public TournamentService(FirebaseService firebaseService, ILogger<TournamentService> logger)
        {
            _firebaseService = firebaseService;
            _logger = logger;
        }

        public async Task<Tournament> CreateTournament(CreateTournamentDto createDto, string organizerId, string role)
        {
            if (role != "admin" && role != "organizador")
                throw new UnauthorizedAccessException("Solo administradores u organizadores pueden crear torneos.");

            var gamesCol = _firebaseService.GetCollection("juegos");
            var gameDoc = await gamesCol.Document(createDto.Game).GetSnapshotAsync();
            if (!gameDoc.Exists)
                throw new ArgumentException("El juego especificado no existe.");

            if (createDto.StartDate <= DateTime.UtcNow)
                throw new ArgumentException("La fecha de inicio debe ser posterior a hoy.");

            if (createDto.SignupDueDate >= createDto.StartDate)
                throw new ArgumentException("La fecha de inscripción debe ser previa a la fecha de inicio.");

            if (createDto.MaxParticipants <= 2)
                throw new ArgumentException("La cantidad máxima de participantes debe ser mayor a 2.");

            var validFormats = new HashSet<string> { "individual", "equipos", "royale" };
            if (!validFormats.Contains(createDto.TournamentFormat))
                throw new ArgumentException("Formato de torneo inválido.");

            var collection = _firebaseService.GetCollection("torneos");
            var tournament = new Tournament
            {
                Id = Guid.NewGuid().ToString(),
                TournamentName = createDto.TournamentName,
                Game = createDto.Game,
                Organizer = organizerId,
                Description = createDto.Description,
                TournamentStatus = "próximo",
                TournamentFormat = createDto.TournamentFormat,
                MaxParticipants = createDto.MaxParticipants,
                CurrentParticipants = 0,
                SignupFee = createDto.SignupFee,
                TotalPrize = createDto.TotalPrize,
                StartDate = createDto.StartDate,
                EndDate = createDto.EndDate,
                SignupDueDate = createDto.SignupDueDate,
                MinLevel = createDto.MinLevel,
                MaxLevel = createDto.MaxLevel,
                IsTeamRequired = createDto.IsTeamRequired,
                TeamSize = createDto.TeamSize,
                CreationDate = DateTime.UtcNow,
                ModifiedRules = false
            };

            var dict = new Dictionary<string, object>
            {
                { "Id", tournament.Id },
                { "TournamentName", tournament.TournamentName },
                { "Game", tournament.Game },
                { "Organizer", tournament.Organizer },
                { "Description", tournament.Description },
                { "TournamentStatus", tournament.TournamentStatus },
                { "TournamentFormat", tournament.TournamentFormat },
                { "MaxParticipants", tournament.MaxParticipants },
                { "CurrentParticipants", tournament.CurrentParticipants },
                { "SignupFee", tournament.SignupFee },
                { "TotalPrize", tournament.TotalPrize },
                { "StartDate", tournament.StartDate },
                { "EndDate", tournament.EndDate },
                { "SignupDueDate", tournament.SignupDueDate },
                { "MinLevel", tournament.MinLevel },
                { "MaxLevel", tournament.MaxLevel },
                { "IsTeamRequired", tournament.IsTeamRequired },
                { "TeamSize", tournament.TeamSize },
                { "CreationDate", tournament.CreationDate },
                { "ModifiedRules", tournament.ModifiedRules }
            };

            await collection.Document(tournament.Id).SetAsync(dict);
            return tournament;
        }

        public async Task<IEnumerable<Tournament>> GetTournaments(string? gameId, string? status, int? minFee, int? maxFee, int? minLevel, int? maxLevel, int page, int pageSize)
        {
            var collection = _firebaseService.GetCollection("torneos");
            Query query = collection;

            if (!string.IsNullOrEmpty(status))
                query = query.WhereEqualTo("TournamentStatus", status);
            else
                query = query.WhereIn("TournamentStatus", new[] { "próximo", "en progreso" });

            if (!string.IsNullOrEmpty(gameId))
                query = query.WhereEqualTo("Game", gameId);

            var snapshot = await query.GetSnapshotAsync();
            var tournaments = new List<Tournament>();

            foreach (var doc in snapshot.Documents)
            {
                var dict = doc.ToDictionary();
                tournaments.Add(MapToTournament(dict));
            }

            var filtered = tournaments.AsQueryable();

            if (minFee.HasValue) filtered = filtered.Where(t => t.SignupFee >= minFee.Value);
            if (maxFee.HasValue) filtered = filtered.Where(t => t.SignupFee <= maxFee.Value);
            if (minLevel.HasValue) filtered = filtered.Where(t => t.MinLevel >= minLevel.Value);
            if (maxLevel.HasValue) filtered = filtered.Where(t => t.MaxLevel <= maxLevel.Value);

            var result = filtered.OrderBy(t => t.StartDate)
                                 .Skip((page - 1) * pageSize)
                                 .Take(pageSize)
                                 .ToList();

            return result;
        }

        public async Task<Tournament> UpdateTournament(string id, UpdateTournamentDto updateDto, string userId, string role)
        {
            var collection = _firebaseService.GetCollection("torneos");
            var docRef = collection.Document(id);
            var snapshot = await docRef.GetSnapshotAsync();

            if (!snapshot.Exists) throw new InvalidOperationException("Torneo no encontrado.");

            var dict = snapshot.ToDictionary();
            var tournament = MapToTournament(dict);

            if (tournament.Organizer != userId && role != "admin")
                throw new UnauthorizedAccessException("No tienes permisos para actualizar este torneo.");

            if (tournament.TournamentStatus != "próximo")
                throw new InvalidOperationException("Solo se pueden actualizar torneos próximos a iniciar.");

            if (updateDto.MaxParticipants < tournament.CurrentParticipants)
                throw new ArgumentException("No puedes reducir la cantidad máxima por debajo de los participantes actuales.");

            var updates = new Dictionary<string, object>
            {
                { "TournamentName", updateDto.TournamentName },
                { "Description", updateDto.Description },
                { "MaxParticipants", updateDto.MaxParticipants },
                { "SignupFee", updateDto.SignupFee },
                { "StartDate", updateDto.StartDate },
                { "SignupDueDate", updateDto.SignupDueDate },
                { "MinLevel", updateDto.MinLevel },
                { "MaxLevel", updateDto.MaxLevel },
                { "ModifiedRules", true }
            };

            await docRef.UpdateAsync(updates);

            var updatedSnapshot = await docRef.GetSnapshotAsync();
            return MapToTournament(updatedSnapshot.ToDictionary());
        }

        public async Task<bool> CancelTournament(string id, string userId, string role)
        {
            var collection = _firebaseService.GetCollection("torneos");
            var docRef = collection.Document(id);
            var snapshot = await docRef.GetSnapshotAsync();

            if (!snapshot.Exists) throw new InvalidOperationException("Torneo no encontrado.");

            var tournament = MapToTournament(snapshot.ToDictionary());

            if (tournament.Organizer != userId && role != "admin")
                throw new UnauthorizedAccessException("No tienes permisos para cancelar este torneo.");

            if (tournament.TournamentStatus != "próximo")
                throw new InvalidOperationException("Solo se pueden cancelar torneos próximos a iniciar.");

            await docRef.UpdateAsync(new Dictionary<string, object> { { "TournamentStatus", "cancelado" } });
            return true;
        }

        public async Task<Tournament> ChangeStatus(string id, ChangeStatusDto statusDto, string userId, string role)
        {
            var collection = _firebaseService.GetCollection("torneos");
            var docRef = collection.Document(id);
            var snapshot = await docRef.GetSnapshotAsync();

            if (!snapshot.Exists) throw new InvalidOperationException("Torneo no encontrado.");

            var tournament = MapToTournament(snapshot.ToDictionary());

            if (tournament.Organizer != userId && role != "admin")
                throw new UnauthorizedAccessException("No tienes permisos para cambiar el estado de este torneo.");

            var current = tournament.TournamentStatus;
            var target = statusDto.TournamentStatus;

            if (current == "próximo" && target != "en progreso")
                throw new InvalidOperationException("Un torneo próximo solo puede cambiar a 'en progreso'.");
            
            if (current == "en progreso" && target != "finalizado")
                throw new InvalidOperationException("Un torneo en progreso solo puede cambiar a 'finalizado'.");

            if (current == "finalizado" || current == "cancelado")
                throw new InvalidOperationException("El torneo ya está finalizado o cancelado.");

            await docRef.UpdateAsync(new Dictionary<string, object> { { "TournamentStatus", target } });

            var updatedSnapshot = await docRef.GetSnapshotAsync();
            return MapToTournament(updatedSnapshot.ToDictionary());
        }

        private Tournament MapToTournament(Dictionary<string, object> dict)
        {
            return new Tournament
            {
                Id = dict["Id"].ToString(),
                TournamentName = dict["TournamentName"].ToString(),
                Game = dict["Game"].ToString(),
                Organizer = dict["Organizer"].ToString(),
                Description = dict["Description"].ToString(),
                TournamentStatus = dict["TournamentStatus"].ToString(),
                TournamentFormat = dict["TournamentFormat"].ToString(),
                MaxParticipants = (int)(long)dict["MaxParticipants"],
                CurrentParticipants = (int)(long)dict["CurrentParticipants"],
                SignupFee = (int)(long)dict["SignupFee"],
                TotalPrize = (int)(long)dict["TotalPrize"],
                StartDate = ((Timestamp)dict["StartDate"]).ToDateTime(),
                EndDate = ((Timestamp)dict["EndDate"]).ToDateTime(),
                SignupDueDate = ((Timestamp)dict["SignupDueDate"]).ToDateTime(),
                MinLevel = (int)(long)dict["MinLevel"],
                MaxLevel = (int)(long)dict["MaxLevel"],
                IsTeamRequired = (bool)dict["IsTeamRequired"],
                TeamSize = (int)(long)dict["TeamSize"],
                CreationDate = ((Timestamp)dict["CreationDate"]).ToDateTime(),
                ModifiedRules = (bool)dict["ModifiedRules"]
            };
        }
    }
}
