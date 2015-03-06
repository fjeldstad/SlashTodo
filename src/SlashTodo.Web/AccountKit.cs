using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using SlashTodo.Core;
using SlashTodo.Core.Domain;
using SlashTodo.Core.Lookups;
using SlashTodo.Core.Queries;

namespace SlashTodo.Web
{
    public class AccountKit
    {
        public IAccountLookup Lookup { get; set; }
        public IAccountQuery Query { get; set; }
        public IRepository<Core.Domain.Account> Repository { get; set; }

        public AccountKit(IAccountLookup lookup, IAccountQuery query, IRepository<Core.Domain.Account> repository)
        {
            Lookup = lookup;
            Query = query;
            Repository = repository;
        } 
    }
}