using System;

namespace RealmForge
{
    /// <summary>
    /// Summary description for FileSearchInfo.
    /// </summary>
    [SerializedClass( "FileSearchInfo" )]
    public class FileSearchInfo
    {
        [Serialized( true )]
        public string Directory;
        [Serialized( true )]
        public string Pattern;

        public FileSearchInfo( string directory, string pattern )
        {
            Directory = directory;
            Pattern = pattern;
        }

        public FileSearchInfo()
            : this( "", "" )
        {
        }
    }
}
