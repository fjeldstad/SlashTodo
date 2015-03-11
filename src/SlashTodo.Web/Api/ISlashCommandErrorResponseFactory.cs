using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nancy;

namespace SlashTodo.Web.Api
{
    public interface ISlashCommandErrorResponseFactory
    {
        Response ActiveAccountNotFound();
        Response InvalidAccountIntegrationSettings();
        Response InvalidSlashCommandToken();
        Response ErrorProcessingCommand();
    }
}
