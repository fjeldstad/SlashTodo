using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace SlashTodo.Web.Api
{
    public class DefaultSlashCommandHandler : ISlashCommandHandler
    {
        public Task<string> Handle(SlashCommand command)
        {
            return Task.FromResult("*TODO*");
        }
    }
}