using System.Text.Json;
using Azure.Data.Tables;
using Azure.Messaging.EventGrid;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;

namespace AzureStorageSample.Api
{
    public class QueueListenerBackgroundService : BackgroundService
    {
        private const string TableName = "AzureStorageSample";
        private readonly TimeSpan _delay = TimeSpan.FromSeconds(10);
        private readonly QueueClient _queueClient;
        private readonly TableServiceClient _tableServiceClient;
        private readonly BlobContainerClient _blobContainerClient;
        private readonly ILogger<QueueListenerBackgroundService> _logger;

        public QueueListenerBackgroundService(ILogger<QueueListenerBackgroundService> logger, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("AzureStorage");
            _queueClient = new QueueClient(connectionString, "azurestoragesamplequeue", new QueueClientOptions { MessageEncoding = QueueMessageEncoding.Base64 });
            _tableServiceClient = new TableServiceClient(connectionString);
            _blobContainerClient = new BlobContainerClient(connectionString, "azurestoragesample");
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogDebug($"QueueListenerBackgroundService is starting.");

            await _tableServiceClient.CreateTableIfNotExistsAsync(TableName);
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogDebug($"QueueListenerBackgroundService task is doing background work.");

                    while (await HasMessages())
                    {
                        // Receive and process 20 messages
                        QueueMessage[] receivedMessages = _queueClient.ReceiveMessages(10, TimeSpan.FromSeconds(5));
                        var tasks = receivedMessages.Select(ProcessMessage);
                        await Task.WhenAll(tasks);
                    }

                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to receive messages");
                }

                await Task.Delay(_delay, stoppingToken);
            }

            _logger.LogDebug($"QueueListenerBackgroundService background task is stopping.");
        }

        private async Task<bool> HasMessages()
        {
            return await _queueClient.ExistsAsync() && (await _queueClient.GetPropertiesAsync()).Value.ApproximateMessagesCount > 0;
        }

        private async Task ProcessMessage(QueueMessage message)
        {
            try
            {
                _logger.LogDebug("Message received: {Message}", message.Body);

                var eventGridEvent = JsonSerializer.Deserialize<EventGridEvent>(message.Body);
                var data = eventGridEvent.Data.ToObjectFromJson<Dictionary<string, object>>();
                var url = data["url"].ToString();
                var blobUriBuilder = new BlobUriBuilder(new Uri(url));

                var blobClient = _blobContainerClient.GetBlobClient(blobUriBuilder.BlobName);
                var blob = await blobClient.DownloadContentAsync();
                var tableClient = _tableServiceClient.GetTableClient(TableName);
                var entity = new TableEntity(DateTime.UtcNow.Date.ToString("yyyy-MM-dd"), blobClient.Name)
                {
                    { "Content", blob.Value.Content.ToString() },
                    { "LastModified", blob.Value.Details.LastModified },
                    { "BlobType", blob.Value.Details.BlobType.ToString() },
                    { "Uri", blobClient.Uri.ToString() },
                    { "Content", blob.Value.Content.ToString() },
                    { "EventData", eventGridEvent.Data.ToString() },
                };

                foreach (var item in blob.Value.Details.Metadata)
                {
                    entity[item.Key] = item.Value;
                }

                await tableClient.UpsertEntityAsync(entity, TableUpdateMode.Replace);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process message. MessageId:{MessageId}, Body:{Body}", message.MessageId, message.Body);
            }

            _queueClient.DeleteMessage(message.MessageId, message.PopReceipt);
        }
    }
}
