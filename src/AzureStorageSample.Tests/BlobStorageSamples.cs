using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace AzureStorageSample.Tests
{
    public class BlobStorageSamples : TestBase
    {
        private const string SampleText = @"This is just some sample data";
        private readonly ITestOutputHelper _testOutputHelper;

        public BlobStorageSamples(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public async Task S1_UploadAsync()
        {
            var file = Utilities.CreateTempFile(SampleText);
            var container = new BlobContainerClient(AzureStorageConnectionString, "upload-sample-container");
            await container.CreateAsync();

            // Get a reference to a blob
            BlobClient blob = container.GetBlobClient("sample-file");

            // Upload file data
            await blob.UploadAsync(file.FullName);

            BlobProperties properties = await blob.GetPropertiesAsync();
            properties.ContentLength.Should().Be(file.Length);
        }

        [Fact]
        public async Task S2_DownloadAsync()
        {
            var downloadPath = Utilities.CreateTempPath();

            BlobContainerClient container = new BlobContainerClient(AzureStorageConnectionString, "upload-sample-container");
            BlobClient blob = container.GetBlobClient("sample-file");
            await blob.DownloadToAsync(downloadPath);
            _testOutputHelper.WriteLine($"File downloaded to: {downloadPath}");

            // Verify the contents
            var content = File.ReadAllText(downloadPath);
            content.Should().Be(SampleText);
        }

        [Fact]
        public void S3_GetSasTokenAsync()
        {
            // Get a reference to a container named "sample-container" and then create it
            BlobContainerClient container = new BlobContainerClient(AzureStorageConnectionString, "upload-sample-container");

            // Get a reference to a blob named "sample-file"
            BlobClient blob = container.GetBlobClient("sample-file");

            var sasToken = blob.GenerateSasUri(Azure.Storage.Sas.BlobSasPermissions.Read, DateTimeOffset.UtcNow.AddMinutes(1));

            _testOutputHelper.WriteLine(sasToken.ToString());
        }

        [Fact]
        public async Task S4_DeleteAsync()
        {
            var container = new BlobContainerClient(AzureStorageConnectionString, "upload-sample-container");
            await container.DeleteIfExistsAsync();
        }

        [Fact]
        public async Task S5_ListAsync()
        {
            BlobServiceClient service = new BlobServiceClient(AzureStorageConnectionString);
            BlobContainerClient container = service.GetBlobContainerClient("list-sample-container");
            await container.CreateAsync();
            try
            {
                var tempFile = Utilities.CreateTempFile(SampleText);
                BlobClient first = container.GetBlobClient("first");
                BlobClient second = container.GetBlobClient("second");
                BlobClient third = container.GetBlobClient("third");

                await container.UploadBlobAsync("first", File.OpenRead(tempFile.FullName));
                await container.UploadBlobAsync("second", File.OpenRead(tempFile.FullName));
                await container.UploadBlobAsync("third", File.OpenRead(tempFile.FullName));

                BlobBatchClient batch = service.GetBlobBatchClient();
                await batch.SetBlobsAccessTierAsync(new Uri[] { first.Uri, second.Uri, third.Uri }, AccessTier.Cool);

                var names = new List<string>();
                await foreach (BlobItem blob in container.GetBlobsAsync())
                {
                    names.Add(blob.Name);
                }

                names.Count.Should().Be(3);
                names.Should().Contain(new[] { "first", "second", "third" });
            }
            finally
            {
                await container.DeleteAsync();
            }
        }

        [Fact]
        public async Task S6_ErrorsAsync()
        {
            BlobContainerClient container = new BlobContainerClient(AzureStorageConnectionString, Randomize("sample-container"));
            await container.CreateAsync();
            var isCreateFailed = false;

            try
            {
                // Try to create the container again
                await container.CreateAsync();
            }
            catch (RequestFailedException ex) when (ex.ErrorCode == BlobErrorCode.ContainerAlreadyExists)
            {
                // Ignore any errors if the container already exists
                isCreateFailed = true;
            }

            await container.DeleteAsync();

            isCreateFailed.Should().BeTrue();
        }
    }
}
