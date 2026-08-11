using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cofe.Core.Utils
{
    public class HtmlAttributeStringSerializer : IPropertySerializer
    {
        #region Constructor
        
        #endregion

        #region Methods

        public string PropertyToString(IEnumerable<Tuple<string, string>> properties)
        {
            string retVal = "";
            foreach (var prop in properties)
                retVal += String.Format(" {0}=\"{1}\"", prop.Item1, prop.Item2);
            return retVal;
        }

        private static void locateNextVariable(ref string working, ref string varName, ref string varValue)
        {
            working = working.Trim();

            Int32 pos1 = working.IndexOf('=');
            if (pos1 == -1)
            {
                //Boolean attribute (e.g. "checked") or the trailing "/" of a self closing tag.
                Int32 space = working.IndexOf(' ');
                if (space == -1) { varName = working; working = ""; }
                else { varName = working.Substring(0, space); working = working.Substring(space + 1); }
                varName = varName.ToLowerInvariant();
                varValue = "TRUE";
                return;
            }

            varName = working.Substring(0, pos1).Trim().ToLowerInvariant();
            String rest = working.Substring(pos1 + 1).TrimStart();

            if ((rest.Length > 0) && ((rest[0] == '"') || (rest[0] == '\'')))
            {
                char q = rest[0];
                Int32 endQuote = rest.IndexOf(q, 1);
                if (endQuote == -1)
                {
                    varValue = rest.Substring(1);
                    working = "";
                }
                else
                {
                    varValue = rest.Substring(1, endQuote - 1);
                    working = rest.Substring(endQuote + 1).TrimStart();
                }
            }
            else
            {
                Int32 space = rest.IndexOf(' ');
                if (space == -1) { varValue = rest; working = ""; }
                else { varValue = rest.Substring(0, space); working = rest.Substring(space + 1); }
            }
        }

        public IEnumerable<Tuple<string, string>> StringToProperty(string propertyString)
        {
            string working = propertyString;
            string varName = "", varValue = "";
            while (working != "")
            {
                locateNextVariable(ref working, ref varName, ref varValue);
                yield return new Tuple<string, string>(varName, varValue);
            }
        }

        #endregion

        #region Data
        
        #endregion

        #region Public Properties
        
        #endregion

       
    }
}
