using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Xunit;
using Xunit.Abstractions;

namespace AzureStorageSample.Tests
{
    public class QueueSamples : TestBase
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public QueueSamples(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public async Task S1_SendMessageAsync()
        {
            QueueClient queue = new QueueClient(AzureStorageConnectionString, "sample-queue");
            await queue.CreateAsync();

            // Send a messages to our queue
            for (var i = 0; i < 5; i++)
            {
                // TODO: show method overloads
                await queue.SendMessageAsync($"Message #{i}");
            }
        }

        [Fact]
        public async Task S2_ReceiveMessagesAsync()
        {
            QueueClient queue = new QueueClient(AzureStorageConnectionString, "sample-queue");

            // Get the next messages from the queue
            foreach (QueueMessage message in (await queue.ReceiveMessagesAsync(maxMessages: 10)).Value)
            {
                // "Process" the message
                _testOutputHelper.WriteLine($"Message: {message.Body}");

                // await queue.UpdateMessageAsync(message.MessageId, message.PopReceipt, message.Body, TimeSpan.FromSeconds(5));
                await queue.DeleteMessageAsync(message.MessageId, message.PopReceipt);
            }
        }

        [Fact]
        public async Task S3_PeekMessagesAsync()
        {
            QueueClient queue = new QueueClient(AzureStorageConnectionString, "sample-queue");

            foreach (PeekedMessage message in (await queue.PeekMessagesAsync(maxMessages: 10)).Value)
            {
                // Inspect the message
                _testOutputHelper.WriteLine($"Message: {message.Body}");
            }
        }
    }
}
