using Azure;
using Azure.Data.Tables;
using Xunit;
using Xunit.Abstractions;

namespace AzureStorageSample.Tests
{
    public class TableSamples : TestBase
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public TableSamples(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public async Task S1_SampleUsingTableEntity()
        {
            var tableName = "Employees1";
            var serviceClient = new TableServiceClient(AzureStorageConnectionString);

            await serviceClient.CreateTableAsync(tableName);
            var tableClient = serviceClient.GetTableClient(tableName);

            var andrew = new CustomerEntity("Andrew", "Fuller")
            {
                Title = "Software engineer",
                Country = "USA",
                City = "New York",
                PhoneNumber = "+1-202-555-0132"
            };

            var entity = andrew.ToTableEntity();

            // Entity doesn't exist in table, so invoking UpsertEntity will simply insert the entity.
            await tableClient.UpsertEntityAsync(andrew);

            // Delete an entity property.
            entity.Remove("PhoneNumber");

            // Entity does exist in the table, so invoking UpsertEntity will update using the given UpdateMode, which defaults to Merge if not given.
            // Since UpdateMode.Replace was passed, the existing entity will be replaced and delete the "Brand" property.
            await tableClient.UpsertEntityAsync(entity, TableUpdateMode.Replace);

            // Get the entity to update.
            TableEntity updatedEntity = await tableClient.GetEntityAsync<TableEntity>(entity.PartitionKey, entity.RowKey);
            updatedEntity["Title"] = "Sr. Software engineer";

            // Since no UpdateMode was passed, the request will default to Merge.
            await tableClient.UpdateEntityAsync(updatedEntity, updatedEntity.ETag);

            updatedEntity = await tableClient.GetEntityAsync<TableEntity>(entity.PartitionKey, entity.RowKey);
            _testOutputHelper.WriteLine($"Title before updating: {entity.GetString("Title")}");
            _testOutputHelper.WriteLine($"Title after updating: {updatedEntity.GetString("Title")}");

            await serviceClient.DeleteTableAsync(tableName);
        }

        [Fact]
        public async Task S1_SampleUsingCustomerEntity()
        {
            var tableName = "Employees2";
            var serviceClient = new TableServiceClient(AzureStorageConnectionString);

            await serviceClient.CreateTableAsync(tableName);
            var tableClient = serviceClient.GetTableClient(tableName);

            var entity = new CustomerEntity("Andrew", "Fuller")
            {
                Title = "Software engineer",
                Country = "USA",
                City = "New York",
                PhoneNumber = "+1-202-555-0132"
            };

            // Entity doesn't exist in table, so invoking UpsertEntity will simply insert the entity.
            await tableClient.UpsertEntityAsync(entity);

            var updatedEntity = (await tableClient.GetEntityAsync<CustomerEntity>(entity.PartitionKey, entity.RowKey)).Value;
            updatedEntity.Title = "Sr. Software engineer";

            // Since no UpdateMode was passed, the request will default to Merge.
            await tableClient.UpdateEntityAsync(updatedEntity, updatedEntity.ETag);

            updatedEntity = (await tableClient.GetEntityAsync<CustomerEntity>(entity.PartitionKey, entity.RowKey)).Value;
            _testOutputHelper.WriteLine($"Title before updating: {entity.Title}");
            _testOutputHelper.WriteLine($"Title after updating: {updatedEntity.Title}");

            await serviceClient.DeleteTableAsync(tableName);
        }

        [Fact]
        public async Task S3_QueryEntitiesAsync()
        {
            // Source: https://github.com/Azure/azure-sdk-for-net/blob/bbc6fb229b6f1f3c318cd57696ff265ec662f565/sdk/tables/Azure.Data.Tables/tests/samples/Sample2_CreateDeleteEntities.cs
            var random = new Random();
            var serviceClient = new TableServiceClient(AzureStorageConnectionString);
            var tableName = "OfficeSupplies4p2" + random.Next();
            var partitionKey = "somePartition";
            var rowKey = "1";
            var rowKey2 = "2";

            await serviceClient.CreateTableAsync(tableName);
            var tableClient = serviceClient.GetTableClient(tableName);

            var entity = new TableEntity(partitionKey, rowKey)
            {
                { "Product", "Markers" },
                { "Price", 5.00 },
                { "Quantity", 10 },
            };
            await tableClient.AddEntityAsync(entity);

            var entity2 = new TableEntity(partitionKey, rowKey2)
            {
                { "Product", "Chair" },
                { "Price", 7.00 },
            };
            await tableClient.AddEntityAsync(entity2);

            // Use the <see cref="TableClient"> to query the table. Passing in OData filter strings is optional.
            AsyncPageable<TableEntity> queryResults = tableClient.QueryAsync<TableEntity>(filter: $"PartitionKey eq '{partitionKey}'");
            int count = 0;

            // Iterate the list in order to access individual queried entities.
            await foreach (TableEntity qEntity in queryResults)
            {
                _testOutputHelper.WriteLine($"{qEntity.GetString("Product")}: {qEntity.GetDouble("Price")}");
                count++;
            }

            _testOutputHelper.WriteLine($"The query returned {count} entities.");

            // Use the <see cref="TableClient"> to query the table using a filter expression.
            double priceCutOff = 6.00;
            AsyncPageable<OfficeSupplyEntity> queryResultsLINQ = tableClient.QueryAsync<OfficeSupplyEntity>(ent => ent.Price >= priceCutOff);

            AsyncPageable<TableEntity> queryResultsSelect = tableClient.QueryAsync<TableEntity>(select: new List<string>() { "Product", "Price" });

            AsyncPageable<TableEntity> queryResultsMaxPerPage = tableClient.QueryAsync<TableEntity>(maxPerPage: 10);

            // Iterate the <see cref="Pageable"> by page.
            await foreach (Page<TableEntity> page in queryResultsMaxPerPage.AsPages())
            {
                _testOutputHelper.WriteLine("This is a new page!");
                foreach (TableEntity qEntity in page.Values)
                {
                    _testOutputHelper.WriteLine($"{qEntity.GetString("Product")} inventoried: {qEntity.GetInt32("Quantity")}");
                }
            }

            await serviceClient.DeleteTableAsync(tableName);
        }

        [Fact]
        public async Task S4_TransactionalBatchAsync()
        {
            // Source: https://github.com/Azure/azure-sdk-for-net/blob/50ebe8ca8b3ac2a1afeae606518966051f9d77e2/sdk/tables/Azure.Data.Tables/tests/samples/Sample6_TransactionalBatchAsync.cs
            var random = new Random();
            var serviceClient = new TableServiceClient(AzureStorageConnectionString);
            var tableName = "OfficeSuppliesBatch" + random.Next();
            var partitionKey = "BatchInsertSample";

            await serviceClient.CreateTableAsync(tableName);
            TableClient client = serviceClient.GetTableClient(tableName);

            // Create a list of 5 entities with the same partition key.
            List<TableEntity> entityList = new List<TableEntity>
            {
                new TableEntity(partitionKey, "01")
                {
                    { "Product", "Marker" },
                    { "Price", 5.00 },
                    { "Brand", "Premium" }
                },
                new TableEntity(partitionKey, "02")
                {
                    { "Product", "Pen" },
                    { "Price", 3.00 },
                    { "Brand", "Premium" }
                },
                new TableEntity(partitionKey, "03")
                {
                    { "Product", "Paper" },
                    { "Price", 0.10 },
                    { "Brand", "Premium" }
                },
                new TableEntity(partitionKey, "04")
                {
                    { "Product", "Glue" },
                    { "Price", 1.00 },
                    { "Brand", "Generic" }
                },
            };

            // Create the batch.
            List<TableTransactionAction> addEntitiesBatch = new List<TableTransactionAction>();

            // Add the entities to be added to the batch.
            addEntitiesBatch.AddRange(entityList.Select(e => new TableTransactionAction(TableTransactionActionType.Add, e)));

            // Submit the batch.
            Response<IReadOnlyList<Response>> response = await client.SubmitTransactionAsync(addEntitiesBatch).ConfigureAwait(false);

            for (int i = 0; i < entityList.Count; i++)
            {
                _testOutputHelper.WriteLine($"The ETag for the entity with RowKey: '{entityList[i].RowKey}' is {response.Value[i].Headers.ETag}");
            }

            var entity = entityList[0];
            var tableClient = client;

            // Create a collection of TableTransactionActions and populate it with the actions for each entity.
            List<TableTransactionAction> batch = new List<TableTransactionAction>
            {
                new TableTransactionAction(TableTransactionActionType.UpdateMerge, entity)
            };

            // Execute the transaction.
            Response<IReadOnlyList<Response>> batchResult = tableClient.SubmitTransaction(batch);

            // Display the ETags for each item in the result.
            // Note that the ordering between the entties in the batch and the responses in the batch responses will always be conssitent.
            for (int i = 0; i < batch.Count; i++)
            {
                _testOutputHelper.WriteLine($"The ETag for the entity with RowKey: '{batch[i].Entity.RowKey}' is {batchResult.Value[i].Headers.ETag}");
            }

            // Create a new batch.
            List<TableTransactionAction> mixedBatch = new List<TableTransactionAction>();

            // Add an entity for deletion to the batch.
            mixedBatch.Add(new TableTransactionAction(TableTransactionActionType.Delete, entityList[0]));

            // Remove this entity from our list so that we can track that it will no longer be in the table.
            entityList.RemoveAt(0);

            // Change only the price of the entity with a RoyKey equal to "02".
            TableEntity mergeEntity = new TableEntity(partitionKey, "02") { { "Price", 3.50 }, };

            // Add a merge operation to the batch.
            // We specify an ETag value of ETag.All to indicate that this merge should be unconditional.
            mixedBatch.Add(new TableTransactionAction(TableTransactionActionType.UpdateMerge, mergeEntity, ETag.All));

            // Update a property on an entity.
            TableEntity updateEntity = entityList[2];
            updateEntity["Brand"] = "Generic";

            // Add an upsert operation to the batch.
            // Using the UpsertEntity method allows us to implicitly ignore the ETag value.
            mixedBatch.Add(new TableTransactionAction(TableTransactionActionType.UpsertReplace, updateEntity));

            // Submit the batch.
            await client.SubmitTransactionAsync(mixedBatch).ConfigureAwait(false);

            // Create a new batch.
            List<TableTransactionAction> deleteEntitiesBatch = new List<TableTransactionAction>();

            // Add the entities for deletion to the batch.
            foreach (TableEntity entityToDelete in entityList)
            {
                deleteEntitiesBatch.Add(new TableTransactionAction(TableTransactionActionType.Delete, entityToDelete));
            }

            // Submit the batch.
            await client.SubmitTransactionAsync(deleteEntitiesBatch).ConfigureAwait(false);

            // Delete the table.
            await client.DeleteAsync();
        }

        public class CustomerEntity : ITableEntity
        {
            public CustomerEntity()
            {
            }

            public CustomerEntity(string firstName, string lastName)
            {
                PartitionKey = firstName;
                RowKey = lastName;
            }

            public string PartitionKey { get; set; }
            public string RowKey { get; set; }
            public string? Title { get; set; }
            public string? Country { get; set; }
            public string? City { get; set; }
            public string? PhoneNumber { get; set; }
            public DateTimeOffset? Timestamp { get; set; }
            public ETag ETag { get; set; }
            public string FirstName => PartitionKey;
            public string LastName => RowKey;

            public TableEntity ToTableEntity()
            {
                return new TableEntity(PartitionKey, RowKey)
                {
                    { nameof(Title), Title },
                    { nameof(Country), Country },
                    { nameof(City), City },
                    { nameof(City), City },
                    { nameof(PhoneNumber), PhoneNumber }
                };
            }
        }

        public class OfficeSupplyEntity : ITableEntity
        {
            public string Product { get; set; }
            public double Price { get; set; }
            public int Quantity { get; set; }
            public string PartitionKey { get; set; }
            public string RowKey { get; set; }
            public DateTimeOffset? Timestamp { get; set; }
            public ETag ETag { get; set; }
        }
    }
}
