using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SlashTodo.Web.Logging
{
    public class NullLogger : ILogger
    {
        public void LogException(
            Exception ex, 
            Dictionary<string, string> tags = null)
        {
        }

        public void LogMessage(
            string message, 
            LoggerErrorLevel level = LoggerErrorLevel.Info, 
            Dictionary<string, string> tags = null)
        {
        }
    }
}