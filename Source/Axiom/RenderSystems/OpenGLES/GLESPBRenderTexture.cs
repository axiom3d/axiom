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
//     <id value="$Id$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Axiom.Media;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.OpenGLES
{
	public class GLESPBRenderTexture : GLESRenderTexture
	{
		#region Fields and Properties

		private GLESPBRTTManager _manager;
		private PixelComponentType _pbFormat;

		#endregion Fields and Properties

		#region Construction and Destruction

		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		/// <param name="target"></param>
		/// <param name="writeGamma"></param>
		/// <param name="fsaa"></param>
		public GLESPBRenderTexture( GLESPBRTTManager manager, string name, GLESSurfaceDescription target, bool writeGamma, int fsaa )
			: base( name, target, writeGamma, fsaa )
		{
			this._manager = manager;
			this._pbFormat = PixelUtil.GetComponentType( target.Buffer.Format );
			this._manager.RequestPBuffer( _pbFormat, Width, Height );
		}

		#endregion Construction and Destruction

		#region GLESRenderTexture Implementation

		public override object this[ string attribute ]
		{
			get
			{
				switch ( attribute.ToLower() )
				{
					case "target":
						GLESSurfaceDescription target = new GLESSurfaceDescription();
						target.Buffer = (GLESHardwarePixelBuffer)pixelBuffer;
						target.ZOffset = zOffset;
						return target;
				}
				return base[ attribute ];
			}
		}

		protected override void dispose( bool disposeManagedResources )
		{
			if ( !IsDisposed )
			{
				if ( disposeManagedResources )
				{
					this._manager.ReleasePBuffer( _pbFormat );
					_manager = null;
				}
			}
			base.dispose( disposeManagedResources );
		}

		#endregion GLESRenderTexture Implementation
	}
}

