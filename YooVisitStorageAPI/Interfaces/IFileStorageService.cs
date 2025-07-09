using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace YooVisitStorageAPI.Interfaces
{
    public interface IFileStorageService
    {
        Task<string> SaveFileAsync(IFormFile file, string subDirectory);
    }
}
