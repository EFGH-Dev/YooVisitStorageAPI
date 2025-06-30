using Microsoft.AspNetCore.Mvc;
using YooVisitStorageAPI.PhotoInterface; // Assure-toi que le namespace est correct

namespace YooVisitStorageAPI.Controllers // Assure-toi que le namespace est correct
{
    [ApiController]
    [Route("api/[controller]")]
    public class PhotosController : ControllerBase
    {
        private readonly IFileStorageService _storageService;

        public PhotosController(IFileStorageService storageService)
        {
            _storageService = storageService;
        }

        [HttpPost("upload")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]

        // --- CORRECTION CLÉ ---
        // On ajoute ces attributs pour autoriser les fichiers volumineux (ex: 100 Mo).
        // C'est crucial pour que le serveur Kestrel n'interrompe pas la connexion.
        [RequestFormLimits(MultipartBodyLengthLimit = 100_000_000)]
        [RequestSizeLimit(100_000_000)]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("Aucun fichier n'a été fourni.");
            }

            try
            {
                var savedFileName = await _storageService.SaveFileAsync(file, "uploads");
                return CreatedAtAction(nameof(UploadImage), new { fileName = savedFileName });
            }
            catch (ArgumentException ex)
            {
                // Erreur de validation (ex: mauvais type de fichier)
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                // Pour le débogage, on affiche l'erreur dans la console de Docker
                Console.WriteLine($"[ERREUR INTERNE] {ex.Message}");
                return StatusCode(500, "Une erreur interne est survenue sur le serveur.");
            }
        }
    }
}