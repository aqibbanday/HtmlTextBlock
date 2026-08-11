/*
 * Created by SharpDevelop.
 * User: LYCJ
 * Date: 18/10/2007
 * Time: 21:42
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using Cofe.Core.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace AqiTechTips
{
	/// <summary>
	/// Represent an element Tag in Html code
	/// </summary>
	public class HtmlTag
	{
        private static IParamParser HtmlAttributeParser = new ParamParser(new HtmlAttributeStringSerializer());

        ///<summary> Maps a BuiltinTags Html name to its array index, built once and reused for every lookup. </summary>
        private static readonly Dictionary<string, int> tagIndexByName = BuildTagIndex();

        private static Dictionary<string, int> BuildTagIndex()
        {
            Dictionary<string, int> dict = new Dictionary<string, int>(Defines.BuiltinTags.Length);
            for (int i = 0; i < Defines.BuiltinTags.Length; i++)
                dict[Defines.BuiltinTags[i].Html] = i;
            return dict;
        }

        private string name;                                     //HtmlTag name without <>
        private Dictionary<string, string> variables = new Dictionary<string, string>();     //Variable List and values
        private int? id;                                          //Cached ID, name never changes after construction.

        ///<summary> Gets HtmlTag ID in BuiltInTags. (without <>) </summary>
        internal int ID
        {
            get
            {
                if (!id.HasValue)
                {
                    string lookupName = (name.Length > 0 && name[0] == '/') ? name.Substring(1) : name;
                    int found;
                    id = tagIndexByName.TryGetValue(lookupName, out found) ? found : -1;
                }
                return id.Value;
            }
        }
        ///<summary> Gets HtmlTag Level in BuiltInTags. (without <>) </summary>
        internal Int32 Level { get { if (ID == -1) return 0; else return Defines.BuiltinTags[ID].tagLevel; } }
        
        internal bool IsEndTag { get {  return ((name.IndexOf('/') == 0) ||(variables.ContainsKey("/"))); } }
        
        ///<summary> Gets HtmlTag name. (without <>) </summary>
        public string Name { get { return name; } }
        ///<summary> Gets variable value. </summary>
        public string this[string key] { get { return variables[key]; } }        
        ///<summary> Gets whether variable list contains the specified key. </summary>
        public bool Contains(string key) { return variables.ContainsKey(key); }
        ///<summary> Returns the string representation of the value of this instance.  </summary>
		public override string ToString()
		{
			return String.Format("<{0}> : {1}", name, variables.ToString());
		}
        
        /// <summary>
        /// Initialite procedure, can be used by child tags.
        /// </summary>
        protected void init(string aName, Dictionary<string, string> aVariables)
        {                        
            name = aName.ToLower();
            if (aVariables == null)
                variables = new Dictionary<string, string>();
            else
                variables = aVariables;            
        }
        
		///<summary> Constructor. </summary>
		public HtmlTag(string aName, string aVarString)
		{
			init(aName, HtmlAttributeParser.StringToDictionary(aVarString));
		}
		
		public HtmlTag(string aText)
		{
            Dictionary<string, string> aList = new Dictionary<string, string>();
			aList.Add("value", aText);
			init("text", aList);
		}
	}		
		
}
