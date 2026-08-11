using System;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;

#if NETFX_CORE
using Windows.UI;
#else
using System.Windows.Media;
#endif


namespace AqiTechTips
{

    public class CurrentStateType
    {
        private List<HtmlTag> activeStyle = new List<HtmlTag>();
        private bool bold;
        private bool italic;
        private bool underline;
        private bool strikethrough;
        private bool subscript;
        private bool superscript;
        private string hyperlink = null;
        private Color? foreground;
        private Color? background;
        private string font = null;
        private double? fontSize;
        private double? fontSizeMultiplier;


        public bool Bold { get { return bold; } }
        public bool Italic { get { return italic; } }
        public bool Underline { get { return underline; } }
        public bool Strikethrough { get { return strikethrough; } }
        public bool SubScript { get { return subscript; } }
        public bool SuperScript { get { return superscript; } }
        public string HyperLink { get { return hyperlink; } }
        public Color? Foreground { get { return foreground; } }
        public Color? Background { get { return background; } }
        public string Font { get { return font; } }
        public double? FontSize { get { return fontSize; } }
        public double? FontSizeMultiplier { get { return fontSizeMultiplier; } }

        public void UpdateStyle(HtmlTag aTag)
        {
            if (!aTag.IsEndTag)
                activeStyle.Add(aTag);
            else
                for (int i = activeStyle.Count - 1; i >= 0; i--)
                    if ('/' + activeStyle[i].Name == aTag.Name)
                    {
                        activeStyle.RemoveAt(i);
                        break;
                    }
            updateStyle();
        }


        private void updateStyle()
        {
            bold = false;
            italic = false;
            underline = false;
            strikethrough = false;
            subscript = false;
            superscript = false;
            foreground = null;
            background = null;
            font = null;
            hyperlink = "";
            fontSize = null;
            fontSizeMultiplier = null;

            foreach (HtmlTag aTag in activeStyle)
            {
                switch (aTag.Name)
                {
                    case "b":
                    case "strong": bold = true; break;
                    case "i":
                    case "em": italic = true; break;
                    case "u": underline = true; break;
                    case "s":
                    case "strike":
                    case "del": strikethrough = true; break;
                    case "sub": subscript = true; break;
                    case "sup": superscript = true; break;
                    case "mark": background = Colors.Yellow; break;
                    case "code": font = "Consolas"; break;
                    case "small": fontSizeMultiplier = 0.85; break;
                    case "big": fontSizeMultiplier = 1.2; break;
                    case "a": if (aTag.Contains("href")) hyperlink = aTag["href"]; break;
                    case "font" :
                        if (aTag.Contains("color"))
                            try { foreground = (Color)ColorConverter.ConvertFromString(aTag["color"]); }
                            catch (Exception ex)
                            {
                                Debug.WriteLine("CurrentState - invalid font color '" + aTag["color"] + "': " + ex.Message);
                                foreground = Colors.Black;
                            }
                        if (aTag.Contains("face"))
                            font = aTag["face"];
                        if (aTag.Contains("size"))
                            try { fontSize= Double.Parse(aTag["size"]); }
                            catch (Exception ex)
                            {
                                Debug.WriteLine("CurrentState - invalid font size '" + aTag["size"] + "': " + ex.Message);
                            }
                        break;
                }

                if (aTag.Contains("style"))
                    applyInlineStyle(aTag["style"]);
            }
        }

        /// <summary>
        /// Parse a CSS-style "prop: value; prop2: value2" string and apply the properties
        /// it recognizes on top of whatever the tag name itself already set.
        /// </summary>
        private void applyInlineStyle(string styleString)
        {
            if (string.IsNullOrWhiteSpace(styleString))
                return;

            foreach (string declaration in styleString.Split(';'))
            {
                int colon = declaration.IndexOf(':');
                if (colon == -1)
                    continue;

                string prop = declaration.Substring(0, colon).Trim().ToLowerInvariant();
                string value = declaration.Substring(colon + 1).Trim();
                if (value.Length == 0)
                    continue;

                switch (prop)
                {
                    case "color":
                        Color? parsedColor = tryParseColor(value);
                        if (parsedColor.HasValue) foreground = parsedColor.Value;
                        break;
                    case "background-color":
                    case "background":
                        Color? parsedBg = tryParseColor(value);
                        if (parsedBg.HasValue) background = parsedBg.Value;
                        break;
                    case "font-weight":
                        {
                            string v = value.ToLowerInvariant();
                            int numericWeight;
                            bool isNumeric = int.TryParse(v, out numericWeight);
                            bold = (v == "bold") || (v == "bolder") || (isNumeric && numericWeight >= 600);
                        }
                        break;
                    case "font-style":
                        italic = (value.ToLowerInvariant() == "italic") || (value.ToLowerInvariant() == "oblique");
                        break;
                    case "text-decoration":
                    case "text-decoration-line":
                        {
                            string v = value.ToLowerInvariant();
                            if (v.Contains("underline")) underline = true;
                            if (v.Contains("line-through")) strikethrough = true;
                        }
                        break;
                    case "font-family":
                        font = value.Trim('\'', '"').Split(',')[0].Trim();
                        break;
                    case "font-size":
                        double? parsedSize = tryParseFontSize(value);
                        if (parsedSize.HasValue) fontSize = parsedSize.Value;
                        break;
                }
            }
        }

        private static Color? tryParseColor(string value)
        {
            value = value.Trim();
            try
            {
                if (value.StartsWith("rgb", StringComparison.OrdinalIgnoreCase))
                {
                    int open = value.IndexOf('(');
                    int close = value.IndexOf(')');
                    if (open == -1 || close == -1 || close < open)
                        return null;

                    string[] parts = value.Substring(open + 1, close - open - 1).Split(',');
                    if (parts.Length < 3)
                        return null;

                    byte r = (byte)Math.Min(255, Math.Max(0, int.Parse(parts[0].Trim())));
                    byte g = (byte)Math.Min(255, Math.Max(0, int.Parse(parts[1].Trim())));
                    byte b = (byte)Math.Min(255, Math.Max(0, int.Parse(parts[2].Trim())));
                    return Color.FromRgb(r, g, b);
                }

                return (Color)ColorConverter.ConvertFromString(value);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("CurrentState - invalid style color '" + value + "': " + ex.Message);
                return null;
            }
        }

        private static double? tryParseFontSize(string value)
        {
            value = value.Trim().ToLowerInvariant();
            try
            {
                if (value.EndsWith("px"))
                    return Double.Parse(value.Substring(0, value.Length - 2));
                if (value.EndsWith("pt"))
                    return Double.Parse(value.Substring(0, value.Length - 2));
                return Double.Parse(value);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("CurrentState - invalid style font-size '" + value + "': " + ex.Message);
                return null;
            }
        }

        public CurrentStateType()
        {

        }



    }

}
