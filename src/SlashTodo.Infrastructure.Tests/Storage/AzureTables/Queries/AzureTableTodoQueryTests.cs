using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Moq;
using NUnit.Framework;
using SlashTodo.Core.Domain;
using SlashTodo.Core.Dtos;
using SlashTodo.Core.Lookups;
using SlashTodo.Infrastructure.Configuration;
using SlashTodo.Infrastructure.Messaging;
using SlashTodo.Infrastructure.Storage.AzureTables;
using SlashTodo.Infrastructure.Storage.AzureTables.Lookups;
using SlashTodo.Infrastructure.Storage.AzureTables.Queries;
using TinyMessenger;

namespace SlashTodo.Infrastructure.Tests.Storage.AzureTables.Queries
{
    [TestFixture]
    public class AzureTableTodoQueryTests
    {
        private readonly AzureSettings _azureSettings = new AzureSettings(new AppSettings());
        private AzureTableTodoQuery _todoQuery;
        private IMessageBus _bus;
        private AzureTableTodoQuery.TodoQueryTableNames _tableNames;

        [SetUp]
        public void BeforeEachTest()
        {
            _bus = new TinyMessageBus(new TinyMessengerHub());
            // Reference different tables for each test to ensure isolation.
            _tableNames = new AzureTableTodoQuery.TodoQueryTableNames
            {
                TodoBySlackConversationId = string.Format("test{0}", Guid.NewGuid().ToString("N")),
                TodoClaimedBySlackUserId = string.Format("test{0}", Guid.NewGuid().ToString("N")),
                TodoCompletedBySlackUserId = string.Format("test{0}", Guid.NewGuid().ToString("N"))
            };
            _todoQuery = new AzureTableTodoQuery(
                CloudStorageAccount.Parse(_azureSettings.StorageConnectionString), _tableNames);
            _todoQuery.RegisterSubscriptions((ISubscriptionRegistry)_bus);
            CreateTablesIfNotExists();
        }

        [TearDown]
        public void AfterEachTest()
        {
            // Delete the tables used by the test that just finished running.
            // Note that according to MSDN, deleting a table can take minutes,
            // so we won't hang around waiting for the result.
            DeleteTablesIfExists();
            _todoQuery.Dispose();
        }

        private CloudTable GetTable(string tableName)
        {
            var storageAccount = CloudStorageAccount.Parse(_azureSettings.StorageConnectionString);
            var cloudTableClient = storageAccount.CreateCloudTableClient();
            var table = cloudTableClient.GetTableReference(tableName);
            return table;
        }

        private AzureTableTodoQuery.TodoDtoTableEntity GetTodoDtoEntity(string tableName, string partitionKey, string rowKey)
        {
            var table = GetTable(tableName);
            var retrieveOp = TableOperation.Retrieve<AzureTableTodoQuery.TodoDtoTableEntity>(partitionKey, rowKey);
            return table.Execute(retrieveOp).Result as AzureTableTodoQuery.TodoDtoTableEntity;
        }

        private void InsertTodoDtoEntities(TodoDto dto, string completedBySlackUserId = null)
        {
            GetTable(_tableNames.TodoBySlackConversationId)
                .Execute(TableOperation.Insert(
                    new AzureTableTodoQuery.TodoDtoTableEntity(
                        dto,
                        x => x.SlackConversationId,
                        x => x.Id.ToString())));
            if (dto.ClaimedBySlackUserId.HasValue())
            {
                GetTable(_tableNames.TodoClaimedBySlackUserId)
                    .Execute(TableOperation.Insert(
                        new AzureTableTodoQuery.TodoDtoTableEntity(
                            dto,
                            x => x.ClaimedBySlackUserId,
                            x => x.Id.ToString())));
            }
            if (completedBySlackUserId.HasValue())
            {
                GetTable(_tableNames.TodoCompletedBySlackUserId)
                    .Execute(TableOperation.Insert(
                        new AzureTableTodoQuery.TodoDtoTableEntity(
                            dto,
                            x => completedBySlackUserId,
                            x => x.Id.ToString())));
            }
        }

        private void CreateTablesIfNotExists()
        {
            var table = GetTable(_tableNames.TodoBySlackConversationId);
            table.CreateIfNotExists();
            table = GetTable(_tableNames.TodoClaimedBySlackUserId);
            table.CreateIfNotExists();
            table = GetTable(_tableNames.TodoCompletedBySlackUserId);
            table.CreateIfNotExists();
        }

        private void DeleteTablesIfExists()
        {
            var table = GetTable(_tableNames.TodoBySlackConversationId);
            table.DeleteIfExists();
            table = GetTable(_tableNames.TodoClaimedBySlackUserId);
            table.DeleteIfExists();
            table = GetTable(_tableNames.TodoCompletedBySlackUserId);
            table.DeleteIfExists();
        }

        [Test]
        public async Task InsertsNewRowsOnTodoAdded()
        {
            // Arrange
            var todoAdded = new TodoAdded
            {
                Id = Guid.NewGuid(),
                Timestamp = DateTime.UtcNow,
                AccountId = Guid.NewGuid(),
                SlackConversationId = "slackConversationId",
                ShortCode = "shortCode",
                Text = "text"
            };

            // Act
            await _bus.Publish(todoAdded);

            // Assert
            var entities = new[]
            {
                GetTodoDtoEntity(_tableNames.TodoBySlackConversationId, todoAdded.SlackConversationId, todoAdded.SlackConversationId),
            };
            foreach (var entity in entities)
            {
                Assert.That(entity, Is.Not.Null);
                Assert.That(entity.Id, Is.EqualTo(todoAdded.Id));
                Assert.That(entity.CreatedAt, Is.EqualTo(todoAdded.Timestamp));
                Assert.That(entity.AccountId, Is.EqualTo(todoAdded.AccountId));
                Assert.That(entity.SlackConversationId, Is.EqualTo(todoAdded.SlackConversationId));
                Assert.That(entity.ShortCode, Is.EqualTo(todoAdded.ShortCode));
                Assert.That(entity.Text, Is.EqualTo(todoAdded.Text));
            }
        }

        [Test]
        public async Task UpdatesOrRemovesRowsOnTodoRemoved()
        {
            // Arrange
            var userId = "userId";
            var dto = GetTodoDto(claimedBySlackUserId: userId, completedAt: DateTime.UtcNow);
            Assert.That(dto.RemovedAt.HasValue, Is.False);
            InsertTodoDtoEntities(dto, completedBySlackUserId: userId);
            var todoRemoved = new TodoRemoved
            {
                Id = dto.Id,
                Timestamp = DateTime.UtcNow,
                SlackConversationId = dto.SlackConversationId,
                ShortCode = dto.ShortCode
            };

            // Act
            await _bus.Publish(todoRemoved);

            // Assert
            var conversationTodoEntity = GetTodoDtoEntity(_tableNames.TodoBySlackConversationId, dto.SlackConversationId, dto.Id.ToString());
            Assert.That(conversationTodoEntity, Is.Null);
            var claimedTodoEntity = GetTodoDtoEntity(_tableNames.TodoClaimedBySlackUserId, dto.ClaimedBySlackUserId, dto.Id.ToString());
            Assert.That(claimedTodoEntity, Is.Null);
            var completedTodoEntity = GetTodoDtoEntity(_tableNames.TodoCompletedBySlackUserId, userId, dto.Id.ToString());
            Assert.That(completedTodoEntity.RemovedAt, Is.EqualTo(todoRemoved.Timestamp));
            Assert.That(completedTodoEntity.ClaimedBySlackUserId, Is.Null);
            Assert.That(completedTodoEntity.ClaimedByUserId, Is.Null);
        }

        [Test]
        public async Task BySlackConversationIdReturnsEmptyWhenQueryFails()
        {
            // Arrange

            // Act
            var todos = await _todoQuery.BySlackConversationId("whatever");

            // Assert
            Assert.That(todos, Is.Empty);
        }

        [Test]
        public async Task ClaimedBySlackUserIdReturnsEmptyWhenQueryFails()
        {
            // Arrange

            // Act
            var todos = await _todoQuery.ClaimedBySlackUserId("whatever");

            // Assert
            Assert.That(todos, Is.Empty);
        }

        [Test]
        public async Task CompletedBySlackUserIdReturnsEmptyWhenQueryFails()
        {
            // Arrange

            // Act
            var todos = await _todoQuery.CompletedBySlackUserId("whatever");

            // Assert
            Assert.That(todos, Is.Empty);
        }

        [Test]
        public async Task BySlackConversationIdReturnsTodoDtosWhenQueryIsSuccessful()
        {
            // Arrange
            var slackConversationId = "slackConversationId";
            var expectedDtos = new[]
            {
                GetTodoDto(slackConversationId: slackConversationId),
                GetTodoDto(slackConversationId: slackConversationId),
                GetTodoDto(slackConversationId: slackConversationId)
            }.OrderBy(x => x.Id).ToArray();
            foreach (var expectedDto in expectedDtos)
            {
                InsertTodoDtoEntities(expectedDto);
            }

            // Act
            var todos = await _todoQuery.BySlackConversationId(slackConversationId);

            // Assert
            var orderedTodos = todos.OrderBy(x => x.Id).ToArray();
            Assert.That(orderedTodos.Length, Is.EqualTo(expectedDtos.Length));
            for (var i = 0; i < expectedDtos.Length; i++)
            {
                expectedDtos[i].AssertIsEqualTo(orderedTodos[i]);
            }
        }

        [Test]
        public async Task ClaimedBySlackUserIdReturnsTodoDtosWhenQueryIsSuccessful()
        {
            // Arrange
            var claimedBySlackUserId = "slackUserId";
            var expectedDtos = new[]
            {
                GetTodoDto(claimedBySlackUserId: claimedBySlackUserId),
                GetTodoDto(claimedBySlackUserId: claimedBySlackUserId),
                GetTodoDto(claimedBySlackUserId: claimedBySlackUserId)
            }.OrderBy(x => x.Id).ToArray();
            foreach (var expectedDto in expectedDtos)
            {
                InsertTodoDtoEntities(expectedDto);
            }

            // Act
            var todos = await _todoQuery.ClaimedBySlackUserId(claimedBySlackUserId);

            // Assert
            var orderedTodos = todos.OrderBy(x => x.Id).ToArray();
            Assert.That(orderedTodos.Length, Is.EqualTo(expectedDtos.Length));
            for (var i = 0; i < expectedDtos.Length; i++)
            {
                expectedDtos[i].AssertIsEqualTo(orderedTodos[i]);
            }
        }

        [Test]
        public async Task CompletedBySlackUserIdReturnsTodoDtosWhenQueryIsSuccessful()
        {
            // Arrange
            var completedBySlackUserId = "slackUserId";
            var expectedDtos = new[]
            {
                GetTodoDto(completedAt: DateTime.UtcNow.AddDays(-1)),
                GetTodoDto(completedAt: DateTime.UtcNow.AddHours(-1)),
                GetTodoDto(completedAt: DateTime.UtcNow.AddMinutes(-1))
            }.OrderBy(x => x.Id).ToArray();
            foreach (var expectedDto in expectedDtos)
            {
                GetTable(_tableNames.TodoCompletedBySlackUserId)
                    .Execute(TableOperation.Insert(
                        new AzureTableTodoQuery.TodoDtoTableEntity(
                            expectedDto,
                            x => completedBySlackUserId,
                            x => x.Id.ToString())));
            }

            // Act
            var todos = await _todoQuery.CompletedBySlackUserId(completedBySlackUserId);

            // Assert
            var orderedTodos = todos.OrderBy(x => x.Id).ToArray();
            Assert.That(orderedTodos.Length, Is.EqualTo(expectedDtos.Length));
            for (var i = 0; i < expectedDtos.Length; i++)
            {
                expectedDtos[i].AssertIsEqualTo(orderedTodos[i]);
            }
        }

        private static TodoDto GetTodoDto(
            Guid? id = null,
            Guid? accountId = null,
            string slackConversationId = "slackConversationId",
            string shortCode = "shortCode",
            string text = "text",
            Guid? claimedByUserId = null,
            string claimedBySlackUserId = null,
            DateTime? createdAt = null,
            DateTime? completedAt = null,
            DateTime? removedAt = null)
        {
            return new TodoDto
            {
                Id = id ?? Guid.NewGuid(),
                AccountId = accountId ?? Guid.NewGuid(),
                SlackConversationId = slackConversationId,
                ShortCode = shortCode,
                Text = text,
                ClaimedByUserId = claimedByUserId,
                ClaimedBySlackUserId = claimedBySlackUserId,
                CreatedAt = createdAt ?? DateTime.UtcNow.AddDays(-3),
                CompletedAt = completedAt,
                RemovedAt = removedAt
            };
        }
    }
    public static class TodoDtoExtensions
    {
        public static void AssertIsEqualTo(this TodoDto actualDto, TodoDto expectedDto)
        {
            Assert.That(actualDto, Is.Not.Null);
            Assert.That(actualDto.Id, Is.EqualTo(expectedDto.Id));
            Assert.That(actualDto.AccountId, Is.EqualTo(expectedDto.AccountId));
            Assert.That(actualDto.SlackConversationId, Is.EqualTo(expectedDto.SlackConversationId));
            Assert.That(actualDto.ShortCode, Is.EqualTo(expectedDto.ShortCode));
            Assert.That(actualDto.Text, Is.EqualTo(expectedDto.Text));
            Assert.That(actualDto.ClaimedBySlackUserId, Is.EqualTo(expectedDto.ClaimedBySlackUserId));
            Assert.That(actualDto.ClaimedByUserId, Is.EqualTo(expectedDto.ClaimedByUserId));
            Assert.That(actualDto.CreatedAt, Is.EqualTo(expectedDto.CreatedAt));
            Assert.That(actualDto.CompletedAt, Is.EqualTo(expectedDto.CompletedAt));
            Assert.That(actualDto.RemovedAt, Is.EqualTo(expectedDto.RemovedAt));
        }
    }
}
