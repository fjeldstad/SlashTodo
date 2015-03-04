using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlashTodo.Infrastructure.Configuration
{
    public class SlackSettings : SettingsBase, ISlackSettings
    {
        public string ClientId { get { return AppSettings.Get("slack:ClientId"); } }
        public string ClientSecret { get { return AppSettings.Get("slack:ClientSecret"); } }
        public Uri OAuthAuthorizationUrl { get { return new Uri(AppSettings.Get("slack:OAuthAuthorizationUrl")); } }
        public Uri OAuthRedirectUrl { get { return new Uri(AppSettings.Get("slack:OAuthRedirectUrl")); } }
        public string OAuthScope { get { return AppSettings.Get("slack:OAuthScope"); } }
        public string ApiBaseUrl { get { return AppSettings.Get("slack:ApiBaseUrl").TrimEnd('/'); } }

        public SlackSettings(IAppSettings appSettings)
            : base(appSettings)
        {
        }
    }
}
