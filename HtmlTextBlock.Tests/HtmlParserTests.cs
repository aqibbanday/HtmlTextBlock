using System.Collections.Generic;
using System.IO;
using System.Linq;
using AqiTechTips;
using Xunit;

namespace HtmlTextBlock.Tests
{
    public class HtmlParserTests
    {
        private static List<HtmlTag> Parse(string html)
        {
            var tree = new HtmlTagTree();
            new HtmlParser(tree).Parse(new StringReader(html));
            // First entry is always the synthetic "master" root tag.
            return tree.ToHtmlTagList().Skip(1).ToList();
        }

        [Fact]
        public void ParsesRealHtmlAngleBracketTags()
        {
            var tags = Parse("The <i><u>quick</u></i> fox");
            var names = tags.Select(t => t.Name).ToList();
            Assert.Equal(new[] { "text", "i", "u", "text", "/u", "/i", "text" }, names);
        }

        [Fact]
        public void ParsesLegacySquareBracketTags()
        {
            var tags = Parse("The [i][u]quick[/u][/i] fox");
            var names = tags.Select(t => t.Name).ToList();
            Assert.Equal(new[] { "text", "i", "u", "text", "/u", "/i", "text" }, names);
        }

        [Fact]
        public void AutoDetectsBracketStylePerTagWhenMixed()
        {
            var tags = Parse("<b>bold</b> and [i]italic[/i]");
            var names = tags.Select(t => t.Name).ToList();
            Assert.Equal(new[] { "b", "text", "/b", "text", "i", "text", "/i" }, names);
        }

        [Fact]
        public void SelfClosingTagWithoutSpaceIsRecognized()
        {
            var tags = Parse("line one<br/>line two");
            Assert.Contains(tags, t => t.Name == "br");
        }

        [Fact]
        public void SelfClosingTagWithSpaceIsRecognized()
        {
            var tags = Parse("line one<br />line two");
            Assert.Contains(tags, t => t.Name == "br");
        }

        [Fact]
        public void SelfClosingTagWithQuotedAttributeAndNoSpaceBeforeSlashIsRecognized()
        {
            var tags = Parse("before<img src=\"x.png\"/>after");
            var names = tags.Select(t => t.Name).ToList();
            Assert.Equal(new[] { "text", "img", "text" }, names);
        }

        [Fact]
        public void HtmlCommentIsSkipped()
        {
            var tags = Parse("before<!-- a comment -->after");
            var names = tags.Select(t => t.Name).ToList();
            Assert.DoesNotContain(names, n => n.StartsWith("!"));
        }

        [Fact]
        public void CommentContainingAnApostropheIsSkippedWithoutCorruptingTrailingContent()
        {
            // Comments are free text terminated by "-->", not an attribute list; the quote-aware
            // tag boundary scan must not treat an apostrophe in comment text as an unterminated
            // quoted value (which would otherwise swallow the rest of the document).
            var tags = Parse("before<!-- don't touch this -->after");
            var names = tags.Select(t => t.Name).ToList();
            Assert.Equal(new[] { "text", "text" }, names);
            Assert.Equal("before", tags[0]["value"]);
            Assert.Equal("after", tags[1]["value"]);
        }

        [Fact]
        public void DoctypeDeclarationIsSkipped()
        {
            var tags = Parse("<!DOCTYPE html>Hello");
            var names = tags.Select(t => t.Name).ToList();
            Assert.Single(names);
            Assert.Equal("text", names[0]);
        }

        [Fact]
        public void HrefAttributeWithDoubleQuotesParsesWithoutStrayQuoteCharacters()
        {
            var tags = Parse("<a href=\"https://example.com/x?y=1\">click</a>");
            var aTag = tags.First(t => t.Name == "a");
            Assert.Equal("https://example.com/x?y=1", aTag["href"]);
        }

        [Fact]
        public void HrefAttributeWithSingleQuotesStillWorks()
        {
            var tags = Parse("<a href='https://example.com'>click</a>");
            var aTag = tags.First(t => t.Name == "a");
            Assert.Equal("https://example.com", aTag["href"]);
        }

        [Fact]
        public void DoubleQuotedAttributeContainingSpacesIsNotTruncated()
        {
            var tags = Parse("<span style=\"color:red;text-decoration:underline line-through\">x</span>");
            var span = tags.First(t => t.Name == "span");
            Assert.Equal("color:red;text-decoration:underline line-through", span["style"]);
        }

        [Theory]
        [InlineData("<a title=\"5 > 3 is true\" href=\"https://example.com\">link</a> after")]
        [InlineData("<a title='5 > 3 is true' href='https://example.com'>link</a> after")]
        public void GreaterThanInsideQuotedAttributeValueDoesNotTruncateTag(string html)
        {
            var tags = Parse(html);
            var names = tags.Select(t => t.Name).ToList();
            Assert.Equal(new[] { "a", "text", "/a", "text" }, names);
            Assert.Equal("https://example.com", tags.First(t => t.Name == "a")["href"]);
            Assert.Equal("link", tags.First(t => t.Name == "text")["value"]);
        }

        [Theory]
        [InlineData("<div title='it's great'>content</div> after")]
        [InlineData("<div title=\"it's great\">content</div> after")]
        public void ApostropheInsideQuotedAttributeValueDoesNotEndTheValueEarly(string html)
        {
            var tags = Parse(html);
            var names = tags.Select(t => t.Name).ToList();
            Assert.Equal(new[] { "div", "text", "/div", "text" }, names);
            Assert.Equal("content", tags.First(t => t.Name == "text")["value"]);
        }

        [Fact]
        public void MalformedNestedQuotesStillFindATagBoundaryWithoutCorruptingTrailingContent()
        {
            // A quote that never properly closes (a stray unescaped quote inside the value)
            // is a pathological case with no unambiguous interpretation; what matters is that
            // it degrades safely - a tag boundary is still found, and content after the tag
            // (here "link" and "after") keeps rendering as ordinary text instead of the entire
            // rest of the document being swallowed into one corrupted text blob.
            var tags = Parse("<a title=\"unterminated quote href=\"https://example.com\">link</a> after");
            var names = tags.Select(t => t.Name).ToList();
            Assert.Equal(new[] { "a", "text", "/a", "text" }, names);
            Assert.Equal("link", tags.First(t => t.Name == "text")["value"]);
            Assert.Equal(" after", tags.Last(t => t.Name == "text")["value"]);
        }

        [Fact]
        public void BooleanAttributeWithoutEqualsSignDefaultsToTrue()
        {
            var tags = Parse("<input disabled>");
            var input = tags.First(t => t.Name == "input");
            Assert.True(input.Contains("disabled"));
            Assert.Equal("TRUE", input["disabled"]);
        }

        [Fact]
        public void EmptyInputProducesNoTags()
        {
            Assert.Empty(Parse(""));
        }

        [Fact]
        public void UnknownTagsAreParsedWithoutThrowing()
        {
            var tags = Parse("<totallymadeup>text</totallymadeup>");
            Assert.NotEmpty(tags);
        }

        [Fact]
        public void LargeDocumentWithManyTagsParsesCompletelyAndCorrectly()
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < 500; i++)
                sb.Append($"<span style=\"color:red\">word{i}</span> ");

            var tags = Parse(sb.ToString());
            int spanOpenCount = tags.Count(t => t.Name == "span");
            int spanCloseCount = tags.Count(t => t.Name == "/span");
            Assert.Equal(500, spanOpenCount);
            Assert.Equal(500, spanCloseCount);
        }
    }
}
