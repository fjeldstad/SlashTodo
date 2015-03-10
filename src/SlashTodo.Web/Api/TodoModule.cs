using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Security;
using SlashTodo.Core.Queries;
using SlashTodo.Infrastructure.Configuration;

namespace SlashTodo.Web.Api
{
    public class TodoModule : NancyModule
    {
        public TodoModule(
            ISlashCommandErrorResponseFactory errorResponseFactory,
            IHostSettings hostSettings,
            IQueryTeamsById queryTeamsById,
            UserKit userKit)
            : base("/api")
        {
            this.RequiresHttps(redirect: true, httpsPort: hostSettings.HttpsPort);

            Post["/{accountId}", true] = async (_, ct) =>
            {
                Guid accountId;
                if (!Guid.TryParse((string)_.accountId, out accountId))
                {
                    return HttpStatusCode.NotFound;
                }
                var account = await queryTeamsById.Query(accountId);
                if (account == null)
                {
                    return errorResponseFactory.ActiveAccountNotFound();
                }
                var command = this.Bind<SlackSlashCommand>();
                if (!string.Equals(account.SlackTeamId, command.TeamId, StringComparison.Ordinal))
                {
                    return errorResponseFactory.InvalidAccountIntegrationSettings();
                }
                if (!account.IsActive)
                {
                    return errorResponseFactory.ActiveAccountNotFound();
                }
                if (!string.Equals(account.SlashCommandToken, command.Token))
                {
                    return errorResponseFactory.InvalidSlashCommandToken();
                }
                var userId = await userKit.Lookup.BySlackUserId(command.UserId);
                if (!userId.HasValue)
                {
                    var user = Core.Domain.User.Create(Guid.NewGuid(), account.Id, command.UserId);
                    user.UpdateName(command.UserName);
                    await userKit.Repository.Save(user); // TODO Await later to reduce response time? Or don't await at all?
                }
                throw new NotImplementedException();
            };
        }
    }
}