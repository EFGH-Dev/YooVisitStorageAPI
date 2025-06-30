namespace YooVisitStorageAPI.PhotoServices
{
    using YooVisitStorageAPI.PhotoInterface;

    public class FileStorageService : IFileStorageService
    {
        // Le chemin de base DANS LE CONTENEUR, qui sera mappé par un volume Docker.
        private readonly string _storagePath = "/app/storage";

        public async Task<string> SaveFileAsync(IFormFile file, string subDirectory)
        {
            var allowedContentTypes = new[] { "image/jpeg", "image/jpg", "image/png" };
            if (!allowedContentTypes.Contains(file.ContentType))
            {
                Console.WriteLine($"[VALIDATION ÉCHOUÉE] Type de fichier reçu: {file.ContentType}");
                throw new ArgumentException("Type de fichier non autorisé. Uniquement JPEG, JPG et PNG.");
            }

            var targetDirectory = Path.Combine(_storagePath, subDirectory);
            Directory.CreateDirectory(targetDirectory);

            var fileExtension = Path.GetExtension(file.FileName);
            var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
            var fullPath = Path.Combine(targetDirectory, uniqueFileName);

            await using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // On affiche un log pour confirmer la sauvegarde dans la console Docker
            Console.WriteLine($"Fichier sauvegardé avec succès : {fullPath}");

            return uniqueFileName;
        }
    }
}
