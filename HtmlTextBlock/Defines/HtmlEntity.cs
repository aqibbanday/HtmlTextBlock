using System;
using System.Collections.Generic;
using System.Text;

namespace AqiTechTips
{
    /// <summary>
    /// Decodes HTML character references (&amp;amp;, &amp;#169;, &amp;#xA9;) in text content.
    /// </summary>
    internal static class HtmlEntity
    {
        private static readonly Dictionary<string, char> named = new Dictionary<string, char>
        {
            { "amp", '&' }, { "lt", '<' }, { "gt", '>' }, { "quot", '"' }, { "apos", '\'' },
            { "nbsp", ' ' }, { "copy", '©' }, { "reg", '®' }, { "trade", '™' },
            { "mdash", '—' }, { "ndash", '–' }, { "hellip", '…' },
            { "lsquo", '‘' }, { "rsquo", '’' }, { "ldquo", '“' }, { "rdquo", '”' },
        };

        public static string Decode(string input)
        {
            if (string.IsNullOrEmpty(input) || input.IndexOf('&') == -1)
                return input;

            StringBuilder result = new StringBuilder(input.Length);
            int i = 0;
            while (i < input.Length)
            {
                char c = input[i];
                if (c == '&')
                {
                    int semi = input.IndexOf(';', i + 1);
                    if (semi != -1 && semi - i <= 12)
                    {
                        string entity = input.Substring(i + 1, semi - i - 1);
                        char? decoded = DecodeEntity(entity);
                        if (decoded.HasValue)
                        {
                            result.Append(decoded.Value);
                            i = semi + 1;
                            continue;
                        }
                    }
                }
                result.Append(c);
                i++;
            }
            return result.ToString();
        }

        private static char? DecodeEntity(string entity)
        {
            if (entity.Length == 0)
                return null;

            if (entity[0] == '#')
            {
                try
                {
                    int code = (entity.Length > 1 && (entity[1] == 'x' || entity[1] == 'X'))
                        ? Convert.ToInt32(entity.Substring(2), 16)
                        : Convert.ToInt32(entity.Substring(1));
                    return (char)code;
                }
                catch
                {
                    return null;
                }
            }

            char value;
            if (named.TryGetValue(entity, out value))
                return value;

            return null;
        }
    }
}
