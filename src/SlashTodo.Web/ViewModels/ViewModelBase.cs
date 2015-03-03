using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SlashTodo.Web.ViewModels
{
    public abstract class ViewModelBase
    {
        public virtual string Title { get { return "SlashTodo"; } }
    }
}