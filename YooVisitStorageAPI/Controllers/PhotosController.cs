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
        [RequestFormLimits(MultipartBodyLengthLimit = 100_000_000)] // 100 Mo
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
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERREUR INTERNE] {ex.Message}");
                return StatusCode(500, "Une erreur interne est survenue sur le serveur.");
            }
        }
    }
}