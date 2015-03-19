using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Orleans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Two10.GrainExtensions
{
    public class BlobLease
    {
        string leaseId;
        CloudBlockBlob blob;
        CloudBlobContainer container;

        public BlobLease(string connectionString,string containerName, string blobName)
        {
            if (string.IsNullOrWhiteSpace(connectionString)) throw new ArgumentNullException("connectionString");
            if (string.IsNullOrWhiteSpace(containerName)) throw new ArgumentNullException("containerName");
            if (string.IsNullOrWhiteSpace(blobName)) throw new ArgumentNullException("blobName");

            var account = CloudStorageAccount.Parse(connectionString);
            var blobClient = account.CreateCloudBlobClient();
            container = blobClient.GetContainerReference(containerName);
            blob = container.GetBlockBlobReference(blobName);
        }

        public async Task Init(TimeSpan leaseTime)
        {
            await container.CreateIfNotExistsAsync();
            await blob.UploadTextAsync(DateTime.UtcNow.ToString());

            // we get an exception here is the lease cannot be aquired
            leaseId = await blob.AcquireLeaseAsync(leaseTime, null);
            if (string.IsNullOrWhiteSpace(leaseId)) throw new ApplicationException("no leaseId");
        }

        public Task Renew()
        {
            if (string.IsNullOrWhiteSpace(this.leaseId)) return TaskDone.Done;
            return blob.RenewLeaseAsync(AccessCondition.GenerateLeaseCondition(leaseId), 
                new BlobRequestOptions { MaximumExecutionTime = TimeSpan.FromSeconds(5), ServerTimeout = TimeSpan.FromSeconds(5)}, 
                null);
        }


        public Task Release()
        {
            var condition = AccessCondition.GenerateLeaseCondition(this.leaseId);
            this.leaseId = null;    
            return blob.ReleaseLeaseAsync(condition);
        }
    }
}
