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
                var htb = new AqiTechTips.HtmlHighlightTextBlock();
                // OnApplyTemplate() would normally trigger Parse(); invoke it directly since
                // no template is applied in this headless test.
                var parseMethod = typeof(AqiTechTips.HtmlHighlightTextBlock)
                    .GetMethod("Parse", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;

                htb.Highlight = "fox";
                parseMethod.Invoke(htb, new object[] { "The quick brown fox jumps" });

                Assert.Contains(htb.Inlines, i => i is Bold);
                var text = new TextRange(htb.ContentStart, htb.ContentEnd).Text;
                Assert.Equal("The quick brown fox jumps", text);
            });
        }
    }
}
