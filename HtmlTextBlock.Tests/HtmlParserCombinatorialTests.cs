using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AqiTechTips;
using Xunit;

namespace HtmlTextBlock.Tests
{
    /// <summary>
    /// A broader sweep of tag/quote/comment/self-closing combinations for HtmlParser. Added
    /// after three consecutive review passes each found a regression in the quote-aware tag
    /// boundary scan (FindTagEnd) - each fix for one edge case broke another (">" inside a
    /// quoted value, an apostrophe inside a quoted value, a self-closing slash right after a
    /// quote, an apostrophe inside comment text). This sweep exists to catch the next one.
    /// </summary>
    public class HtmlParserCombinatorialTests
    {
        /// <summary>
        /// Snippets the parser is expected to fully recover from: parsing must resynchronize
        /// by the time it reaches trailing content, so a canary tag appended after each one
        /// must come through completely intact and unmerged with anything before it.
        /// </summary>
        public static IEnumerable<object[]> RealisticSnippets => new List<object[]>
        {
            new object[] { "<a href=\"x.com\">link</a>" },
            new object[] { "<a href='x.com'>link</a>" },
            new object[] { "<a title=\"5 > 3\" href=\"x.com\">link</a>" },
            new object[] { "<a title='5 > 3' href='x.com'>link</a>" },
            new object[] { "<a title=\"it's ok\" href=\"x.com\">link</a>" },
            new object[] { "<img src=\"x.png\"/>" },
            new object[] { "<img src='x.png'/>" },
            new object[] { "<br/>" },
            new object[] { "<br />" },
            new object[] { "<hr>" },
            new object[] { "<!-- plain comment -->" },
            new object[] { "<!-- comment with apostrophe don't -->" },
            new object[] { "<!-- comment with quote \"hi\" -->" },
            new object[] { "<!-- comment with both it's \"here\" -->" },
            new object[] { "<span style=\"color:red;font-weight:bold\">styled</span>" },
            new object[] { "<span style='color:red'>styled</span>" },
            new object[] { "<font color=\"red\" face=\"Arial\" size=\"12\">x</font>" },
            new object[] { "[b]legacy[/b]" },
            new object[] { "<b>real</b> and [i]legacy[/i]" },
            new object[] { "<a href=\"http://example.com/path?a=1&b=2\">link</a>" },
            new object[] { "<div data-value=\"a/b/c\">x</div>" },
            new object[] { "<input type=\"text\" value=\"don't\" disabled>" },
            new object[] { "<a href=\"x.com\" title=\"a 'quoted' word\">link</a>" },
            new object[] { "<a href='x.com' title=\"a 'quoted' word\">link</a>" },
            new object[] { "<!DOCTYPE html>" },
            new object[] { "" },
            new object[] { "plain text with no tags at all" },
        };

        [Theory]
        [MemberData(nameof(RealisticSnippets))]
        public void ParserFullyRecoversBeforeTrailingCanaryTag(string prefix)
        {
            string html = prefix + "<em>CANARY123</em>";

            var tree = new HtmlTagTree();
            new HtmlParser(tree).Parse(new StringReader(html));
            var tags = tree.ToHtmlTagList().Skip(1).ToList();

            Assert.True(tags.Count >= 3, "Expected at least the canary's 3 tags; got: " + string.Join(", ", tags.Select(t => t.Name)));
            var last3 = tags.Skip(tags.Count - 3).ToList();
            Assert.Equal("em", last3[0].Name);
            Assert.Equal("text", last3[1].Name);
            Assert.Equal("CANARY123", last3[1]["value"]);
            Assert.Equal("/em", last3[2].Name);
        }

        /// <summary>
        /// Pathological inputs with no unambiguous interpretation - what matters here is safe
        /// degradation: parsing completes (no exception, no hang) and produces a finite,
        /// reasonable number of tags, rather than corrupting output or looping forever.
        /// </summary>
        public static IEnumerable<object[]> PathologicalSnippets => new List<object[]>
        {
            new object[] { "<a title=\"unterminated" },
            new object[] { "<!-- unterminated comment" },
            new object[] { "<div title='''''''>x</div>" },
            new object[] { "<div title=\"\"\"\"\">x</div>" },
            new object[] { "<><><>" },
            new object[] { "<<<<<<<" },
            new object[] { ">>>>>>>" },
            new object[] { "<a href=\"x\"><a href=\"y\"><a href=\"z\">" },
            new object[] { new string('<', 500) },
            new object[] { "<div title=\"" + new string('a', 1000) + "\">x</div>" },
        };

        [Theory]
        [MemberData(nameof(PathologicalSnippets))]
        public void PathologicalInputDegradesSafelyWithoutThrowingOrHanging(string html)
        {
            var tree = new HtmlTagTree();
            var ex = Record.Exception(() => new HtmlParser(tree).Parse(new StringReader(html)));

            Assert.Null(ex);
            var tags = tree.ToHtmlTagList();
            Assert.True(tags.Count < 1000, "Tag count exploded unexpectedly: " + tags.Count);
        }
    }
}
