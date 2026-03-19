
using System.IO;
using System.Threading.Tasks;

namespace BazanAI.Media.Storage;

public interface IStorageService
{
    Task UploadAsync(string bucket, string key, Stream data);
    Task<Stream> DownloadAsync(string bucket, string key);
}
