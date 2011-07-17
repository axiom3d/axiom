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
#endregion LGPL License

#region SVN Version Information
// <file>
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Windows.Forms;
using IO = System.IO;
using SWF = System.Windows.Forms;

using Axiom.Core;
using Axiom.Graphics;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.DirectX9
{

	public class DefaultForm : Form
	{
	    private readonly WindowClassStyle _classStyle;
	    private readonly WindowsExtendedStyle _dwStyleEx;
	    private readonly WindowStyles _windowStyle;
	    private RenderWindow _renderWindow;

	    public DefaultForm( WindowClassStyle classStyle, WindowsExtendedStyle dwStyleEx, string title, 
            WindowStyles windowStyle, int left, int top, int winWidth, int winHeight, Control parentHWnd )
	    {
	        _classStyle = classStyle;
	        _dwStyleEx = dwStyleEx;
	        _windowStyle = windowStyle;

	        SuspendLayout();
           
            AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            BackColor = System.Drawing.Color.Black;
            ClientSize = new System.Drawing.Size(640, 480);
            Name = title;
	        Left = left;
	        Top = top;
	        Width = winWidth;
	        Height = winHeight;
            if (parentHWnd != null)
	            Parent = parentHWnd;

            Load += DefaultFormLoad;
            Deactivate += DefaultFormDeactivate;
            Activated += DefaultFormActivated;
            Closing += DefaultFormClose;
            Resize += DefaultFormResize;

            ResumeLayout(false);
	    }

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.Style = (int)_windowStyle;
                cp.ExStyle = (int)_dwStyleEx;
                cp.ClassStyle = (int)_classStyle;
                return cp;
            }
        }

	    protected override void WndProc( ref Message m )
		{
			if ( !Win32MessageHandling.WndProc( _renderWindow, ref m ) )
				base.WndProc( ref m );
		}

		/// <summary>
		/// </summary>
		public void DefaultFormDeactivate( object source, EventArgs e )
		{
			if ( _renderWindow != null )
			{
				_renderWindow.IsActive = false;
			}
		}

		/// <summary>
		/// </summary>
		public void DefaultFormActivated( object source, EventArgs e )
		{
			if ( _renderWindow != null )
			{
				_renderWindow.IsActive = true;
			}
		}

		/// <summary>
		/// </summary>
		public void DefaultFormClose( object source, System.ComponentModel.CancelEventArgs e )
		{
			// set the window to inactive
			if ( _renderWindow != null )
			{
				_renderWindow.IsActive = false;
			}
		}

		private void DefaultFormLoad( object sender, EventArgs e )
		{
			try
			{
				var strm = ResourceGroupManager.Instance.OpenResource( "AxiomIcon.ico", ResourceGroupManager.BootstrapResourceGroupName );
				if ( strm != null )
				{
					Icon = new System.Drawing.Icon( strm );
				}
			}
			catch ( IO.FileNotFoundException )
			{
			}
		}

		private void DefaultFormResize( object sender, EventArgs e )
		{
			Root.Instance.SuspendRendering = WindowState == FormWindowState.Minimized;
		}

		/// <summary>
		///		Get/Set the RenderWindow associated with this form.
		/// </summary>
		public RenderWindow RenderWindow
		{
			get
			{
				return _renderWindow;
			}
			set
			{
				_renderWindow = value;
			}
		}
	}
}