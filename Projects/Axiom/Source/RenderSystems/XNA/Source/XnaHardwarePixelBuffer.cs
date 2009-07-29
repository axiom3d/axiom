#region LGPL License
/*
Axiom Graphics Engine Library
Copyright (C) 2003-2006  Axiom Project Team

The overall design, and a majority of the core engine and rendering code 
contained within this library is a derivative of the open source Object Oriented 
Graphics Engine OGRE, which can be found at http://ogre.sourceforge.net.  
Many thanks to the OGRE team for maintaining such a high quality project.

This library is free software; you can redistribute it and/or
modify it under the terms of the GNU Lesser General Public
License as published by the Free Software Foundation; either
version 2.1 of the License, or (at your option) any later version.

This library is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public
License along with this library; if not, write to the Free Software
Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
*/
#endregion

#region SVN Version Information
// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <id value="$Id$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Drawing;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

using Axiom.RenderSystems.Xna;

using VertexDeclaration = Axiom.Graphics.VertexDeclaration;
using Root = Axiom.Core.Root;
using Axiom.Core;
using Axiom.Graphics;
using Axiom.Media;

using XNA = Microsoft.Xna.Framework;
using XFG = Microsoft.Xna.Framework.Graphics;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.Xna
{
    /// <summary>
    /// 	Xna implementation of HardwarePixelBuffer
    /// </summary>
    public class XnaHardwarePixelBuffer : HardwarePixelBuffer
    {
        #region Fields and Properties

        ///<summary>
        ///    Accessor for surface
        ///</summary>
        public XFG.Texture FSAASurface
        {
            get
            {
                return null;
            }
        }


        ///<summary>
        ///    Accessor for surface
        ///</summary>
        public XFG.Texture Surface
        {
            get
            {
                return null;
            }
        }

        #endregion Fields and Properties

        #region Construction and Destruction

        ///<summary>
        ///</summary>
        ///<param name="width"></param>
        ///<param name="height"></param>
        ///<param name="depth"></param>
        ///<param name="format"></param>
        ///<param name="usage"></param>
        ///<param name="useSystemMemory"></param>
        ///<param name="useShadowBuffer"></param>
        public XnaHardwarePixelBuffer( int width, int height, int depth, PixelFormat format, BufferUsage usage, bool useSystemMemory, bool useShadowBuffer )
            : base( width, height, depth, format, usage, useSystemMemory, useShadowBuffer )
        {
        }

        #endregion Construction and Destruction

        #region HardwarePixelBuffer Implementation

        ///<summary>
        ///    Copies a region from normal memory to a region of this pixelbuffer. The source
        ///    image can be in any pixel format supported by Axiom, and in any size. 
        ///</summary>
        ///<param name="src">PixelBox containing the source pixels and format in memory</param>
        ///<param name="dstBox">Image.BasicBox describing the destination region in this buffer</param>
        ///<remarks>
        ///    The source and destination regions dimensions don't have to match, in which
        ///    case scaling is done. This scaling is generally done using a bilinear filter in hardware,
        ///    but it is faster to pass the source image in the right dimensions.
        ///    Only call this function when both  buffers are unlocked. 
        ///</remarks>
        public override void BlitFromMemory( PixelBox src, BasicBox dstBox )
        {
            throw new NotImplementedException();
        }

        ///<summary>
        ///    Copies a region of this pixelbuffer to normal memory.
        ///</summary>
        ///<param name="srcBox">BasicBox describing the source region of this buffer</param>
        ///<param name="dst">PixelBox describing the destination pixels and format in memory</param>
        ///<remarks>
        ///    The source and destination regions don't have to match, in which
        ///    case scaling is done.
        ///    Only call this function when the buffer is unlocked. 
        ///</remarks>
        public override void BlitToMemory( BasicBox srcBox, PixelBox dst )
        {
            throw new NotImplementedException();
        }

        ///<summary>
        ///    Internal implementation of <see cref="HardwareBuffer.Lock"/>.
        ///</summary>
        protected override PixelBox LockImpl( BasicBox lockBox, BufferLocking options )
        {
            // Set extents and format
            PixelBox rval = new PixelBox( lockBox, Format );

            return rval;
        }

        /// <summary>
        ///     Internal implementation of <see cref="HardwareBuffer.Unlock"/>.
        /// </summary>
        protected override void UnlockImpl()
        {
            // Nothing to do here
        }

        #endregion HardwarePixelBuffer Implementation
    }
}
