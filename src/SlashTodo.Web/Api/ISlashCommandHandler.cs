using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlashTodo.Web.Api
{
    public interface ISlashCommandHandler
    {
        Task<string> Handle(SlashCommand command);
    }
}
