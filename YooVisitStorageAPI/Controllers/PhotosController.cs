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
public class PhotosController : ControllerBase
{
    private readonly StorageDbContext _context;
    private readonly IWebHostEnvironment _hostingEnvironment;

    public PhotosController(StorageDbContext context, IWebHostEnvironment hostingEnvironment)
    {
        _context = context;
        _hostingEnvironment = hostingEnvironment;
    }

    [Authorize]
    [HttpPost("upload")]
    public async Task<IActionResult> Upload([FromForm] PhotoUploadRequest request)
    {
        if (request.File == null || request.File.Length == 0)
        {
            return BadRequest("Aucun fichier n'a été uploadé.");
        }

        var userEmail = User.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrEmpty(userEmail))
        {
            return Unauthorized("Impossible de trouver l'email de l'utilisateur dans le token.");
        }

        var emailPart = userEmail.Split('@').First();
        var latitudeString = request.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var longitudeString = request.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var datePart = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var newFileName = $"{emailPart}-{latitudeString}-{longitudeString}-{datePart}.png";

        var uploadsFolder = Path.Combine(_hostingEnvironment.ContentRootPath, "storage");
        var filePath = Path.Combine(uploadsFolder, newFileName);

        Directory.CreateDirectory(uploadsFolder);

        await using (var fileStream = new FileStream(filePath, FileMode.Create))
        {
            await request.File.CopyToAsync(fileStream);
        }

        var photo = new Photo
        {
            Id = Guid.NewGuid(),
            FileName = newFileName,
            FilePath = filePath,
            UploadedAt = DateTime.UtcNow,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            UserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!),
            Description = request.Description
        };

        _context.Photos.Add(photo);
        await _context.SaveChangesAsync();

        return Ok(new { PhotoId = photo.Id, FileName = newFileName });
    }

    // --- ENDPOINT DE TOUTES LES PHOTOS ---
    [HttpGet("all-photos")]
    public async Task<IActionResult> GetAllPhotos()
    {
        var currentUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var rawData = await (from photo in _context.Photos
                             join user in _context.Users on photo.UserId equals user.IdUtilisateur
                             select new
                             {
                                 PhotoData = photo,
                                 UserEmail = user.Email
                             }).ToListAsync();

        var photosWithUser = rawData.Select(data => new PhotoDto
        {
            Id = data.PhotoData.Id,
            Latitude = data.PhotoData.Latitude,
            Longitude = data.PhotoData.Longitude,
            ImageUrl = $"{Request.Scheme}://{Request.Host}/storage/{data.PhotoData.FileName}",
            IsOwner = data.PhotoData.UserId == currentUserId,
            UserName = data.UserEmail.Split('@').First(),
            Description = data.PhotoData.Description,
            UploadedAt = data.PhotoData.UploadedAt
        }).ToList();

        return Ok(photosWithUser);
    }

    // --- ENDPOINT DE PHOTOS PERSONNELLES ---
    [Authorize]
    [HttpGet("my-photos")]
    public async Task<IActionResult> GetMyPhotos()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var userName = User.FindFirstValue(ClaimTypes.Email)?.Split('@').First();

        var photos = await _context.Photos
            .Where(p => p.UserId == userId)
            .Select(p => new PhotoDto
            {
                Id = p.Id,
                Latitude = p.Latitude,
                Longitude = p.Longitude,
                ImageUrl = $"{Request.Scheme}://{Request.Host}/storage/{p.FileName}",
                IsOwner = true,
                UserName = userName
            })
            .ToListAsync();

        return Ok(photos);
    }

    [Authorize]
    [HttpPost("{photoId}/rate")]
    public async Task<IActionResult> RatePhoto(Guid photoId, [FromBody] RatePhotoRequestDto request)
    {
        // 1. On identifie le joueur qui donne la note (le "rateur")
        var raterUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // 2. On trouve la photo à noter
        var photoToRate = await _context.Photos.FindAsync(photoId);
        if (photoToRate == null)
        {
            return NotFound("Photo non trouvée.");
        }

        // 3. On vérifie que le joueur n'essaie pas de noter sa propre photo
        if (photoToRate.UserId == raterUserId)
        {
            return BadRequest("Vous ne pouvez pas noter votre propre photo.");
        }

        // 4. On vérifie si ce joueur n'a pas déjà noté cette photo
        var existingRating = await _context.PhotoRatings
            .FirstOrDefaultAsync(r => r.PhotoId == photoId && r.RaterUserId == raterUserId);

        if (existingRating != null)
        {
            return Conflict("Vous avez déjà noté cette photo.");
        }

        // 5. Tout est bon, on enregistre la nouvelle note
        var newRating = new PhotoRating
        {
            Id = Guid.NewGuid(),
            PhotoId = photoId,
            RaterUserId = raterUserId,
            RatingValue = request.Rating,
            RatedAt = DateTime.UtcNow
        };
        _context.PhotoRatings.Add(newRating);

        // 6. On trouve le propriétaire de la photo pour lui donner de l'XP
        var photoOwner = await _context.Users.FindAsync(photoToRate.UserId);
        if (photoOwner != null)
        {
            // Chaque note donne 10 points d'XP !
            photoOwner.Experience += 10;
        }

        // 7. On sauvegarde toutes les modifications dans la base de données
        await _context.SaveChangesAsync();

        return Ok(new { Message = "Merci pour votre vote !", NewExperience = photoOwner?.Experience });
    }
}
