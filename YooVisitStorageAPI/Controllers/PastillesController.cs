using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using YooVisitStorageAPI.Data;
using YooVisitStorageAPI.Dtos;
using YooVisitStorageAPI.Models;

namespace YooVisitStorageAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PastillesController : ControllerBase
{
    private readonly StorageDbContext _context;
    private readonly IWebHostEnvironment _hostingEnvironment;

    public PastillesController(StorageDbContext context, IWebHostEnvironment hostingEnvironment)
    {
        _context = context;
        _hostingEnvironment = hostingEnvironment;
    }

    // --- Endpoint pour CRÉER une nouvelle pastille (avec sa première photo) ---
    [HttpPost]
    public async Task<IActionResult> CreatePastille([FromForm] PastilleCreateDto request)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // On utilise une transaction pour s'assurer que tout est créé correctement, ou rien du tout.
        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // 1. On crée la Pastille (le Point d'Intérêt)
            var pastille = new Pastille
            {
                Id = Guid.NewGuid(),
                Title = request.Title,
                Description = request.Description,
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                Altitude = request.Altitude,
                StyleArchitectural = request.StyleArchitectural,
                PeriodeConstruction = request.PeriodeConstruction,
                HorairesOuverture = request.HorairesOuverture,
                CreatedByUserId = userId,
            };
            _context.Pastilles.Add(pastille);
            await _context.SaveChangesAsync();

            // 2. On sauvegarde la photo associée
            var uploadsFolder = Path.Combine(_hostingEnvironment.ContentRootPath, "storage");
            var fileName = $"{pastille.Id}_{DateTime.UtcNow.Ticks}.jpg";
            var filePath = Path.Combine(uploadsFolder, fileName);

            await using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await request.File.CopyToAsync(fileStream);
            }

            // 3. On crée l'entité Photo et on la lie à la Pastille
            var photo = new Photo
            {
                Id = Guid.NewGuid(),
                FileName = fileName,
                FilePath = filePath,
                UploadedAt = DateTime.UtcNow,
                PastilleId = pastille.Id // On lie la photo à la pastille
            };
            _context.Photos.Add(photo);
            await _context.SaveChangesAsync();

            // Si tout s'est bien passé, on valide la transaction
            await transaction.CommitAsync();

            // On renvoie un DTO complet avec le nom fraîchement récupéré
            var user = await _context.Users.FindAsync(userId);
            var userName = user?.Nom ?? user?.Email.Split('@').First() ?? "Inconnu";

            var resultDto = new PastilleDto { /* Mappe la pastille vers le DTO ici */ };

            return Ok(resultDto);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, $"Erreur interne du serveur : {ex.Message}");
        }
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PastilleDto>>> GetAllPastilles()
    {
        var pastilles = await _context.Pastilles
            .Include(p => p.Photos) // On inclut les photos
            .Include(p => p.Ratings) // On inclut les notes
            .Select(p => new
            {
                Pastille = p,
                // On va chercher le nom dans la table Users associée
                UserName = _context.Users.Where(u => u.IdUtilisateur == p.CreatedByUserId).Select(u => u.Nom).FirstOrDefault() ??
                           _context.Users.Where(u => u.IdUtilisateur == p.CreatedByUserId).Select(u => u.Email).FirstOrDefault()
            })
            .ToListAsync();

        var dtos = pastilles.Select(data => new PastilleDto
        {
            Id = data.Pastille.Id,
            Title = data.Pastille.Title,
            Description = data.Pastille.Description,
            Latitude = data.Pastille.Latitude,
            Longitude = data.Pastille.Longitude,
            CreatedByUserName = data.UserName != null ? data.UserName.Split('@').First() : "Inconnu",
            AverageRating = data.Pastille.Ratings.Any() ? data.Pastille.Ratings.Average(r => r.RatingValue) : 0,
            Photos = data.Pastille.Photos.Select(photo => new PhotoDto { /* Mappe les photos */ }).ToList()
        });

        return Ok(dtos);
    }

    [Authorize]
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdatePastille(Guid id, [FromBody] PastilleUpdateDto updateDto)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var pastille = await _context.Pastilles.FindAsync(id);

        if (pastille == null) return NotFound("Pastille non trouvée.");

        // CONTRÔLE DE SÉCURITÉ : Seul le propriétaire peut modifier.
        if (pastille.CreatedByUserId != userId)
        {
            return Forbid("Vous n'avez pas l'autorisation de modifier cette pastille.");
        }

        pastille.Title = updateDto.Title;
        pastille.Description = updateDto.Description;
        await _context.SaveChangesAsync();

        return NoContent(); // Succès
    }

    [Authorize]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePastille(Guid id)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var pastille = await _context.Pastilles.Include(p => p.Photos).FirstOrDefaultAsync(p => p.Id == id);

        if (pastille == null) return NotFound("Pastille non trouvée.");

        // CONTRÔLE DE SÉCURITÉ : Seul le propriétaire peut supprimer.
        if (pastille.CreatedByUserId != userId)
        {
            return Forbid("Vous n'avez pas l'autorisation de supprimer cette pastille.");
        }

        // On supprime les fichiers physiques associés
        foreach (var photo in pastille.Photos)
        {
            var filePath = Path.Combine(_hostingEnvironment.ContentRootPath, "storage", photo.FileName);
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
        }

        // La suppression de la pastille entraînera la suppression en cascade des photos
        // et des ratings associés si la base de données est bien configurée.
        _context.Pastilles.Remove(pastille);
        await _context.SaveChangesAsync();

        return NoContent(); // Succès
    }

    [Authorize]
    [HttpPost("{id}/rate")]
    public async Task<IActionResult> RatePastille(Guid id, [FromBody] PastilleRatingDto request)
    {
        var raterUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var pastilleToRate = await _context.Pastilles.FindAsync(id);

        if (pastilleToRate == null) return NotFound("Pastille non trouvée.");
        if (pastilleToRate.CreatedByUserId == raterUserId) return BadRequest("Vous ne pouvez pas noter votre propre pastille.");

        var existingRating = await _context.PastilleRatings
            .FirstOrDefaultAsync(r => r.PastilleId == id && r.RaterUserId == raterUserId);

        if (existingRating != null) return Conflict("Vous avez déjà noté cette pastille.");

        _context.PastilleRatings.Add(new PastilleRating
        {
            Id = Guid.NewGuid(),
            PastilleId = id,
            RaterUserId = raterUserId,
            RatingValue = request.Rating,
            RatedAt = DateTime.UtcNow
        });

        var photoOwner = await _context.Users.FindAsync(pastilleToRate.CreatedByUserId);
        if (photoOwner != null)
        {
            photoOwner.Experience += 10; // Chaque note donne 10 XP !
        }

        await _context.SaveChangesAsync();

        return Ok(new { Message = "Merci pour votre vote !", NewExperience = photoOwner?.Experience });
    }

}
