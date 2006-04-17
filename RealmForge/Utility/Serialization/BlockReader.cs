using System;
using System.IO;
using System.Text;

namespace RealmForge.Serialization
{
    /// <summary>
    /// Reads blocks of a file as delimited a specified character
    /// Similiar to StreamReader.ReadLine() but using any character as the delimeter
    /// </summary>
    public class BlockReader
    {
        #region Protected Fields

        protected char delim;
        protected StreamReader file;
        protected int bufferSize;
        protected int bufferLen = 0;
        protected int bufferStart = 0;
        protected StringBuilder sb = new StringBuilder();
        protected char[] buffer;
        protected bool finishedBlock = false;
        protected bool finishedFile = false;
        #endregion

        #region Constructors
        public BlockReader( Stream file, char delim, int bufferSize )
        {
            this.file = new StreamReader( file );
            this.delim = delim;
            this.bufferSize = bufferSize;
            buffer = new char[bufferSize];
        }
        #endregion

        #region Properties

        public int DelimiterIndex
        {
            get
            {
                return Array.IndexOf( buffer, delim, bufferStart, bufferLen );
            }
        }

        public char Delimiter
        {
            get
            {
                return delim;
            }
            set
            {
                delim = value;
            }
        }
        #endregion

        #region Public Methods

        public string ReadBlock()
        {
            if ( finishedFile )
            {
                return null;
            }
            finishedBlock = false;
            Append();	//append anything left in the buffer
            while ( !finishedBlock )
            {
                Read();
                Append();
            }
            //get the result
            string result = sb.ToString();
            //clear the result cache
            sb.Length = 0;
            return result;
        }
        #endregion

        #region Protected Methods
        protected void Append()
        {
            if ( bufferLen == 0 )
                return;
            int index = DelimiterIndex;
            if ( index != -1 )
            {//if found delim inside the block, split it
                finishedBlock = true;//end reading loop
                sb.Append( buffer, bufferStart, index - bufferStart );
                //start where left off for next append
                bufferLen -= ( index - bufferStart + 1 );
                bufferStart = index + 1;

            }
            else
            {//append entire buffer and continue for next read
                sb.Append( buffer, bufferStart, bufferLen );
            }
        }
        protected void Read()
        {
            //try to fill the buffer
            bufferLen = file.Read( buffer, 0, bufferSize );
            bufferStart = 0;
            if ( bufferLen == 0 )
            {
                finishedBlock = true;
                finishedFile = true;
            }
        }
        #endregion
    }
}
