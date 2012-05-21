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

using Axiom.Graphics;
using Axiom.Media;

using Javax.Microedition.Khronos.Egl;

using OpenTK.Graphics;
using OpenTK.Platform.Android;

using NativeWindowType = System.IntPtr;
using NativeDisplayType = System.IntPtr;

using Axiom.Core;
using Axiom.Collections;

using OpenTK.Platform;
using OpenTK;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.OpenGLES2.Android
{
	internal class AndroidWindow : RenderWindow
	{
		protected AndroidSupport glSupport;
		protected AndroidContext context;
		protected bool closed;
		private IWindowInfo windowInfo;

		public AndroidWindow( AndroidSupport glSupport )
		{
			this.glSupport = glSupport;
			this.closed = false;
			this.context = null;
		}

		~AndroidWindow()
		{
			if ( this.context != null )
			{
				this.context = null;
			}
		}

		protected AndroidContext CreateGLContext( int handle )
		{
			return new AndroidContext( this.glSupport, this.context.GraphicsContext, this.windowInfo );
		}

		protected void GetLeftAndTopFromNativeWindow( out int left, out int top, uint width, uint height )
		{
			left = top = 0;
		}

		protected void InitNativeCreatedWindow( NamedParameterList miscParams )
		{
			LogManager.Instance.Write( "\tInitNativeCreateWindow called" );

			if ( miscParams != null )
			{
				if ( miscParams.ContainsKey( "externalWindowInfo" ) )
				{
					this.windowInfo = (IWindowInfo) miscParams[ "externalWindowInfo" ];
				}
				if ( miscParams.ContainsKey( "externalGLContext" ) )
				{
					var value = miscParams[ "externalGLContext" ];
					if ( value is IGraphicsContext )
					{
						this.context = new AndroidContext( this.glSupport, ( value as IGraphicsContext ), this.windowInfo );
					}
					else
					{
						var ex = new InvalidCastException();
						throw new AxiomException( "externalGLContext must be of type IGraphicsContext", ex );
					}
				}
			}
		}

		protected void CreateNativeWindow( int left, int top, uint width, uint height, string title )
		{
			LogManager.Instance.Write( "\tCreateNativeWindow called" );
		}

		public override void Reposition( int left, int top )
		{
			LogManager.Instance.Write( "\tReposition called" );
		}

		public override void Resize( int width, int height )
		{
			LogManager.Instance.Write( "\tresize called" );
		}

		public override void CopyContentsToMemory( PixelBox pb, FrameBuffer buffer ) {}

		public override void Create( string name, int width, int height, bool fullScreen, NamedParameterList miscParams )
		{
			LogManager.Instance.Write( "\tCreate called" );
			this.InitNativeCreatedWindow( miscParams );

			this.name = name;
			this.width = width;
			this.height = height;
			left = 0;
			top = 0;
			active = true;

			this.closed = false;
		}

		public override void Destroy()
		{
			LogManager.Instance.Write( "Destroy called" );
		}

		public override bool IsClosed
		{
			get { return this.closed; }
		}

		public override bool RequiresTextureFlipping
		{
			get { return false; }
		}

		public override object this[ string attribute ]
		{
			get
			{
				if ( attribute == "WINDOWINFO" )
				{
					return this.windowInfo;
				}
				else if ( attribute == "GLCONTEXT" )
				{
					return this.context;
				}
				return base[ attribute ];
			}
		}
	}
}
