using System;
using System.Text;
using System.Text.RegularExpressions;

namespace AqiTechTips
{
    /// <summary>
    /// Helpers for building Html markup to feed into HtmlTextBlock/HtmlHighlightTextBlock.
    /// </summary>
    public static class HtmlTextBuilder
    {
        private static readonly Regex whitespaceSplit = new Regex(@"(\s+)", RegexOptions.Compiled);

        /// <summary>
        /// Wraps each word in <paramref name="text"/> in its own &lt;span style="..."&gt; using the
        /// CSS style <paramref name="styleSelector"/> returns for that word (word text, zero-based
        /// word index). Return null or "" from the selector to leave a word unstyled. Whitespace
        /// between words is preserved as-is.
        /// </summary>
        public static string StyleWords(string text, Func<string, int, string> styleSelector)
        {
            if (string.IsNullOrEmpty(text))
                return text;
            if (styleSelector == null)
                throw new ArgumentNullException(nameof(styleSelector));

            StringBuilder result = new StringBuilder(text.Length + 32);
            string[] tokens = whitespaceSplit.Split(text);
            int wordIndex = 0;

            foreach (string token in tokens)
            {
                if (token.Length == 0)
                    continue;

                if (string.IsNullOrWhiteSpace(token))
                {
                    result.Append(token);
                    continue;
                }

                string style = styleSelector(token, wordIndex);
                wordIndex++;

                if (string.IsNullOrEmpty(style))
                    result.Append(Escape(token));
                else
                    result.Append("<span style=\"").Append(EscapeAttribute(style)).Append("\">").Append(Escape(token)).Append("</span>");
            }

            return result.ToString();
        }

        private static string Escape(string text)
        {
            return text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
        }

        private static string EscapeAttribute(string value)
        {
            return value.Replace("&", "&amp;").Replace("\"", "&quot;").Replace("<", "&lt;").Replace(">", "&gt;");
        }
    }
}
