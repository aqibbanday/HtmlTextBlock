using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows;
using AqiTechTips;
using System.IO;

namespace AqiTechTips
{
    public class HtmlHighlightTextBlock : TextBlock
    {
        public string Highlight
        {
            get { return (string)GetValue(HighlightProperty); }
            set { SetValue(HighlightProperty, value); }
        }


        public static readonly DependencyProperty HighlightProperty =
        DependencyProperty.Register("Highlight", typeof(string), typeof(HtmlHighlightTextBlock), new UIPropertyMetadata(""));


        public static DependencyProperty HtmlProperty = DependencyProperty.Register("Html", typeof(string),
                typeof(HtmlHighlightTextBlock), new UIPropertyMetadata("Html", new PropertyChangedCallback(OnHtmlChanged)));

        public string Html { get { return (string)GetValue(HtmlProperty); } set { SetValue(HtmlProperty, value); } }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            Parse(Html);
        }

        private void Parse(string html)
        {
            if (!String.IsNullOrEmpty(Highlight))
            {
                int searchFrom = 0;
                int idx = html.IndexOf(Highlight, searchFrom, StringComparison.InvariantCultureIgnoreCase);
                while (idx != -1)
                {
                    if (IsInsideTag(html, idx))
                    {
                        searchFrom = idx + 1;
                    }
                    else
                    {
                        html = String.Format("{0}<b>{1}</b>{2}",
                            html.Substring(0, idx), html.Substring(idx, Highlight.Length), html.Substring(idx + Highlight.Length));
                        searchFrom = idx + 7 + Highlight.Length;
                    }
                    idx = html.IndexOf(Highlight, searchFrom, StringComparison.InvariantCultureIgnoreCase);
                }
            }

            Inlines.Clear();
            HtmlTagTree tree = new HtmlTagTree();
            HtmlParser parser = new HtmlParser(tree); //output
            parser.Parse(new StringReader(html));     //input

            HtmlUpdater updater = new HtmlUpdater(this); //output
            updater.Update(tree);
        }

        /// <summary>
        /// Scans from the start of html for &lt;...&gt; / [...] tag spans (mirroring HtmlParser's
        /// bracket auto-detection) to determine whether index falls inside one, so Highlight
        /// matches inside tag names/attributes are skipped instead of corrupting the markup.
        /// </summary>
        private static bool IsInsideTag(string html, int index)
        {
            int pos = 0;
            while (pos < html.Length)
            {
                int angle = html.IndexOf('<', pos);
                int square = html.IndexOf('[', pos);
                char openCh = '<', closeCh = '>';
                if ((square != -1) && ((angle == -1) || (square < angle)))
                {
                    openCh = '[';
                    closeCh = ']';
                }

                int start = html.IndexOf(openCh, pos);
                if (start == -1 || start > index)
                    return false;

                int end = html.IndexOf(closeCh, start);
                if (end == -1)
                    return true; //Unterminated tag - treat everything from here on as inside it.
                if (index <= end)
                    return index >= start;

                pos = end + 1;
            }
            return false;
        }

        public static void OnHtmlChanged(DependencyObject s, DependencyPropertyChangedEventArgs e)
        {
            HtmlHighlightTextBlock sender = (HtmlHighlightTextBlock)s;
            sender.Parse((string)e.NewValue);
        }

        public HtmlHighlightTextBlock()
        {

        }

    }
}
