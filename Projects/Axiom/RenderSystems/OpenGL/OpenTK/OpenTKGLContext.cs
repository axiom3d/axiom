#region LGPL License

/*
Axiom Graphics Engine Library
Copyright © 2003-2011 Axiom Project Team

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
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id:$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Windows.Forms;

using Axiom.Core;

using OpenTK;
using OpenTK.Graphics;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.OpenGL
{
	internal class OpenTKGLContext : GLContext
	{
		private GLControl glControl;
		private GraphicsContext graphicsContext;
		private OpenTK.Platform.IWindowInfo windowInfo;

		public OpenTKGLContext( OpenTK.Platform.IWindowInfo windowInfo )
		{
			// setup created glcontrol / gtk control
			this.windowInfo = windowInfo;
			graphicsContext = new GraphicsContext( GraphicsMode.Default, this.windowInfo );
			Initialized = true;
		}

		public OpenTKGLContext( OpenTK.Graphics.GraphicsMode mode, OpenTK.Platform.IWindowInfo windowInfo )
		{
			this.windowInfo = windowInfo;
			graphicsContext = new GraphicsContext( mode, this.windowInfo );
			Initialized = true;
		}

		public OpenTKGLContext( Control control, Control parent )
		{
			// replaces form's (parent) picturebox (control) by glControl
			glControl = new GLControl();
			glControl.VSync = false;
			glControl.Dock = control.Dock;
			glControl.BackColor = control.BackColor;
			glControl.Location = control.Location;
			glControl.Name = control.Name;
			glControl.Size = control.Size;
			glControl.TabIndex = control.TabIndex;
			glControl.Show();

			int count = 0;
			while ( glControl.Context == null && ++count < 10 )
			{
				System.Threading.Thread.Sleep( 10 );
			}
			if ( glControl.Context == null )
				throw new Exception( "glControl.Context == null" );

			Form form = (Form)parent;
			form.Controls.Add( glControl );
			control.Hide();

			if ( ResourceGroupManager.Instance.FindResourceFileInfo( ResourceGroupManager.DefaultResourceGroupName, "AxiomIcon.ico" ).Count > 0 )
			{
				using ( System.IO.Stream icon = ResourceGroupManager.Instance.OpenResource( "AxiomIcon.ico" ) )
				{
					if ( icon != null )
						form.Icon = new System.Drawing.Icon( icon );
				}
			}
			Initialized = true;
		}

		public override bool VSync
		{
			get
			{
				return graphicsContext.VSync;
			}
			set
			{
				graphicsContext.VSync = value;
			}
		}

		public void SwapBuffers()
		{
			if ( glControl != null )
			{
				glControl.MakeCurrent();
				glControl.SwapBuffers();
			}
			else if ( graphicsContext != null )
			{
				graphicsContext.MakeCurrent( windowInfo );
				graphicsContext.SwapBuffers();
			}
		}

		public override void SetCurrent()
		{
			if ( glControl != null )
			{
				glControl.MakeCurrent();
			}
			else if ( graphicsContext != null )
			{
				graphicsContext.MakeCurrent( windowInfo );
			}
		}

		public override void EndCurrent()
		{
		}

		public override GLContext Clone()
		{
			throw new NotImplementedException();
			return null;
		}
	}
}