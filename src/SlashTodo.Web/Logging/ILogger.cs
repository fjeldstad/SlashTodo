using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlashTodo.Web.Logging
{
    public interface ILogger
    {
        void LogException(Exception ex, Dictionary<string, string> tags = null);
        void LogMessage(string message, LoggerErrorLevel level = LoggerErrorLevel.Info, Dictionary<string, string> tags = null);
    }

    public enum LoggerErrorLevel
    {
        Fatal,
        Error,
        Warning,
        Info,
        Debug
    }
}
