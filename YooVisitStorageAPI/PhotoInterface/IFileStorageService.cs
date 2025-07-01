namespace YooVisitStorageAPI.PhotoInterface
{
    public interface IFileStorageService
    {
        Task<string> SaveFileAsync(IFormFile file, string subDirectory);
    }
}
