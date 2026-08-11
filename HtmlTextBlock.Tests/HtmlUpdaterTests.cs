using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using AqiTechTips;
using Xunit;

namespace HtmlTextBlock.Tests
{
    public class HtmlUpdaterTests
    {
        private static TextBlock Render(string html)
        {
            var tree = new HtmlTagTree();
            new HtmlParser(tree).Parse(new StringReader(html));
            var tb = new TextBlock();
            new HtmlUpdater(tb).Update(tree);
            return tb;
        }

        private static string RenderedText(TextBlock tb)
        {
            return new TextRange(tb.ContentStart, tb.ContentEnd).Text;
        }

        [Fact]
        public void BoldTagProducesBoldInline()
        {
            StaThread.Run(() =>
            {
                var tb = Render("<b>hi</b>");
                Assert.Contains(tb.Inlines, i => i is Bold);
                Assert.Equal("hi", RenderedText(tb).Trim());
            });
        }

        [Fact]
        public void StrongTagIsTreatedAsBold()
        {
            StaThread.Run(() =>
            {
                var tb = Render("<strong>hi</strong>");
                Assert.Contains(tb.Inlines, i => i is Bold);
            });
        }

        [Fact]
        public void EmTagIsTreatedAsItalic()
        {
            StaThread.Run(() =>
            {
                var tb = Render("<em>hi</em>");
                Assert.Contains(tb.Inlines, i => i is Italic);
            });
        }

        [Theory]
        [InlineData("<s>gone</s>")]
        [InlineData("<strike>gone</strike>")]
        [InlineData("<del>gone</del>")]
        public void StrikethroughVariantsApplyStrikethroughDecoration(string html)
        {
            StaThread.Run(() =>
            {
                var tb = Render(html);
                var run = Assert.IsType<Run>(Assert.Single(tb.Inlines));
                Assert.Contains(run.TextDecorations, d => d.Location == TextDecorationLocation.Strikethrough);
            });
        }

        [Fact]
        public void UnderlineAndStrikethroughCanCombine()
        {
            StaThread.Run(() =>
            {
                var tb = Render("<span style=\"text-decoration:underline line-through\">x</span>");
                var run = Assert.IsType<Run>(Assert.Single(tb.Inlines));
                Assert.Contains(run.TextDecorations, d => d.Location == TextDecorationLocation.Underline);
                Assert.Contains(run.TextDecorations, d => d.Location == TextDecorationLocation.Strikethrough);
            });
        }

        [Fact]
        public void MarkTagAppliesYellowBackground()
        {
            StaThread.Run(() =>
            {
                var tb = Render("<mark>hi</mark>");
                var run = Assert.IsType<Run>(Assert.Single(tb.Inlines));
                Assert.Equal(Colors.Yellow, ((SolidColorBrush)run.Background).Color);
            });
        }

        [Fact]
        public void CodeTagUsesMonospaceFont()
        {
            StaThread.Run(() =>
            {
                var tb = Render("<code>x=1</code>");
                var run = Assert.IsType<Run>(Assert.Single(tb.Inlines));
                Assert.Equal("Consolas", run.FontFamily.Source);
            });
        }

        [Fact]
        public void SmallTagShrinksFontRelativeToTextBlock()
        {
            StaThread.Run(() =>
            {
                var tree = new HtmlTagTree();
                new HtmlParser(tree).Parse(new StringReader("<small>tiny</small>"));
                var tb = new TextBlock { FontSize = 20 };
                new HtmlUpdater(tb).Update(tree);
                var run = Assert.IsType<Run>(Assert.Single(tb.Inlines));
                Assert.Equal(17.0, run.FontSize, 3);
            });
        }

        [Fact]
        public void BigTagGrowsFontRelativeToTextBlock()
        {
            StaThread.Run(() =>
            {
                var tree = new HtmlTagTree();
                new HtmlParser(tree).Parse(new StringReader("<big>huge</big>"));
                var tb = new TextBlock { FontSize = 10 };
                new HtmlUpdater(tb).Update(tree);
                var run = Assert.IsType<Run>(Assert.Single(tb.Inlines));
                Assert.Equal(12.0, run.FontSize, 3);
            });
        }

        [Fact]
        public void FontTagAppliesColorFaceAndSize()
        {
            StaThread.Run(() =>
            {
                var tb = Render("<font color=\"red\" face=\"Arial\" size=\"18\">x</font>");
                var run = Assert.IsType<Run>(Assert.Single(tb.Inlines));
                Assert.Equal(Colors.Red, ((SolidColorBrush)run.Foreground).Color);
                Assert.Equal("Arial", run.FontFamily.Source);
                Assert.Equal(18.0, run.FontSize);
            });
        }

        [Fact]
        public void HyperlinkTagSetsNavigateUri()
        {
            StaThread.Run(() =>
            {
                var tb = Render("<a href=\"https://example.com\">click</a>");
                var link = Assert.IsType<Hyperlink>(Assert.Single(tb.Inlines));
                Assert.Equal(new Uri("https://example.com"), link.NavigateUri);
            });
        }

        [Fact]
        public void InvalidHyperlinkDoesNotThrowAndLeavesNavigateUriNull()
        {
            StaThread.Run(() =>
            {
                var tb = Render("<a href=\"not a valid uri\">click</a>");
                var link = Assert.IsType<Hyperlink>(Assert.Single(tb.Inlines));
                Assert.Null(link.NavigateUri);
            });
        }

        [Fact]
        public void SpanStyleAppliesColorBackgroundWeightStyleDecorationFamilyAndSize()
        {
            StaThread.Run(() =>
            {
                var tb = Render("<span style=\"color:red;background-color:#00ff00;font-weight:bold;font-style:italic;text-decoration:underline;font-family:Consolas;font-size:20px\">x</span>");
                // Bold/Italic wrap the Run, so walk to find the innermost Run for family/size/decoration
                // and check the composed tree for color/background which are set on the outermost span.
                Inline outer = Assert.Single(tb.Inlines);
                Assert.Equal(Colors.Red, ((SolidColorBrush)outer.Foreground).Color);
                Assert.Equal(Colors.Lime, ((SolidColorBrush)outer.Background).Color);
                Assert.IsType<Italic>(outer);
                var bold = Assert.IsType<Bold>(((Span)outer).Inlines.FirstInline);
                var run = Assert.IsType<Run>(bold.Inlines.FirstInline);
                Assert.Equal("Consolas", run.FontFamily.Source);
                Assert.Equal(20.0, run.FontSize);
                Assert.Contains(outer.TextDecorations, d => d.Location == TextDecorationLocation.Underline);
            });
        }

        [Fact]
        public void SpanStyleSupportsRgbColorFunction()
        {
            StaThread.Run(() =>
            {
                var tb = Render("<span style=\"color: rgb(10, 20, 30)\">x</span>");
                var run = Assert.IsType<Run>(Assert.Single(tb.Inlines));
                Assert.Equal(Color.FromRgb(10, 20, 30), ((SolidColorBrush)run.Foreground).Color);
            });
        }

        [Fact]
        public void NestedTagsComposeStyles()
        {
            StaThread.Run(() =>
            {
                // HtmlUpdater always wraps Bold before Italic regardless of source tag nesting
                // order, since it composes from CurrentState's boolean flags, not tag structure.
                // TextDecorations is set on the fully-composed outer inline (here, Italic) -
                // WPF's text engine applies it to everything nested inside, but it is not a
                // DependencyProperty-inheritance value readable back from the descendant Run.
                var tb = Render("<b><i><u>combo</u></i></b>");
                var italic = Assert.IsType<Italic>(Assert.Single(tb.Inlines));
                var bold = Assert.IsType<Bold>(italic.Inlines.FirstInline);
                Assert.IsType<Run>(bold.Inlines.FirstInline);
                Assert.Contains(italic.TextDecorations, d => d.Location == TextDecorationLocation.Underline);
            });
        }

        [Theory]
        [InlineData("Tom &amp; Jerry", "Tom & Jerry")]
        [InlineData("&lt;tag&gt;", "<tag>")]
        [InlineData("&#65;&#66;", "AB")]
        [InlineData("&#x41;&#x42;", "AB")]
        [InlineData("&copy;2026", "©2026")]
        [InlineData("no entities here", "no entities here")]
        public void HtmlEntitiesAreDecodedInRenderedText(string input, string expected)
        {
            StaThread.Run(() =>
            {
                var tb = Render(input);
                Assert.Equal(expected, RenderedText(tb));
            });
        }

        [Fact]
        public void BrTagProducesLineBreak()
        {
            StaThread.Run(() =>
            {
                var tb = Render("line one<br/>line two");
                Assert.Contains(tb.Inlines, i => i is LineBreak);
            });
        }

        [Fact]
        public void UnhandledElementTagRendersEmptyRunInsteadOfThrowing()
        {
            StaThread.Run(() =>
            {
                var tb = Render("<img src=\"x.png\">");
                // img is a registered Element tag with no rendering logic; should not throw.
                Assert.NotNull(tb);
            });
        }

        [Fact]
        public void BindingTagWithNullPropertyValueRendersEmptyInsteadOfThrowing()
        {
            StaThread.Run(() =>
            {
                var tree = new HtmlTagTree();
                new HtmlParser(tree).Parse(new StringReader("Value: <binding path=\"Nothing\" />"));
                var tb = new TextBlock { DataContext = new { Nothing = (string?)null } };
                new HtmlUpdater(tb).Update(tree);
                Assert.Equal("Value: ", RenderedText(tb));
            });
        }

        [Theory]
        [InlineData("<A HREF=\"https://example.com\">link</A>")]
        [InlineData("<a HREF=\"https://example.com\">link</a>")]
        public void UppercaseOrMixedCaseAttributeNamesStillApply(string html)
        {
            StaThread.Run(() =>
            {
                var tb = Render(html);
                var link = Assert.IsType<Hyperlink>(Assert.Single(tb.Inlines));
                Assert.Equal(new Uri("https://example.com"), link.NavigateUri);
            });
        }

        [Fact]
        public void UppercaseFontColorAttributeStillApplies()
        {
            StaThread.Run(() =>
            {
                var tb = Render("<FONT COLOR=\"red\">red?</FONT>");
                var run = Assert.IsType<Run>(Assert.Single(tb.Inlines));
                Assert.Equal(Colors.Red, ((SolidColorBrush)run.Foreground).Color);
            });
        }

        [Fact]
        public void AstralPlaneNumericEntityDecodesAsSurrogatePairInsteadOfTruncating()
        {
            StaThread.Run(() =>
            {
                var tb = Render("grinning: &#128512; done");
                Assert.Equal("grinning: " + char.ConvertFromUtf32(0x1F600) + " done", RenderedText(tb));
            });
        }

        [Fact]
        public void PointFontSizeConvertsToDeviceIndependentPixels()
        {
            StaThread.Run(() =>
            {
                var tb = Render("<span style=\"font-size:12pt\">x</span>");
                var run = Assert.IsType<Run>(Assert.Single(tb.Inlines));
                Assert.Equal(16.0, run.FontSize, 3);
            });
        }
    }
}
