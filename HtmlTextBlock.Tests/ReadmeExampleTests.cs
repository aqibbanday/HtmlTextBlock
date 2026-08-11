using System;
using System.IO;
using System.Windows.Controls;
using System.Windows.Documents;
using AqiTechTips;
using Xunit;

namespace HtmlTextBlock.Tests
{
    /// <summary>
    /// Spot-checks a few of the README's documented examples actually behave as written.
    /// </summary>
    public class ReadmeExampleTests
    {
        [Fact]
        public void BindingTagResolvesPropertyFromDataContext()
        {
            StaThread.Run(() =>
            {
                var tree = new HtmlTagTree();
                new HtmlParser(tree).Parse(new StringReader("Hello, <binding path=\"UserName\" />!"));
                var tb = new TextBlock { DataContext = new { UserName = "Ada" } };
                new HtmlUpdater(tb).Update(tree);

                var text = new TextRange(tb.ContentStart, tb.ContentEnd).Text;
                Assert.Equal("Hello, Ada!", text);
            });
        }

        [Fact]
        public void HighlightTextBlockBoldsMatchingSubstring()
        {
            StaThread.Run(() =>
            {
                var htb = InvokeHighlightParse("The quick brown fox jumps", "fox");

                Assert.Contains(htb.Inlines, i => i is Bold);
                var text = new TextRange(htb.ContentStart, htb.ContentEnd).Text;
                Assert.Equal("The quick brown fox jumps", text);
            });
        }

        [Fact]
        public void HighlightMatchInsideTagAttributeDoesNotCorruptMarkup()
        {
            StaThread.Run(() =>
            {
                var htb = InvokeHighlightParse("<a href=\"http://example.com\">a link</a>", "href");

                var link = Assert.IsType<Hyperlink>(Assert.Single(htb.Inlines));
                Assert.Equal(new Uri("http://example.com"), link.NavigateUri);
                var text = new TextRange(htb.ContentStart, htb.ContentEnd).Text;
                Assert.Equal("a link", text);
            });
        }

        private static AqiTechTips.HtmlHighlightTextBlock InvokeHighlightParse(string html, string highlight)
        {
            var htb = new AqiTechTips.HtmlHighlightTextBlock { Highlight = highlight };
            // OnApplyTemplate() would normally trigger Parse(); invoke it directly since
            // no template is applied in this headless test.
            var parseMethod = typeof(AqiTechTips.HtmlHighlightTextBlock)
                .GetMethod("Parse", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
            parseMethod.Invoke(htb, new object[] { html });
            return htb;
        }
    }
}
