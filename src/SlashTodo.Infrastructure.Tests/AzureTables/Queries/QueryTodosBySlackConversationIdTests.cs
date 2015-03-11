using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Moq;
using NUnit.Framework;
using SlashTodo.Core.Domain;
using SlashTodo.Core.Dtos;
using SlashTodo.Infrastructure.AzureTables;
using SlashTodo.Infrastructure.AzureTables.Queries.Entities;
using SlashTodo.Infrastructure.Configuration;
using SlashTodo.Infrastructure.Messaging;
using SlashTodo.Infrastructure.AzureTables.Queries;
using SlashTodo.Tests.Common;
using TinyMessenger;

namespace SlashTodo.Infrastructure.Tests.AzureTables.Queries
{
    [TestFixture]
    public class QueryTodosBySlackConversationIdTests
    {
        private readonly CloudStorageAccount _storageAccount = CloudStorageAccount.Parse((new AzureSettings(new AppSettings())).StorageConnectionString);
        private QueryTodosBySlackConversationId _query;
        private IMessageBus _bus;
            
        [SetUp]
        public void BeforeEachTest()
        {
            _bus = new TinyMessageBus(new TinyMessengerHub());
            // Reference a different table for each test to ensure isolation.
            _query = new QueryTodosBySlackConversationId(
                _storageAccount,
                string.Format("test{0}", Guid.NewGuid().ToString("N")));
            _query.RegisterSubscriptions((ISubscriptionRegistry)_bus);
        }

        [TearDown]
        public void AfterEachTest()
        {
            // Delete the table used by the test that just finished running.
            // Note that according to MSDN, deleting a table can take minutes,
            // so we won't hang around waiting for the result.
            var table = _storageAccount.GetTable(_query.TableName);
            table.DeleteIfExists();
            _query.Dispose();
        }

        [Test]
        public async Task InsertsNewRowOnTodoAdded()
        {
            // Arrange
            var todoAdded = new TodoAdded
            {
                Id = "id",
                SlackConversationId = "conversationId",
                TeamId = "teamId",
                ShortCode = "shortCode",
                Text = "text",
                Timestamp = DateTime.UtcNow
            };

            // Act
            await _bus.Publish(todoAdded);

            // Assert
            var table = _storageAccount.GetTable(_query.TableName);
            var row = table.Retrieve<TodoDtoTableEntity>(todoAdded.SlackConversationId, todoAdded.ShortCode);
            Assert.That(row, Is.Not.Null);
            Assert.That(row.Id, Is.EqualTo(todoAdded.Id));
            Assert.That(row.SlackConversationId, Is.EqualTo(todoAdded.SlackConversationId));
            Assert.That(row.TeamId, Is.EqualTo(todoAdded.TeamId));
            Assert.That(row.ShortCode, Is.EqualTo(todoAdded.ShortCode));
            Assert.That(row.Text, Is.EqualTo(todoAdded.Text));
            Assert.That(row.CreatedAt, Is.EqualTo(todoAdded.Timestamp));
        }

        [Test]
        public async Task DeletesRowOnTodoRemoved()
        {
            // Arrange
            var dto = DtoFactory.Todo();
            var table = _storageAccount.GetTable(_query.TableName);
            table.Insert(new TodoDtoTableEntity(dto, x => x.SlackConversationId, x => x.ShortCode));
            var todoRemoved = new TodoRemoved
            {
                Id = dto.Id,
                SlackConversationId = dto.SlackConversationId,
                ShortCode = dto.ShortCode,
                Timestamp = DateTime.UtcNow
            };

            // Act
            await _bus.Publish(todoRemoved);

            // Assert
            var row = table.Retrieve<TodoDtoTableEntity>(dto.SlackConversationId, dto.ShortCode);
            Assert.That(row, Is.Null);
        }

        [Test]
        public async Task UpdatesRowOnTodoTicked()
        {
            // Arrange
            var dto = DtoFactory.Todo(claimedByUserId: "userId");
            var table = _storageAccount.GetTable(_query.TableName);
            table.Insert(new TodoDtoTableEntity(dto, x => x.SlackConversationId, x => x.ShortCode));
            var todoTicked = new TodoTicked
            {
                Id = dto.Id,
                SlackConversationId = dto.SlackConversationId,
                ShortCode = dto.ShortCode,
                Timestamp = DateTime.UtcNow
            };

            // Act
            await _bus.Publish(todoTicked);

            // Assert
            var row = table.Retrieve<TodoDtoTableEntity>(dto.SlackConversationId, dto.ShortCode);
            Assert.That(row, Is.Not.Null);
            Assert.That(row.CompletedAt, Is.EqualTo(todoTicked.Timestamp));
            Assert.That(row.ClaimedByUserId, Is.Null);
        }

        [Test]
        public async Task UpdatesRowOnTodoUnticked()
        {
            // Arrange
            var dto = DtoFactory.Todo(completedAt: DateTime.UtcNow);
            var table = _storageAccount.GetTable(_query.TableName);
            table.Insert(new TodoDtoTableEntity(dto, x => x.SlackConversationId, x => x.ShortCode));
            var todoUnticked = new TodoUnticked
            {
                Id = dto.Id,
                SlackConversationId = dto.SlackConversationId,
                ShortCode = dto.ShortCode,
                Timestamp = DateTime.UtcNow
            };

            // Act
            await _bus.Publish(todoUnticked);

            // Assert
            var row = table.Retrieve<TodoDtoTableEntity>(dto.SlackConversationId, dto.ShortCode);
            Assert.That(row, Is.Not.Null);
            Assert.That(row.CompletedAt, Is.Null);
        }

        [Test]
        public async Task UpdatesRowOnTodoClaimed()
        {
            // Arrange
            var dto = DtoFactory.Todo();
            var table = _storageAccount.GetTable(_query.TableName);
            table.Insert(new TodoDtoTableEntity(dto, x => x.SlackConversationId, x => x.ShortCode));
            var todoClaimed = new TodoClaimed
            {
                Id = dto.Id,
                SlackConversationId = dto.SlackConversationId,
                ShortCode = dto.ShortCode,
                Timestamp = DateTime.UtcNow,
                UserId = "userId"
            };

            // Act
            await _bus.Publish(todoClaimed);

            // Assert
            var row = table.Retrieve<TodoDtoTableEntity>(dto.SlackConversationId, dto.ShortCode);
            Assert.That(row, Is.Not.Null);
            Assert.That(row.ClaimedByUserId, Is.EqualTo(todoClaimed.UserId));
        }

        [Test]
        public async Task UpdatesRowOnTodoFreed()
        {
            // Arrange
            var dto = DtoFactory.Todo(claimedByUserId: "userId");
            var table = _storageAccount.GetTable(_query.TableName);
            table.Insert(new TodoDtoTableEntity(dto, x => x.SlackConversationId, x => x.ShortCode));
            var todoFreed = new TodoFreed
            {
                Id = dto.Id,
                SlackConversationId = dto.SlackConversationId,
                ShortCode = dto.ShortCode,
                Timestamp = DateTime.UtcNow
            };

            // Act
            await _bus.Publish(todoFreed);

            // Assert
            var row = table.Retrieve<TodoDtoTableEntity>(dto.SlackConversationId, dto.ShortCode);
            Assert.That(row, Is.Not.Null);
            Assert.That(row.ClaimedByUserId, Is.Null);
        }

        [Test]
        public async Task BySlackConversationIdReturnsEmpltyWhenQueryFails()
        {
            // Arrange

            // Act
            var todos = await _query.BySlackConversationId("whatever");

            // Assert
            Assert.That(todos, Is.Empty);
        }

        [Test]
        public async Task BySlackConversationIdReturnsDtosWhenQueryIsSuccessful()
        {
            // Arrange
            var conversationId = "conversationId";
            var otherConversationId = "otherConversationId";
            var dtos = new[]
            {
                DtoFactory.Todo(id: "id1", slackConversationId: conversationId),
                DtoFactory.Todo(id: "id2", slackConversationId: otherConversationId)
            };
            var table = _storageAccount.GetTable(_query.TableName);
            foreach (var dto in dtos)
            {
                table.Insert(new TodoDtoTableEntity(dto, x => x.SlackConversationId, x => x.ShortCode));
            }

            // Act
            var todos = await _query.BySlackConversationId(conversationId);

            // Assert
            todos.Single().AssertIsEqualTo(dtos.First());
        }
    }
    public static class TodoDtoExtensions
    {
        public static void AssertIsEqualTo(this TodoDto actualDto, TodoDto expectedDto)
        {
            Assert.That(actualDto, Is.Not.Null);
            Assert.That(actualDto.Id, Is.EqualTo(expectedDto.Id));
            Assert.That(actualDto.SlackConversationId, Is.EqualTo(expectedDto.SlackConversationId));
            Assert.That(actualDto.ShortCode, Is.EqualTo(expectedDto.ShortCode));
            Assert.That(actualDto.TeamId, Is.EqualTo(expectedDto.TeamId));
            Assert.That(actualDto.Text, Is.EqualTo(expectedDto.Text));
            Assert.That(actualDto.ClaimedByUserId, Is.EqualTo(expectedDto.ClaimedByUserId));
            Assert.That(actualDto.CreatedAt, Is.EqualTo(expectedDto.CreatedAt));
            Assert.That(actualDto.CompletedAt, Is.EqualTo(expectedDto.CompletedAt));
            Assert.That(actualDto.RemovedAt, Is.EqualTo(expectedDto.RemovedAt));
        }
    }
}
