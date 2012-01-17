#region MIT/X11 License
//Copyright © 2003-2012 Axiom 3D Rendering Engine Project
//
//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in
//all copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//THE SOFTWARE.
#endregion License

#region SVN Version Information
// <file>
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

#endregion Namespace Declarations

namespace Axiom.Core
{
    /// <summary>
    /// Numeric identifier for a workqueue request
    /// </summary>
    public struct RequestID
    {
        private uint mValue;
        
        public uint Value
        {
            get { return mValue; }
        }

        public RequestID( uint reqId )
        {
            mValue = reqId;
        }

        public static bool operator ==( RequestID lr, RequestID rr )
        {
            return lr.Value == rr.Value;
        }

        public static bool operator !=( RequestID lr, RequestID rr )
        {
            return !( lr == rr );
        }

        public static implicit operator RequestID( uint val )
        {
            return new RequestID( val );
        }

        public override bool Equals( object obj )
        {
            if ( obj != null && obj is RequestID )
                return this == (RequestID)obj;

            return false;
        }

        public override string ToString()
        {
            return mValue.ToString();
        }
    };
}
