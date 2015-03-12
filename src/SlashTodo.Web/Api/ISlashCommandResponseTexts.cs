using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlashTodo.Web.Api
{
    public interface ISlashCommandResponseTexts
    {
        string ErrorSendingToIncomingWebhook();
        string UnknownCommand(SlashCommand command);
        string UsageInstructions(SlashCommand command);
    }
}
