using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;

namespace SlashTodo.Infrastructure.Configuration
{
    public class AppSettings : IAppSettings
    {
        public string Get(string key)
        {
            return ConfigurationManager.AppSettings[key];
        }
    }
}