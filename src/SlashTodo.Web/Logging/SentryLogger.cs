using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using SharpRaven;
using SharpRaven.Data;

namespace SlashTodo.Web.Logging
{
    public class SentryLogger : ILogger
    {
        private readonly IRavenClient _sentryClient;

        public SentryLogger(IRavenClient sentryClient)
        {
            _sentryClient = sentryClient;
        }

        public void LogException(
            Exception ex, 
            Dictionary<string, string> tags = null)
        {
            _sentryClient.CaptureException(ex, tags: tags);
        }

        public void LogMessage(
            string message, 
            LoggerErrorLevel level = LoggerErrorLevel.Info, 
            Dictionary<string, string> tags = null)
        {
            _sentryClient.CaptureMessage(new SentryMessage(message), level: GetErrorLevel(level), tags: tags);
        }

        private static ErrorLevel GetErrorLevel(LoggerErrorLevel value)
        {
            switch (value)
            {
                case LoggerErrorLevel.Fatal:
                    return ErrorLevel.Fatal;
                case LoggerErrorLevel.Error:
                    return ErrorLevel.Error;
                case LoggerErrorLevel.Warning:
                    return ErrorLevel.Warning;
                case LoggerErrorLevel.Debug:
                    return ErrorLevel.Debug;
                default:
                    return ErrorLevel.Info;
            }
        }
    }
}