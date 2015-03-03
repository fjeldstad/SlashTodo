using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nancy;

namespace SlashTodo.Web.Tests
{
    public class TestRootPathProvider : IRootPathProvider
    {
        private static readonly string RootPath;

        static TestRootPathProvider()
        {
            var directoryName = Path.GetDirectoryName(typeof(Bootstrapper).Assembly.CodeBase);

            if (directoryName != null)
            {
                var assemblyPath = directoryName.Replace(@"file:\", string.Empty);

                RootPath = Path.Combine(assemblyPath, "..", "..", "..", "SlashTodo.Web");
            }
        }

        public string GetRootPath()
        {
            return RootPath;
        }
    }
}
