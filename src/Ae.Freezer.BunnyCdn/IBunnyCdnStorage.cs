using BunnyCDN.Net.Storage.Models;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Ae.Freezer.BunnyCdn
{
    /// <summary>
    /// Interface for BunnyCDN storage operations, allowing for mocking in unit tests.
    /// </summary>
    public interface IBunnyCdnStorage
    {
        /// <summary>
        /// Upload a stream to BunnyCDN storage.
        /// </summary>
        Task UploadAsync(Stream stream, string path);

        /// <summary>
        /// Get storage objects from a path.
        /// </summary>
        Task<List<StorageObject>> GetStorageObjectsAsync(string path);

        /// <summary>
        /// Delete an object from storage.
        /// </summary>
        Task<bool> DeleteObjectAsync(string path);
    }
}
