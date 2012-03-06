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
using System.Drawing;
using System.Windows.Forms;
using Axiom.Core;
using Axiom.Graphics;
using IO = System.IO;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.DirectX9
{
	public sealed class DefaultForm : Form
	{
	    private readonly WindowClassStyle _classStyle;
	    private readonly WindowsExtendedStyle _dwStyleEx;
	    
        #region RenderWindow

        private RenderWindow _renderWindow;

        /// <summary>
        ///	Get/Set the RenderWindow associated with this form.
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

        #endregion RenderWindow

        #region WindowStyles

        private WindowStyles _windowStyle;

        /// <summary>
        /// Get/Set window styles
        /// </summary>
        public WindowStyles WindowStyles
        {
            get
            {
                return _windowStyle;
            }

            set
            {
                _windowStyle = value;
            }
        }

        #endregion WindowStyles

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

        public DefaultForm( WindowClassStyle classStyle, WindowsExtendedStyle dwStyleEx, string title,
            WindowStyles windowStyle, int left, int top, int winWidth, int winHeight, Control parentHWnd )
        {
            _classStyle = classStyle;
            _dwStyleEx = dwStyleEx;
            _windowStyle = windowStyle;

            SuspendLayout();

            BackColor = Color.Black;
            Name = title;
            Left = left;
            Top = top;
            Width = winWidth;
            Height = winHeight;
            if ( parentHWnd != null )
                Parent = parentHWnd;

            Load += _defaultFormLoad;
            Deactivate += _defaultFormDeactivate;
            Activated += _defaultFormActivated;
            Closing += _defaultFormClose;
            Resize += _defaultFormResize;
            Cursor.Hide();

            ResumeLayout( false );
        }
        
	    protected override void WndProc( ref Message m )
		{
			if ( !Win32MessageHandling.WndProc( _renderWindow, ref m ) )
				base.WndProc( ref m );
		}

		public void _defaultFormDeactivate( object source, EventArgs e )
		{
			if ( _renderWindow != null )
				_renderWindow.IsActive = false;
		}

        public void _defaultFormActivated( object source, EventArgs e )
		{
			if ( _renderWindow != null )
				_renderWindow.IsActive = true;
		}

		public void _defaultFormClose( object source, System.ComponentModel.CancelEventArgs e )
		{
			// set the window to inactive
			if ( _renderWindow != null )
				_renderWindow.IsActive = false;
		}

		private void _defaultFormLoad( object sender, EventArgs e )
		{
			try
			{
				var strm = ResourceGroupManager.Instance.OpenResource( "AxiomIcon.ico", ResourceGroupManager.BootstrapResourceGroupName );
				if ( strm != null )
					Icon = new Icon( strm );
			}
			catch ( IO.FileNotFoundException )
			{
			}
		}

		private void _defaultFormResize( object sender, EventArgs e )
		{
			Root.Instance.SuspendRendering = WindowState == FormWindowState.Minimized;
		}
	};
}