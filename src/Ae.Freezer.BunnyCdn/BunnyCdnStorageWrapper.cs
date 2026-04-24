using BunnyCDN.Net.Storage;
using BunnyCDN.Net.Storage.Models;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Ae.Freezer.BunnyCdn
{
    /// <summary>
    /// Wrapper for BunnyCDNStorage that implements IBunnyCdnStorage.
    /// </summary>
    public sealed class BunnyCdnStorageWrapper : IBunnyCdnStorage
    {
        private readonly BunnyCDNStorage _storage;

        /// <summary>
        /// Construct a new wrapper around the given BunnyCDNStorage instance.
        /// </summary>
        public BunnyCdnStorageWrapper(BunnyCDNStorage storage)
        {
            _storage = storage;
        }

        /// <inheritdoc/>
        public Task UploadAsync(Stream stream, string path) => _storage.UploadAsync(stream, path);

        /// <inheritdoc/>
        public Task<List<StorageObject>> GetStorageObjectsAsync(string path) => _storage.GetStorageObjectsAsync(path);

        /// <inheritdoc/>
        public Task<bool> DeleteObjectAsync(string path) => _storage.DeleteObjectAsync(path);
    }
}
