using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlashTodo.Infrastructure
{
    public static class StringExtensions
    {
        public static bool HasValue(this string @string)
        {
            return !string.IsNullOrWhiteSpace(@string);
        }
    }
}
