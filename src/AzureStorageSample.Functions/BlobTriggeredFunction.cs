using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace AzureStorageSample.Functions
{
    public class BlobTriggeredFunction
    {
        [FunctionName("BlobTriggeredFunction")]
        public async Task Run(
            [BlobTrigger("azurestoragesample/{name}.txt")] BlobClient blobClient,
            string name,
            ILogger log,
            [Table("MyTable")] TableClient tableClient)
        {
            log.LogInformation($"C# Blob trigger function Processed blob\n Name:{name}");
            var blob = await blobClient.DownloadContentAsync();
            var entity = new TableEntity(DateTime.UtcNow.Date.ToString("yyyy-MM-dd"), blobClient.Name)
            {
                { "Content", blob.Value.Content.ToString() },
                { "LastModified", blob.Value.Details.LastModified },
                { "BlobType", blob.Value.Details.BlobType.ToString() },
                { "Uri", blobClient.Uri.ToString() },
                { "Content", blob.Value.Content.ToString() },
                { "EventData", null },
            };

            await tableClient.UpsertEntityAsync(entity);
        }
    }
}
