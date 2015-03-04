using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using SlashTodo.Core;
using SlashTodo.Core.Domain;
using SlashTodo.Web.Lookups;
using SlashTodo.Web.Queries;

namespace SlashTodo.Web
{
    public class UserKit
    {
        public IUserLookup Lookup { get; set; }
        public IUserQuery Query { get; set; }
        public IRepository<User> Repository { get; set; } 
    }
}