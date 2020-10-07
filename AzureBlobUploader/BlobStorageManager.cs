using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;

namespace AzureBloblUploader
{
    /// <summary>
    /// Blob storage manager class
    /// </summary>
    public class BlobStorageManager
    {
        private readonly CloudStorageAccount _storageaccount;
        private readonly CloudBlobClient _blobStorageClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="BlobStorageManager" /> class.
        /// </summary>
        /// <param name="connectionStringName">Name of the connection string in app.config or web.config file.</param>
        public BlobStorageManager(string connectionStringName)
        {
            _storageaccount = CloudStorageAccount.Parse(ConfigurationManager.ConnectionStrings[connectionStringName].ConnectionString);

            _blobStorageClient = _storageaccount.CreateCloudBlobClient();
            _blobStorageClient.RetryPolicy = RetryPolicies.Retry(4, TimeSpan.Zero);
        }

        /// <summary>
        /// Updates or created a blob in Azure blob storage
        /// </summary>
        /// <param name="containerName">Name of the container.</param>
        /// <param name="blobName">Name of the blob.</param>
        /// <param name="content">The content of the blob.</param>
        /// <returns></returns>
        public bool UploadBlob(string containerName, string blobName, byte[] content)
        {
            return Execute(
                    () =>
                    {
                        CloudBlobContainer container = _blobStorageClient.GetContainerReference(containerName);
                        CloudBlob blob = container.GetBlobReference(blobName);
                        blob.UploadByteArray(content);
                    });
        }

        /// <summary>
        /// Creates the container in Azure blobl storage
        /// </summary>
        /// <param name="containerName">Name of the container.</param>
        /// <returns>True if contianer was created successfully</returns>
        public bool CreateContainer(string containerName)
        {
            return Execute(
                    () =>
                    {
                        CloudBlobContainer container = _blobStorageClient.GetContainerReference(containerName);
                        container.Create();
                    });
        }

        /// <summary>
        /// Checks if a container exist.
        /// </summary>
        /// <param name="containerName">Name of the container.</param>
        /// <returns>True if conainer exists</returns>
        public bool DoesContainerExist(string containerName)
        {
            bool returnValue = false;
            Execute(
                    () =>
                    {
                        IEnumerable<CloudBlobContainer> containers = _blobStorageClient.ListContainers();
                        returnValue = containers.Any(one => one.Name == containerName);
                    });
            return returnValue;
        }

        /// <summary>
        /// Uploads the directory to blobl storage
        /// </summary>
        /// <param name="sourceDirectory">The source directory name.</param>
        /// <param name="containerName">Name of the container to upload to.</param>
        /// <returns>True if successfully uploaded</returns>
        public bool UploadDirectory(string sourceDirectory, string containerName)
        {
            return UploadDirectory(sourceDirectory, containerName, string.Empty);
        }

        private bool UploadDirectory(string sourceDirectory, string containerName, string prefixAzureFolderName)
        {
            return Execute(
                () =>
                {
                    // create container if not exists
                    if (!DoesContainerExist(containerName))
                    {
                        CreateContainer(containerName);
                    }
                    var folder = new DirectoryInfo(sourceDirectory);
                    var files = folder.GetFiles();
                    foreach (var fileInfo in files)
                    {
                        string blobName = fileInfo.Name;
                        if (!string.IsNullOrEmpty(prefixAzureFolderName))
                        {
                            blobName = prefixAzureFolderName + "/" + blobName;
                        }
                        UploadBlob(containerName, blobName, File.ReadAllBytes(fileInfo.FullName));
                    }
                    var subFolders = folder.GetDirectories();
                    foreach (var directoryInfo in subFolders)
                    {
                        var prefix = directoryInfo.Name;
                        if (!string.IsNullOrEmpty(prefixAzureFolderName))
                        {
                            prefix = prefixAzureFolderName + "/" + prefix;
                        }
                        UploadDirectory(directoryInfo.FullName, containerName, prefix);
                    }
                });
        }

        private bool Execute(Action doWork)
        {
            try
            {
                doWork();
                return true;
            }
            catch (StorageClientException ex)
            {
                if ((int)ex.StatusCode == 409)
                {
                    return false;
                }
                throw;
            }
        }
    }
}
