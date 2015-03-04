using System;
using System.Threading.Tasks;
using Microsoft.Owin;
using Microsoft.Owin.Extensions;
using Owin;
using SlashTodo.Infrastructure.Configuration;

[assembly: OwinStartup(typeof(SlashTodo.Web.Startup))]

namespace SlashTodo.Web
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.UseNancy(options =>
            {
                options.Bootstrapper = new Bootstrapper(new AppSettings());
            });
            app.UseStageMarker(PipelineStage.MapHandler);
        }
    }
}
