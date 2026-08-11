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
        public void HtmlCommentIsSkipped()
        {
            var tags = Parse("before<!-- a comment -->after");
            var names = tags.Select(t => t.Name).ToList();
            Assert.DoesNotContain(names, n => n.StartsWith("!"));
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
