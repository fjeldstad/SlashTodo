using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlashTodo.Infrastructure.Configuration
{
    public interface IHostSettings
    {
        string ApiBaseUrl { get; }
        int HttpsPort { get; }
    }
}
