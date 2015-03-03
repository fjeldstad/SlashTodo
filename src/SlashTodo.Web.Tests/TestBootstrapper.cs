using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nancy.Bootstrapper;
using Nancy.Session;
using Nancy.Testing;
using Nancy.TinyIoc;

namespace SlashTodo.Web.Tests
{
    public class TestBootstrapper : ConfigurableBootstrapper
    {
        public TestBootstrapper(Action<ConfigurableBootstrapperConfigurator> config)
            : base(config)
        {
        }

        protected override void ApplicationStartup(TinyIoCContainer container, IPipelines pipelines)
        {
            base.ApplicationStartup(container, pipelines);

            // Look for views under the current module folder (if any) primarily.
            Conventions.ViewLocationConventions.Insert(0, (viewName, model, context) =>
            {
                return string.Concat(context.ModuleName, "/Views/", viewName);
            });
        }
    }
}
