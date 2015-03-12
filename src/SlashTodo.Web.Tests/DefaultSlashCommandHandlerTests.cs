using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Moq;
using Nancy;
using NUnit.Framework;
using SlashTodo.Core;
using SlashTodo.Core.Domain;
using SlashTodo.Core.Dtos;
using SlashTodo.Core.Queries;
using SlashTodo.Infrastructure.Slack;
using SlashTodo.Tests.Common;
using SlashTodo.Web.Api;
using SlashTodo.Web.Logging;

namespace SlashTodo.Web.Tests
{
    [TestFixture]
    public class DefaultSlashCommandHandlerTests
    {
        private Mock<IRepository<Core.Domain.Todo>> _todoRepositoryMock;
        private Mock<QueryTodos.IBySlackConversationId> _queryTodosBySlackConversationIdMock;
        private Mock<ISlackIncomingWebhookApi> _incomingWebhookApiMock;
        private Mock<ISlashCommandResponseTexts> _responseTextsMock;
        private Mock<ITodoMessageFormatter> _todoFormatterMock;
        private Mock<ILogger> _loggerMock;
        private DefaultSlashCommandHandler _handler;
        private Uri _incomingWebhookUrl;
        private SlackIncomingWebhookMessage _sentMessage;

        [SetUp]
        public void BeforeEachTest()
        {
            _incomingWebhookUrl = new Uri("https://hooks.slack.com/a/b/c");
            _todoRepositoryMock = new Mock<IRepository<Todo>>();
            _queryTodosBySlackConversationIdMock = new Mock<QueryTodos.IBySlackConversationId>();
            _incomingWebhookApiMock = new Mock<ISlackIncomingWebhookApi>();
            _responseTextsMock = new Mock<ISlashCommandResponseTexts>();
            _todoFormatterMock = new Mock<ITodoMessageFormatter>();
            _loggerMock = new Mock<ILogger>();

            _sentMessage = null;
            _incomingWebhookApiMock
                .Setup(x => x.Send(It.IsAny<Uri>(), It.IsAny<SlackIncomingWebhookMessage>()))
                .Returns(Task.FromResult<object>(null))
                .Callback((Uri u, SlackIncomingWebhookMessage msg) => _sentMessage = msg);

            _handler = new DefaultSlashCommandHandler(
                _todoRepositoryMock.Object, 
                _queryTodosBySlackConversationIdMock.Object,
                _incomingWebhookApiMock.Object, 
                _responseTextsMock.Object,
                _todoFormatterMock.Object,
                _loggerMock.Object);
        }

        [Test]
        public void HandleThrowsOnNullArguments()
        {
            Assert.Throws<ArgumentNullException>(async () => await _handler.Handle(null, It.IsAny<Uri>()));
            Assert.Throws<ArgumentNullException>(async () => await _handler.Handle(new SlashCommand(), null));
        }

        [Test]
        public async Task UnknownCommandTextReturnsCorrectResponseText()
        {
            // Arrange
            var unknownCommand = "unknownCommand";
            _responseTextsMock.Setup(x => x.UnknownCommand(It.IsAny<SlashCommand>())).Returns(unknownCommand);
            var command = GetSlashCommand("trololololol");

            // Act
            var response = await _handler.Handle(command, _incomingWebhookUrl);

            // Assert
            Assert.That(response, Is.EqualTo(unknownCommand));
        }

        [Test]
        public async Task TodoHelpReturnsHelpText()
        {
            // Arrange
            var helpText = "helpText";
            _responseTextsMock.Setup(x => x.UsageInstructions(It.IsAny<SlashCommand>())).Returns(helpText);
            var command = GetSlashCommand("help");

            // Act
            var response = await _handler.Handle(command, _incomingWebhookUrl);

            // Assert
            Assert.That(response, Is.EqualTo(helpText));
        }

        [Test]
        public async Task TodoReturnsConversationTodoList()
        {
            // Arrange
            var command = GetSlashCommand("");
            var todos = new[]
            {
                DtoFactory.Todo(slackConversationId: command.ConversationId)
            };
            _queryTodosBySlackConversationIdMock.Setup(x => x.BySlackConversationId(command.ConversationId)).ReturnsAsync(todos);
            var formattedTodoList = "todoList";
            _todoFormatterMock.Setup(x => x.FormatAsConversationTodoList(It.IsAny<IEnumerable<TodoDto>>()))
                .Returns(formattedTodoList);

            // Act
            var response = await _handler.Handle(command, _incomingWebhookUrl);

            // Assert
            Assert.That(response, Is.EqualTo(formattedTodoList));
        }

        [Test]
        public async Task TodoShowSendsConversationTodoListToIncomingWebhook()
        {
            // Arrange
            var command = GetSlashCommand("show");
            var todos = new[]
            {
                DtoFactory.Todo(slackConversationId: command.ConversationId)
            };
            _queryTodosBySlackConversationIdMock.Setup(x => x.BySlackConversationId(command.ConversationId)).ReturnsAsync(todos);
            var formattedTodoList = "todoList";
            _todoFormatterMock.Setup(x => x.FormatAsConversationTodoList(It.IsAny<IEnumerable<TodoDto>>()))
                .Returns(formattedTodoList);
            
            // Act
            var response = await _handler.Handle(command, _incomingWebhookUrl);

            // Assert
            Assert.That(response, Is.Null);
            _sentMessage.AssertCorrespondsToCommandAndText(command, formattedTodoList);
        }

        [Test]
        public async Task ExceptionThrownWhenSendingToIncomingWebhookIsLoggedAndResultsInErrorMessageBeingReturned()
        {
            // Arrange
            var command = GetSlashCommand("show");
            var todos = new[]
            {
                DtoFactory.Todo(slackConversationId: command.ConversationId)
            };
            _queryTodosBySlackConversationIdMock.Setup(x => x.BySlackConversationId(command.ConversationId)).ReturnsAsync(todos);
            var formattedTodoList = "todoList";
            _todoFormatterMock.Setup(x => x.FormatAsConversationTodoList(It.IsAny<IEnumerable<TodoDto>>()))
                .Returns(formattedTodoList);
            _incomingWebhookApiMock.Setup(x => x.Send(It.IsAny<Uri>(), It.IsAny<SlackIncomingWebhookMessage>())).Throws<HttpRequestException>();
            var errorMessage = "errorMessage";
            _responseTextsMock.Setup(x => x.ErrorSendingToIncomingWebhook()).Returns(errorMessage);

            // Act
            var response = await _handler.Handle(command, _incomingWebhookUrl);

            // Assert
            Assert.That(response, Is.EqualTo(errorMessage));
            _loggerMock.Verify(x => x.LogException(It.IsAny<HttpRequestException>(), It.IsAny<Dictionary<string, string>>()), Times.Once);
        }

        private static SlashCommand GetSlashCommand(string text)
        {
            return new SlashCommand
            {
                TeamId = "teamId",
                TeamDomain = "team",
                ConversationId = "conversationId",
                ConversationName = "conversationName",
                Token = "token",
                UserId = "userId",
                UserName = "userName",
                Command = "command",
                Text = text
            };
        }
    }

    public static class SlackIncomingWebhookMessageExtensions
    {
        public static void AssertCorrespondsToCommandAndText(this SlackIncomingWebhookMessage message, SlashCommand command, string text)
        {
            Assert.That(message, Is.Not.Null);
            Assert.That(message.Text, Is.EqualTo(text));
            Assert.That(message.ConversationId, Is.EqualTo(command.ConversationId));
            Assert.That(message.UnfurlLinks, Is.False);
            Assert.That(message.EnableMarkdown, Is.True);
            Assert.That(message.Attachments, Is.Empty);
            Assert.That(message.ImageUrl, Is.Null);
            Assert.That(message.IconEmoji, Is.Not.Null);
            Assert.That(message.UserName, Is.Not.Null);
            Assert.That(message.IconUrl, Is.Null);
        }
    }
}
