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
    public class UserKit
    {
        public IUserLookup Lookup { get; set; }
        public IUserQuery Query { get; set; }
        public IRepository<Core.Domain.User> Repository { get; set; } 

        public UserKit(IUserLookup lookup, IUserQuery query, IRepository<Core.Domain.User> repository)
        {
            Lookup = lookup;
            Query = query;
            Repository = repository;
        } 
    }
}