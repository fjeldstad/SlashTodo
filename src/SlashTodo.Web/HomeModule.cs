using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.ModelBinding;
using Nancy;
using SlashTodo.Infrastructure;
using SlashTodo.Infrastructure.Configuration;
using SlashTodo.Web.ViewModels;
using HttpUtility = Nancy.Helpers.HttpUtility;

namespace SlashTodo.Web
{
    public class HomeModule : NancyModule
    {
        public HomeModule(IViewModelFactory viewModelFactory)
        {
            Get["/"] = _ =>
            {
                return View["Start.cshtml", viewModelFactory.Create<EmptyViewModel>()];
            };
        }
    }
}