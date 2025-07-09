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
            UserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!)
        };

        _context.Photos.Add(photo);
        await _context.SaveChangesAsync();

        return Ok(new { PhotoId = photo.Id, FileName = newFileName });
    }

    // --- ON AJOUTE LE NOUVEL ENDPOINT ICI ---
    [HttpGet("all-photos")]
    public async Task<IActionResult> GetAllPhotos()
    {
        var photos = await _context.Photos
            .Select(p => new PhotoDto
            {
                Id = p.Id,
                Latitude = p.Latitude,
                Longitude = p.Longitude,
                // On construit une URL publique pour l'image
                ImageUrl = $"{Request.Scheme}://{Request.Host}/storage/{p.FileName}"
            })
            .ToListAsync();

        return Ok(photos);
    }

    // --- L'ANCIEN ENDPOINT RESTE LÀ, IL EST TOUJOURS UTILE ---
    [Authorize]
    [HttpGet("my-photos")]
    public async Task<IActionResult> GetMyPhotos()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var photos = await _context.Photos
            .Where(p => p.UserId == userId)
            .Select(p => new PhotoDto
            {
                Id = p.Id,
                Latitude = p.Latitude,
                Longitude = p.Longitude,
                ImageUrl = $"{Request.Scheme}://{Request.Host}/storage/{p.FileName}"
            })
            .ToListAsync();

        return Ok(photos);
    }
}
