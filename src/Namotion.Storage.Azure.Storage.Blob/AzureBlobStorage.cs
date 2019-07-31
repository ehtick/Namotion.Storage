﻿using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Namotion.Storage.Abstractions;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Namotion.Storage.Azure.Storage.Blob
{
    public class AzureBlobStorage : IBlobStorage
    {
        private readonly CloudStorageAccount _storageAccount;

        private AzureBlobStorage(CloudStorageAccount storageAccount)
        {
            _storageAccount = storageAccount;
        }

        public static IBlobStorage CreateFromConnectionString(string connectionString)
        {
            return new AzureBlobStorage(CloudStorageAccount.Parse(connectionString));
        }

        public async Task<Abstractions.BlobProperties> GetPropertiesAsync(string path, CancellationToken cancellationToken)
        {
            var blob = await GetBlobReferenceAsync(path, cancellationToken).ConfigureAwait(false);
            await blob.FetchAttributesAsync().ConfigureAwait(false);
            return new Abstractions.BlobProperties(
                blob.Properties.Length,
                blob.Properties.Created,
                blob.Properties.LastModified,
                blob.Properties.ETag);
        }

        private async Task<CloudBlockBlob> GetBlobReferenceAsync(string path, CancellationToken cancellationToken)
        {
            var pathSegments = path.Split('/');
            var containerName = pathSegments.First();
            var blobName = string.Join("/", pathSegments.Skip(1));

            var container = await GetCloudBlobContainerAsync(containerName, cancellationToken).ConfigureAwait(false);
            return container.GetBlockBlobReference(blobName);
        }

        public async Task<Stream> OpenReadAsync(string path, CancellationToken cancellationToken = default)
        {
            var blob = await GetBlobReferenceAsync(path, cancellationToken).ConfigureAwait(false);
            return await blob.OpenReadAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task<Stream> OpenWriteAsync(string path, CancellationToken cancellationToken = default)
        {
            var blob = await GetBlobReferenceAsync(path, cancellationToken).ConfigureAwait(false);
            return await blob.OpenWriteAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task<bool> ExistsAsync(string path, CancellationToken cancellationToken = default)
        {
            var blob = await GetBlobReferenceAsync(path, cancellationToken).ConfigureAwait(false);
            return await blob.ExistsAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task DeleteAsync(string path, CancellationToken cancellationToken = default)
        {
            try
            {
                var blob = await GetBlobReferenceAsync(path, cancellationToken).ConfigureAwait(false);
                await blob.DeleteAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e) when (e.Message.Contains("does not exist."))
            {
            }
        }

        public void Dispose()
        {
        }

        private async Task<CloudBlobContainer> GetCloudBlobContainerAsync(string containerName, CancellationToken cancellationToken)
        {
            var cloudBlobClient = _storageAccount.CreateCloudBlobClient();
            var cloudBlobContainer = cloudBlobClient.GetContainerReference(containerName);

            if (await cloudBlobContainer.ExistsAsync(cancellationToken).ConfigureAwait(false) == false) // TODO: Create on throw and do not always check
            {
                await cloudBlobContainer.CreateAsync(cancellationToken).ConfigureAwait(false);
            }

            return cloudBlobContainer;
        }
    }
}
