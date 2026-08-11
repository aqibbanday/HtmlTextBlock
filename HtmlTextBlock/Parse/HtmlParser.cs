/*
 * Created by SharpDevelop.
 * User: LYCJ
 * Date: 19/10/2007
 * Time: 3:16
 *
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using System.IO;

namespace AqiTechTips
{
	/// <summary>
    /// MiniHtml internal Html Paraser, used since D7 version of TQzHtmlLabel2.
    /// Scans the input once using an index cursor rather than re-slicing the
    /// shrinking remainder into a new string on every tag (which made parsing
    /// quadratic in the number of tags for larger documents).
    /// </summary>
    public class HtmlParser
    {
        private HtmlTagTree tree;
    	internal HtmlTagNode previousNode = null;

        /// <summary>
        /// Constructor
        /// </summary>
        public HtmlParser(HtmlTagTree aTree)
        {
        	tree = aTree;
        }

        /// <summary> Add a Non TextTag to Tag List </summary>
        internal void addTag(HtmlTag aTag)
        {
//            HtmlTagNode newNode = new HtmlTagNode(
        	if (previousNode == null) { previousNode = tree; }

        	while (!previousNode.CanAdd(aTag))
        		previousNode = previousNode.Parent;

        	previousNode = previousNode.Add(aTag);
        }
        /// <summary>
        /// Finds the next occurrence of endBracket starting from start, skipping over any
        /// single- or double-quoted attribute value so a literal endBracket character inside
        /// a quoted value (e.g. title="5 &gt; 3") doesn't end the tag early. Returns -1 if not found.
        /// </summary>
        private static int FindTagEnd(string input, int start, char endBracket)
        {
            bool inQuote = false;
            char quoteChar = '\0';
            for (int i = start; i < input.Length; i++)
            {
                char c = input[i];
                if (inQuote)
                {
                    if (c == quoteChar)
                        inQuote = false;
                }
                else if ((c == '"') || (c == '\''))
                {
                    inQuote = true;
                    quoteChar = c;
                }
                else if (c == endBracket)
                {
                    return i;
                }
            }
            return -1;
        }
        /// <summary>
        /// Reads the next tag starting from <paramref name="pos"/> in <paramref name="input"/>.
        /// Auto detects whether the tag uses HTML angle brackets (&lt;b&gt;) or legacy square
        /// brackets ([b]), whichever opens first from the current position. Advances
        /// <paramref name="pos"/> past what was consumed. Returns false once no further tag
        /// is found, in which case beforeTag holds the remaining text.
        /// </summary>
        private static bool tryReadNextTag(string input, ref int pos, out string beforeTag, out string tagName, out string tagVars)
        {
            Int32 angleBracketPos = input.IndexOf('<', pos);
            Int32 squareBracketPos = input.IndexOf('[', pos);

            char startBracket = '<';
            char endBracket = '>';
            if ((squareBracketPos != -1) && ((angleBracketPos == -1) || (squareBracketPos < angleBracketPos)))
            {
                startBracket = '[';
                endBracket = ']';
            }

            Int32 pos1 = input.IndexOf(startBracket, pos);
            Int32 pos2 = (pos1 == -1) ? -1 : FindTagEnd(input, pos1 + 1, endBracket);

            if ((pos1 == -1) || (pos2 == -1))
            {
                beforeTag = input.Substring(pos);
                tagName = "";
                tagVars = "";
                pos = input.Length;
                return false;
            }

            String tagStr = input.Substring(pos1 + 1, pos2 - pos1 - 1);
            beforeTag = input.Substring(pos, pos1 - pos);
            Int32 nextPos = pos2 + 1;

            Int32 pos3 = tagStr.IndexOf(' ');
            if ((pos3 != -1) && (tagStr != ""))
            {
                tagName = tagStr.Substring(0, pos3);
                tagVars = tagStr.Substring(pos3 + 1, tagStr.Length - pos3 - 1);
            }
            else
            {
                tagName = tagStr;
                tagVars = "";
            }

            if (tagName.StartsWith("!--"))
            {
                if ((tagName.Length < 6) || (!(tagName.EndsWith("--"))))
                {
                    Int32 pos4 = input.IndexOf("-->", nextPos);
                    if (pos4 != -1)
                        nextPos = pos4 + 2;
                }
                tagName = "";
                tagVars = "";
            }
            else if (tagName.StartsWith("!"))
            {
                //Doctype or other declaration - ignore.
                tagName = "";
                tagVars = "";
            }
            else if (tagName.EndsWith("/") && (tagName.Length > 1))
            {
                //Self closing tag without a space before the slash, e.g. <br/>.
                tagName = tagName.Substring(0, tagName.Length - 1);
            }

            pos = nextPos;
            return true;
        }
        /// <summary>
        /// Parse Html
        /// </summary>
        public void Parse(TextReader reader)
        {
        	previousNode = null;

            string input = reader.ReadToEnd();
            Int32 pos = 0;

            while (pos < input.Length)
            {
                string beforeTag, tagName, tagVars;
                tryReadNextTag(input, ref pos, out beforeTag, out tagName, out tagVars);

                if (beforeTag != "")
                    addTag(new HtmlTag(beforeTag));    //Text
                if (tagName != "")
                    addTag(new HtmlTag(tagName, tagVars));
            }
        }

        public static void DebugUnit()
        {
            //string beforeTag="", afterTag="", tagName="", tagVar="";
            //readNextTag("<!-- xyz --><a href=\"xyz\"><b>", ref beforeTag, ref afterTag, ref tagName, ref tagVar);
            //readNextTag(afterTag, ref beforeTag, ref afterTag, ref tagName, ref tagVar);
            //Console.WriteLine(beforeTag);
            //Console.WriteLine(afterTag);
            //Console.WriteLine(tagName);
            //Console.WriteLine(tagVar);
            //string Html = "<b>test</b>";
//
//            mh.parser.Parse((new StringReader(Html)));
//            mh.masterTag.childTags.PrintItems();
        }
    }
}
