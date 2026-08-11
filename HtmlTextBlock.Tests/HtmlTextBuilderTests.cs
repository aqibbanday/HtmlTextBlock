using System;
using System.IO;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using AqiTechTips;
using Xunit;

namespace HtmlTextBlock.Tests
{
    public class HtmlTextBuilderTests
    {
        [Fact]
        public void WrapsEachWordWithItsOwnStyleAndPreservesWhitespace()
        {
            string html = HtmlTextBuilder.StyleWords("one two three", (word, i) => $"color:red");

            Assert.Equal(
                "<span style=\"color:red\">one</span> <span style=\"color:red\">two</span> <span style=\"color:red\">three</span>",
                html);
        }

        [Fact]
        public void NullOrEmptyStyleLeavesWordUnwrapped()
        {
            string html = HtmlTextBuilder.StyleWords("keep plain STYLE ME", (word, i) => word == "STYLE" ? "background-color:yellow" : null);

            Assert.Equal("keep plain <span style=\"background-color:yellow\">STYLE</span> ME", html);
        }

        [Fact]
        public void WordIndexIsZeroBasedAndIncrementsOnlyForWords()
        {
            var indices = new System.Collections.Generic.List<int>();
            HtmlTextBuilder.StyleWords("a b c", (word, i) => { indices.Add(i); return null; });

            Assert.Equal(new[] { 0, 1, 2 }, indices);
        }

        [Fact]
        public void WordTextIsHtmlEscaped()
        {
            string html = HtmlTextBuilder.StyleWords("A&B <tag> C", (word, i) => i == 0 ? "color:red" : null);

            Assert.Equal("<span style=\"color:red\">A&amp;B</span> &lt;tag&gt; C", html);
        }

        [Fact]
        public void StyleValueIsAttributeEscaped()
        {
            string html = HtmlTextBuilder.StyleWords("x", (word, i) => "font-family:\"Comic Sans\"");

            Assert.Equal("<span style=\"font-family:&quot;Comic Sans&quot;\">x</span>", html);
        }

        [Fact]
        public void NullInputReturnsNull()
        {
            Assert.Null(HtmlTextBuilder.StyleWords(null!, (w, i) => "color:red"));
        }

        [Fact]
        public void EmptyInputReturnsEmpty()
        {
            Assert.Equal("", HtmlTextBuilder.StyleWords("", (w, i) => "color:red"));
        }

        [Fact]
        public void NullSelectorThrows()
        {
            Assert.Throws<ArgumentNullException>(() => HtmlTextBuilder.StyleWords("x", null!));
        }

        [Fact]
        public void GeneratedMarkupRoundTripsThroughParserAndRendersCorrectly()
        {
            StaThread.Run(() =>
            {
                string[] palette = { "red", "green", "blue" };
                string html = HtmlTextBuilder.StyleWords("alpha beta gamma", (word, i) => "color:" + palette[i % palette.Length]);

                var tree = new HtmlTagTree();
                new HtmlParser(tree).Parse(new StringReader(html));
                var tb = new TextBlock();
                new HtmlUpdater(tb).Update(tree);

                var text = new TextRange(tb.ContentStart, tb.ContentEnd).Text;
                Assert.Equal("alpha beta gamma", text);

                var wordRuns = new System.Collections.Generic.List<Run>();
                foreach (var inline in tb.Inlines)
                    if (inline is Run r && r.Text.Trim().Length > 0) wordRuns.Add(r);

                Assert.Equal(3, wordRuns.Count);
                Assert.Equal(Colors.Red, ((SolidColorBrush)wordRuns[0].Foreground).Color);
                Assert.Equal(Colors.Green, ((SolidColorBrush)wordRuns[1].Foreground).Color);
                Assert.Equal(Colors.Blue, ((SolidColorBrush)wordRuns[2].Foreground).Color);
            });
        }
    }
}
