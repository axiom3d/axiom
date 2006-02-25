using System;
using System.Collections.Specialized;


namespace RealmForge
{
    #region Delegates
    public delegate string StringLookup( string key );
    #endregion

    /// <summary>
    /// A Utility class for parsing and string interpolation, replacement, and manipulation
    /// </summary>
    public class StringUtil
    {
        #region Static Methods


        /// <summary>
        /// Gets a collection of StringLocationPair objects that represent the matches
        /// </summary>
        /// <param name="target"></param>
        /// <param name="beforeGroup"></param>
        /// <param name="afterGroup"></param>
        /// <returns></returns>
        public static StringCollection FindGroups( string target, string beforeGroup, string afterGroup, bool includeDelimitersInSubstrings )
        {
            StringCollection results = new StringCollection();
            if ( target == null || target.Length == 0 )
                return results;

            int beforeMod = 0;
            int afterMod = 0;
            if ( includeDelimitersInSubstrings )
            {//be sure to not exlude the delims
                beforeMod = beforeGroup.Length;
                afterMod = afterGroup.Length;
            }
            int startIndex = 0;
            while ( ( startIndex = target.IndexOf( beforeGroup, startIndex ) ) != -1 )
            {
                int endIndex = target.IndexOf( afterGroup, startIndex );//the index of the char after it
                if ( endIndex == -1 )
                    break;
                int length = endIndex - startIndex - beforeGroup.Length;//move to the first char in the string
                string substring = substring = target.Substring( startIndex + beforeGroup.Length - beforeMod,
                    length - afterMod );

                results.Add( substring );
                //results.Add(new StringLocationPair(substring,startIndex));
                startIndex = endIndex + 1;
                //the Interpolate*() methods will not work if expressions are expandded inside expression due to an optimization
                //so start after endIndex

            }
            return results;
        }

        public static string ReplaceGroups( string target, string beforeGroup, string afterGroup, StringLookup lookup )
        {
            int targetLength = target.Length;
            StringCollection strings = FindGroups( target, beforeGroup, afterGroup, false );
            foreach ( string substring in strings )
            {
                target = target.Replace( beforeGroup + substring + afterGroup, lookup( substring ) );
            }
            return target;
        }

        /// <summary>
        /// Replaces ${var} statements in a string with the corresonding values as detirmined by the lookup delegate
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public static string InterpolateForVariables( string target, StringLookup lookup )
        {
            return ReplaceGroups( target, "${", "}", lookup );
        }


        /// <summary>
        /// Replaces {var} statements in a string with the corresonding values as detirmined by the lookup delegate
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public static string InterpolateForFormatVariables( string target, StringLookup lookup )
        {
            return ReplaceGroups( target, "{", "}", lookup );
        }



        /// <summary>
        /// Replaces ${var} statements in a string with the corresonding environment variable with name var
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public static string InterpolateForEnvironmentVariables( string target )
        {
            return InterpolateForVariables( target, new StringLookup( Environment.GetEnvironmentVariable ) );
        }

        #endregion
    }
}
