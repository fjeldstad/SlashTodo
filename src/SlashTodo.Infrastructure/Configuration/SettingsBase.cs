using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SlashTodo.Infrastructure.Configuration
{
    public abstract class SettingsBase
    {
        private readonly IAppSettings _appSettings;

        protected IAppSettings AppSettings { get { return _appSettings; } }

        protected SettingsBase(IAppSettings appSettings)
        {
            _appSettings = appSettings;
        }
    }
}