using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using SlashTodo.Core;
using SlashTodo.Core.Domain;
using SlashTodo.Infrastructure.Slack;
using SlashTodo.Web.Api;
using SlashTodo.Web.Logging;

namespace SlashTodo.Web.Tests
{
    [TestFixture]
    public class DefaultSlashCommandHandlerTests
    {
        private Mock<IRepository<Core.Domain.Todo>> _todoRepositoryMock;
        private Mock<ISlackIncomingWebhookApi> _incomingWebhookApiMock;
        private Mock<ISlashCommandResponseTexts> _responseTextsMock;
        private Mock<ILogger> _loggerMock;
        private DefaultSlashCommandHandler _handler;
        private Uri _incomingWebhookUrl;

        [SetUp]
        public void BeforeEachTest()
        {
            _incomingWebhookUrl = new Uri("https://hooks.slack.com/a/b/c");
            _todoRepositoryMock = new Mock<IRepository<Todo>>();
            _incomingWebhookApiMock = new Mock<ISlackIncomingWebhookApi>();
            _responseTextsMock = new Mock<ISlashCommandResponseTexts>();
            _loggerMock = new Mock<ILogger>();
            _handler = new DefaultSlashCommandHandler(
                _todoRepositoryMock.Object, 
                _incomingWebhookApiMock.Object, 
                _responseTextsMock.Object,
                _loggerMock.Object);
        }

        [Test]
        public void HandleThrowsOnNullArguments()
        {
            Assert.Throws<ArgumentNullException>(async () => await _handler.Handle(null, It.IsAny<Uri>()));
            Assert.Throws<ArgumentNullException>(async () => await _handler.Handle(new SlashCommand(), null));
        }

        [Test]
        public void ExceptionThrownWhenSendingToIncomingWebhookIsLoggedAndResultsInErrorMessageBeingReturned()
        {
            throw new NotImplementedException();
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
        public async Task HelpReturnsHelpText()
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
}
