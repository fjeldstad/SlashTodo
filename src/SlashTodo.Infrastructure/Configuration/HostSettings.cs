using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlashTodo.Infrastructure.Configuration
{
    public class HostSettings : SettingsBase, IHostSettings
    {
        public string ApiBaseUrl { get { return AppSettings.Get("host:ApiBaseUrl"); } }
        public int HttpsPort { get { return int.Parse(AppSettings.Get("host:HttpsPort")); } }

        public HostSettings(IAppSettings appSettings)
            : base(appSettings)
        {
        }
    }
}
