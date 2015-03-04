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
    public class AccountKit
    {
        public IAccountLookup Lookup { get; set; }
        public IAccountQuery Query { get; set; }
        public IRepository<Account> Repository { get; set; } 
    }
}