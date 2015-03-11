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

        public static string SubstringByWords(this string value, int startWordIndex, int? numberOfWords = null, params string[] separators)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }
            if (separators == null)
            {
                separators = new[] { " " };
            }
            var words = value.Trim().Split(separators, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length <= startWordIndex)
            {
                return string.Empty;
            }
            var wordsToTake = Math.Min(words.Length - startWordIndex, numberOfWords ?? words.Length);
            return string.Join(" ", words.Skip(startWordIndex).Take(wordsToTake)).Trim();
        }

        public static string[] Words(this string value, params string[] separators)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return new string[0];
            }
            if (separators == null)
            {
                separators = new[] { " " };
            }
            return value.Trim().Split(separators, StringSplitOptions.RemoveEmptyEntries);
        }
    }
}
