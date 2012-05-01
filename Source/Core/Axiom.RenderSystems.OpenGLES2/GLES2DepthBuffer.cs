#region LGPL License
/*
Axiom Graphics Engine Library
Copyright (C) 2003-2010 Axiom Project Team
This file is part of Axiom.RenderSystems.OpenGLES
C# version developed by bostich.

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
#endregion LGPL License

#region SVN Version Information
// <file>
//     <license see="http://axiomengine.sf.net/wiki/index.php/license.txt"/>
//     <id value="$Id: GLES2DepthBuffer.cs 2805 2011-08-28 18:54:56Z bostich $"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations
using System;
using Axiom.Graphics;
using GLenum = OpenTK.Graphics.ES20.All;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.OpenGLES2
{
	/// <summary>
	/// OpenGL supports 2 different methods: FBO & Copy.
	/// Each one has it's own limitations. Non-FBO methods are solved using "dummy" DepthBuffers.
	/// That is, a DepthBuffer pointer is attached to the RenderTarget (for the sake of consistency)
	/// but it doesn't actually contain a Depth surface/renderbuffer (mDepthBuffer & mStencilBuffer are
	/// null pointers all the time) Those dummy DepthBuffers are identified thanks to their GL context.
	/// Note that FBOs don't allow sharing with the main window's depth buffer. Therefore even
	/// when FBO is enabled, a dummy DepthBuffer is still used to manage the windows.
	/// </summary>
	[OgreVersion( 1, 8, 0, "It's from trunk rev.'b0d2092773fb'" )]
	public class GLES2DepthBuffer : DepthBuffer
	{
		protected int _multiSampleQuality;
		protected GLES2Context _creatorContext;
		protected GLES2RenderBuffer _depthBuffer;
		protected GLES2RenderBuffer _stencilBuffer;
		protected GLES2RenderSystem _renderSystem;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="poolId"></param>
		/// <param name="renderSystem"></param>
		/// <param name="creatorContext"></param>
		/// <param name="depth"></param>
		/// <param name="stencil"></param>
		/// <param name="width"></param>
		/// <param name="height"></param>
		/// <param name="fsaa"></param>
		/// <param name="multiSampleQuality"></param>
		/// <param name="isManual"></param>
		public GLES2DepthBuffer( PoolId poolId, GLES2RenderSystem renderSystem, GLES2Context creatorContext,
			GLES2RenderBuffer depth, GLES2RenderBuffer stencil,
			int width, int height, int fsaa, int multiSampleQuality, bool isManual )
			: base( poolId, 0, width, height, fsaa, "", isManual ) 
		{
			_creatorContext = creatorContext;
			_multiSampleQuality = multiSampleQuality;
			_depthBuffer = depth;
			_stencilBuffer = stencil;
			_renderSystem = renderSystem;

			if ( _depthBuffer != null )
			{
				switch (_depthBuffer.GLFormat)
				{
					case OpenTK.Graphics.ES20.All.DepthComponent16:
                        bitDepth = 16;
                        break;
                    case GLenum.DepthComponent24Oes:
                    case GLenum.DepthComponent32Oes:
                    case GLenum.Depth24Stencil8Oes: //Packed depth / stencil
                        bitDepth = 32;
                        break;
					  
				}
			}

		}
        protected override void dispose(bool disposeManagedResources)
        {
            if (_stencilBuffer != null && _stencilBuffer != _depthBuffer)
            {
                _stencilBuffer.Dispose();
                _stencilBuffer = null;
            }
            if (_depthBuffer == null)
            {
                _depthBuffer.Dispose();
                _depthBuffer = null;
            }
            base.dispose(disposeManagedResources);
        }
		/// <summary>
		/// 
		/// </summary>
		/// <param name="renderTarget"></param>
		/// <returns></returns>
		public override bool IsCompatible( RenderTarget renderTarget )
		{
            bool retVal = false;

            //Check standard stuff first.
            if (_renderSystem.Capabilities.HasCapability(Capabilities.RTTDepthbufferResolutionLessEqual))
            {
                if (base.IsCompatible(renderTarget))
                    return false;
            }
            else
            {
                if (this.Width != renderTarget.Width ||
                    this.Height != renderTarget.Height ||
                    this.Fsaa != renderTarget.FSAA)
                    return false;
            }
            //Now check this is the appropriate format
            GLES2FrameBufferObject fbo = null;
            fbo = (GLES2FrameBufferObject)renderTarget["FBO"];

            if (fbo == null)
            {
                GLES2Context windowContext = (GLES2Context)renderTarget["GLCONTEXT"];

                //Non-FBO and FBO depth surfaces don't play along, only dummmies which match the same
                //context
                if (_depthBuffer == null && _stencilBuffer == null && _creatorContext == windowContext)
                    retVal = true;
            }
            else
            {
                //Check this isn't a dummy non-FBO depth buffer with an FBO target, don't mix them.
                //If you don't want depth buffer, use a Null Depth Buffer, not a dummy one.
                if (_depthBuffer != null || _stencilBuffer != null)
                {
                    var internalFormat = fbo.Format;
                    GLenum depthFormat = GLenum.None, stencilFormat = GLenum.None;
                    _renderSystem.GetDepthStencilFormatFor(internalFormat, ref depthFormat, ref stencilFormat);
                    bool bSameDepth = false;
                    if (_depthBuffer != null)
                    {
                        bSameDepth |= _depthBuffer.GLFormat == depthFormat;
                    }

                    bool bSameStencil = false;
                    if (_stencilBuffer == null || _stencilBuffer == _depthBuffer)
                        bSameDepth = stencilFormat == GLenum.None;
                    else
                    {
                        if (_stencilBuffer != null)
                            bSameStencil = stencilFormat == _stencilBuffer.GLFormat;
                    }

                    retVal = bSameDepth && bSameStencil;
                }
            }

            return retVal;
		}


        public GLES2Context GLContext { get { return _creatorContext; } }
        public GLES2RenderBuffer DepthBuffer { get { return _depthBuffer; } }
        public GLES2RenderBuffer StencilBuffer { get { return _stencilBuffer; } }
	}
}