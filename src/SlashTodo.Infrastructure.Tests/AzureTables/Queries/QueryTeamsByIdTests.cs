using System;
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
    public class QueryTeamsByIdTests
    {
        private readonly CloudStorageAccount _storageAccount = CloudStorageAccount.Parse((new AzureSettings(new AppSettings())).StorageConnectionString);
        private string _tableName;
        private QueryTeamsById _queryTeamsById;
        private IMessageBus _bus;
            
        [SetUp]
        public void BeforeEachTest()
        {
            _bus = new TinyMessageBus(new TinyMessengerHub());
            // Reference a different table for each test to ensure isolation.
            _queryTeamsById = new QueryTeamsById(
                _storageAccount,
                string.Format("test{0}", Guid.NewGuid().ToString("N")));
            _queryTeamsById.RegisterSubscriptions((ISubscriptionRegistry)_bus);
        }

        [TearDown]
        public void AfterEachTest()
        {
            // Delete the table used by the test that just finished running.
            // Note that according to MSDN, deleting a table can take minutes,
            // so we won't hang around waiting for the result.
            var table = _storageAccount.GetTable(_queryTeamsById.TableName);
            table.DeleteIfExists();
            _queryTeamsById.Dispose();
        }

        [Test]
        public async Task InsertsNewRowOnTeamCreated()
        {
            // Arrange
            var teamCreated = new TeamCreated
            {
                Id = "id",
                Timestamp = DateTime.UtcNow
            };

            // Act
            await _bus.Publish(teamCreated);

            // Assert
            var table = _storageAccount.GetTable(_queryTeamsById.TableName);
            var row = table.Retrieve<TeamDtoTableEntity>(teamCreated.Id, teamCreated.Id);
            Assert.That(row, Is.Not.Null);
            Assert.That(row.Id, Is.EqualTo(teamCreated.Id));
            Assert.That(row.CreatedAt, Is.EqualTo(teamCreated.Timestamp));
        }

        [Test]
        public async Task UpdatesRowOnTeamInfoUpdated()
        {
            // Arrange
            var dto = DtoFactory.Team(name: "oldSlackTeamName", slackUrl: new Uri("https://oldteam.slack.com"));
            var table = _storageAccount.GetTable(_queryTeamsById.TableName);
            table.Insert(new TeamDtoTableEntity(dto));
            var teamInfoUpdated = new TeamInfoUpdated
            {
                Id = dto.Id,
                Name = "newSlackTeamName",
                SlackUrl = new Uri("https://team.slack.com")
            };

            // Act
            await _bus.Publish(teamInfoUpdated);

            // Assert
            var row = table.Retrieve<TeamDtoTableEntity>(dto.Id, dto.Id);
            Assert.That(row, Is.Not.Null);
            Assert.That(row.Name, Is.EqualTo(teamInfoUpdated.Name));
            Assert.That(row.SlackUrl, Is.EqualTo(teamInfoUpdated.SlackUrl.AbsoluteUri));
        }

        [Test]
        public async Task UpdatesRowOnTeamSlashCommandTokenUpdated()
        {
            // Arrange
            var dto = DtoFactory.Team(slashCommandToken: "oldSlashCommandToken");
            var table = _storageAccount.GetTable(_queryTeamsById.TableName);
            table.Insert(new TeamDtoTableEntity(dto));
            var teamSlashCommandTokenUpdated = new TeamSlashCommandTokenUpdated
            {
                Id = dto.Id,
                SlashCommandToken = "newSlashCommandToken"
            };

            // Act
            await _bus.Publish(teamSlashCommandTokenUpdated);

            // Assert
            var row = table.Retrieve<TeamDtoTableEntity>(dto.Id, dto.Id);
            Assert.That(row, Is.Not.Null);
            Assert.That(row.SlashCommandToken, Is.EqualTo(teamSlashCommandTokenUpdated.SlashCommandToken));
        }

        [Test]
        public async Task UpdatesRowOnTeamIncomingWebhookUrlUpdated()
        {
            // Arrange
            var dto = DtoFactory.Team(incomingWebhookUrl: new Uri("https://api.slack.com/old-incoming-webhook"));
            var table = _storageAccount.GetTable(_queryTeamsById.TableName);
            table.Insert(new TeamDtoTableEntity(dto));
            var teamIncomingWebhookUrlUpdated = new TeamIncomingWebhookUpdated
            {
                Id = dto.Id,
                IncomingWebhookUrl = new Uri("https://api.slack.com/new-incoming-webhook")
            };

            // Act
            await _bus.Publish(teamIncomingWebhookUrlUpdated);

            // Assert
            var row = table.Retrieve<TeamDtoTableEntity>(dto.Id, dto.Id);
            Assert.That(row, Is.Not.Null);
            Assert.That(row.IncomingWebhookUrl, Is.EqualTo(teamIncomingWebhookUrlUpdated.IncomingWebhookUrl));
        }

        [Test]
        public async Task UpdatesRowOnTeamActivated()
        {
            // Arrange
            var dto = DtoFactory.Team(activatedAt: null);
            var table = _storageAccount.GetTable(_queryTeamsById.TableName);
            table.Insert(new TeamDtoTableEntity(dto));
            var teamActivated = new TeamActivated
            {
                Id = dto.Id,
                Timestamp = DateTime.UtcNow
            };

            // Act
            await _bus.Publish(teamActivated);

            // Assert
            var row = table.Retrieve<TeamDtoTableEntity>(dto.Id, dto.Id);
            Assert.That(row, Is.Not.Null);
            Assert.That(row.IsActive, Is.True);
        }

        [Test]
        public async Task UpdatesRowOnTeamDeactivated()
        {
            // Arrange
            var dto = DtoFactory.Team(activatedAt: DateTime.UtcNow.AddDays(-1));
            var table = _storageAccount.GetTable(_queryTeamsById.TableName);
            table.Insert(new TeamDtoTableEntity(dto));
            var accountDeactivated = new TeamDeactivated
            {
                Id = dto.Id,
                Timestamp = DateTime.UtcNow
            };

            // Act
            await _bus.Publish(accountDeactivated);

            // Assert
            var row = table.Retrieve<TeamDtoTableEntity>(dto.Id, dto.Id);
            Assert.That(row, Is.Not.Null);
            Assert.That(row.IsActive, Is.False);
        }

        [Test]
        public async Task ByIdReturnsNullWhenQueryFails()
        {
            // Arrange

            // Act
            var team = await _queryTeamsById.ById("whatever");

            // Assert
            Assert.That(team, Is.Null);
        }

        [Test]
        public async Task ByIdReturnsDtoWhenQueryIsSuccessful()
        {
            // Arrange
            var expectedDto = DtoFactory.Team(activatedAt: DateTime.UtcNow.AddDays(-1));
            var table = _storageAccount.GetTable(_queryTeamsById.TableName);
            table.Insert(new TeamDtoTableEntity(expectedDto));

            // Act
            var accountDto = await _queryTeamsById.ById(expectedDto.Id);

            // Assert
            accountDto.AssertIsEqualTo(expectedDto);
        }
    }
    public static class TeamDtoExtensions
    {
        public static void AssertIsEqualTo(this TeamDto actualDto, TeamDto expectedDto)
        {
            Assert.That(actualDto, Is.Not.Null);
            Assert.That(actualDto.Id, Is.EqualTo(expectedDto.Id));
            Assert.That(actualDto.Name, Is.EqualTo(expectedDto.Name));
            Assert.That(actualDto.SlackUrl, Is.EqualTo(expectedDto.SlackUrl));
            Assert.That(actualDto.SlashCommandToken, Is.EqualTo(expectedDto.SlashCommandToken));
            Assert.That(actualDto.IncomingWebhookUrl, Is.EqualTo(expectedDto.IncomingWebhookUrl));
            Assert.That(actualDto.CreatedAt, Is.EqualTo(expectedDto.CreatedAt));
            Assert.That(actualDto.ActivatedAt, Is.EqualTo(expectedDto.ActivatedAt));
        }
    }
}
