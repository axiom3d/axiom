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

namespace Axiom.Media
{
    /// <summary>
    ///   Codec specialized in images.
    /// </summary>
    /// <remarks>
    ///   The users implementing subclasses of ImageCodec are required to return a valid pointer to a ImageData class from the decode(...) function.
    /// </remarks>
    public abstract class ImageCodec : Codec
    {
        [OgreVersion(1, 7, 2)]
        public override string DataType
        {
            get
            {
                return "ImageData";
            }
        }

        /// <summary>
        ///   Codec return class for images. Has information about the size and the pixel format of the image.
        /// </summary>
        public class ImageData : CodecData
        {
            [OgreVersion(1, 7, 2)]
            public override string DataType
            {
                get
                {
                    return "ImageData";
                }
            }

            public int width;
            public int height;
            public int depth = 1;
            public int size;
            public ImageFlags flags;
            public int numMipMaps;
            public PixelFormat format;
        };
    };
}