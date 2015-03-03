using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Nancy;
using Nancy.Bootstrapper;
using Nancy.Session;
using Nancy.TinyIoc;
using SlashTodo.Infrastructure;

namespace SlashTodo.Web
{
    public class Bootstrapper : DefaultNancyBootstrapper
    {
        protected override void ApplicationStartup(TinyIoCContainer container, IPipelines pipelines)
        {
            base.ApplicationStartup(container, pipelines);

            // Look for views under the current module folder (if any) primarily.
            Conventions.ViewLocationConventions.Insert(0, (viewName, model, context) =>
            {
                return string.Concat(context.ModuleName, "/Views/", viewName);
            });

            CookieBasedSessions.Enable(pipelines);
        }

        protected override void ConfigureRequestContainer(TinyIoCContainer container, NancyContext context)
        {
            base.ConfigureRequestContainer(container, context);
            container.Register<ISession>(context.Request.Session);
            container.Register<IOAuthState, SessionBasedOAuthState>();
        }
    }
}