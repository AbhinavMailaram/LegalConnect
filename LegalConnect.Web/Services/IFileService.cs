using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace LegalConnect.Web.Services
{
    public interface IFileService
    {
        Task<string> SaveFileAsync(IFormFile file, string subFolder);
        bool DeleteFile(string relativePath);
        bool IsValidFileType(IFormFile file, string[] allowedExtensions);
        bool IsValidFileSize(IFormFile file, long maxBytes);
    }
}
