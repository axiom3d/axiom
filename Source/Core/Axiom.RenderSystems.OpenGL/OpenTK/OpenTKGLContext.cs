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
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using Axiom.Core;
using OpenTK;
using OpenTK.Graphics;
using System.Windows.Forms;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.OpenGL
{
	internal class OpenTKGLContext : GLContext
	{
		private readonly GLControl glControl;
		private readonly GraphicsContext graphicsContext;
		private readonly OpenTK.Platform.IWindowInfo windowInfo;

		public OpenTKGLContext( OpenTK.Platform.IWindowInfo windowInfo )
		{
			// setup created glcontrol / gtk control
			this.windowInfo = windowInfo;
			this.graphicsContext = new GraphicsContext( GraphicsMode.Default, this.windowInfo );
			Initialized = true;
		}

		public OpenTKGLContext( Control control, Control parent )
		{
			// replaces form's (parent) picturebox (control) by glControl
			this.glControl = new GLControl();
			this.glControl.VSync = false;
			this.glControl.Dock = control.Dock;
			this.glControl.BackColor = control.BackColor;
			this.glControl.Location = control.Location;
			this.glControl.Name = control.Name;
			this.glControl.Size = control.Size;
			this.glControl.TabIndex = control.TabIndex;
			this.glControl.Show();

			int count = 0;
			while ( this.glControl.Context == null && ++count < 10 )
			{
				System.Threading.Thread.Sleep( 10 );
			}
			if ( this.glControl.Context == null )
			{
				throw new Exception( "glControl.Context == null" );
			}

			var form = (Form)parent;
			form.Controls.Add( this.glControl );
			control.Hide();

			if (
				ResourceGroupManager.Instance.FindResourceFileInfo( ResourceGroupManager.DefaultResourceGroupName, "AxiomIcon.ico" )
					.Count > 0 )
			{
				using ( System.IO.Stream icon = ResourceGroupManager.Instance.OpenResource( "AxiomIcon.ico" ) )
				{
					if ( icon != null )
					{
						form.Icon = new System.Drawing.Icon( icon );
					}
				}
			}
			Initialized = true;
		}

		public override bool VSync
		{
			get
			{
				return true;
			}
			set
			{
				
			}
		}

		public void SwapBuffers()
		{
			if ( this.glControl != null )
			{
				this.glControl.MakeCurrent();
				this.glControl.SwapBuffers();
			}
			else if ( this.graphicsContext != null )
			{
				this.graphicsContext.MakeCurrent( this.windowInfo );
				this.graphicsContext.SwapBuffers();
			}
		}

		public override void SetCurrent()
		{
			if ( this.glControl != null )
			{
				this.glControl.MakeCurrent();
			}
			else if ( this.graphicsContext != null )
			{
				this.graphicsContext.MakeCurrent( this.windowInfo );
			}
		}

		public override void EndCurrent()
		{
		}

		public override GLContext Clone()
		{
			throw new NotImplementedException();
		}
	}
}