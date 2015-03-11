using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Responses;
using Nancy.Security;
using SlashTodo.Core;
using SlashTodo.Core.Queries;
using SlashTodo.Infrastructure;
using SlashTodo.Infrastructure.Configuration;

namespace SlashTodo.Web.Api
{
    public class TodoModule : NancyModule
    {
        public TodoModule(
            ISlashCommandErrorResponseFactory errorResponseFactory,
            IHostSettings hostSettings,
            QueryTeams.IById queryTeamsById,
            QueryUsers.IById queryUsersById,
            IRepository<Core.Domain.User> userRepository,
            ISlashCommandHandler slashCommandHandler)
            : base("/api")
        {
            this.RequiresHttps(redirect: true, httpsPort: hostSettings.HttpsPort);

            Post["/{teamId}", true] = async (_, ct) =>
            {
                var teamId = (string)_.teamId;
                if (!teamId.HasValue())
                {
                    return HttpStatusCode.NotFound;
                }
                var team = await queryTeamsById.ById(teamId);
                if (team == null)
                {
                    return errorResponseFactory.ActiveAccountNotFound();
                }
                var command = this.Bind<SlashCommand.Raw>().ToSlashCommand();
                if (!team.IsActive)
                {
                    return errorResponseFactory.ActiveAccountNotFound();
                }
                if (!string.Equals(team.SlashCommandToken, command.Token))
                {
                    return errorResponseFactory.InvalidSlashCommandToken();
                }
                var userDto = await queryUsersById.ById(command.UserId);
                if (userDto == null)
                {
                    var user = Core.Domain.User.Create(command.UserId, team.Id);
                    user.UpdateName(command.UserName);
                    await userRepository.Save(user); // TODO Await later to reduce response time? Or don't await at all?
                }
                var responseText = await slashCommandHandler.Handle(command);
                if (responseText.HasValue())
                {
                    return new TextResponse(statusCode: HttpStatusCode.OK, contents: responseText);
                }
                return HttpStatusCode.OK;
            };
        }
    }
}