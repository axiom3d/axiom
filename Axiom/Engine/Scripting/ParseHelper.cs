using System;
using System.IO;
using System.Text;
using Axiom.Core;
using Axiom.MathLib;

namespace Axiom.Scripting {
    /// <summary>
    /// 	Class contining helper methods for parsing text files.
    /// </summary>
    public class ParseHelper {
        #region Methods
		
        /// <summary>
        ///		Helper method to log a formatted error when encountering problems with parsing
        ///		an attribute.
        /// </summary>
        /// <param name="attribute"></param>
        /// <param name="context"></param>
        /// <param name="expectedParams"></param>
        public static void LogParserError(string attribute, string context, string reason) {
            string error = string.Format("Bad {0} attribute in block '{1}'. Reason: {2}", attribute, context, reason);

            System.Diagnostics.Trace.WriteLine(error);
        }

        /// <summary>
        ///		Parses a boolean type value 
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static bool ParseBool(string val) {
            switch(val) {
                case "true":
                case "on":
                    return true;
                case "false":
                case "off":
                    return false;
            }

            // make the compiler happy
            return false;
        }

        /// <summary>
        ///		Parses an array of params and returns a color from it.
        /// </summary>
        /// <param name="val"></param>
        public static ColorEx ParseColor(string[] values) {
            ColorEx color = new ColorEx();
            color.r = float.Parse(values[0]);
            color.g = float.Parse(values[1]);
            color.b = float.Parse(values[2]);
            color.a = (values.Length == 4) ? float.Parse(values[3]) : 1.0f;

            return color;
        }

        /// <summary>
        ///		Parses an array of params and returns a color from it.
        /// </summary>
        /// <param name="val"></param>
        public static Vector3 ParseVector3(string[] values) {
            Vector3 vec = new Vector3();
            vec.x = float.Parse(values[0]);
            vec.y = float.Parse(values[1]);
            vec.z = float.Parse(values[2]);

            return vec;
        }

        /// <summary>
        ///		Helper method to nip/tuck the string before parsing it.  This includes trimming spaces from the beginning
        ///		and end of the string, as well as removing excess spaces in between values.
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static string ReadLine(TextReader reader) {
            string line = reader.ReadLine();

            if(line != null) {
                line = line.Replace("\t", " ");
                line = line.Trim();

                // ignore blank lines, lines without spaces, or comments
                if(line.Length == 0 || line.IndexOf(' ') == -1 || line.StartsWith("//")) {
                    return line;
                }

                StringBuilder sb = new StringBuilder();

                string[] values = line.Split(' ');

                // reduce big space gaps between values down to a single space
                for(int i = 0; i < values.Length; i++) {
                    string val = values[i];

                    if(val.Length != 0) {
                        sb.Append(val + " ");
                    }
                }
				
                line = sb.ToString();
                line = line.TrimEnd();
            } // if
			
            return line;
        }

        /// <summary>
        ///		Helper method to remove the first item from a string array and return a new array 1 element smaller
        ///		starting at the second element of the original array.  This helpe to seperate the params from the command
        ///		in the various script files.
        /// </summary>
        /// <param name="splitLine"></param>
        /// <returns></returns>
        public static string[] GetParams(string[] all) {
            // create a seperate parm list that has the command removed
            string[] parms = new string[all.Length - 1];
            Array.Copy(all, 1, parms, 0, parms.Length);

            return parms;
        }

        /// <summary>
        ///    Advances in the stream until it hits the next {.
        /// </summary>
        public static void SkipToNextOpenBrace(TextReader reader) {
            string line = "";
            while(line != null && line != "{") {
                line = ReadLine(reader);
            }
        }

        /// <summary>
        ///    Advances in the stream until it hits the next }.
        /// </summary>
        /// <param name="reader"></param>
        public static void SkipToNextCloseBrace(TextReader reader) {
            string line = "";
            while(line != null && line != "}") {
                line = ReadLine(reader);
            }
        }

        #endregion
    }
}
