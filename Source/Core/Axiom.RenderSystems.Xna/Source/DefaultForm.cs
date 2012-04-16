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
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Axiom.Core;
using Axiom.Graphics;
using SWF = System.Windows.Forms;
using XNA = Microsoft.Xna.Framework;
using XFG = Microsoft.Xna.Framework.Graphics;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.Xna
{

    public class DefaultForm : Form
    {
        private RenderWindow renderWindow;

        public DefaultForm()
        {
            InitializeComponent();

            Deactivate += DefaultForm_Deactivate;
            Activated += DefaultForm_Activated;
            Closing += DefaultForm_Close;
            Resize += DefaultForm_Resize;
        }

        protected override void Dispose( bool disposing )
        {
            if ( !IsDisposed )
            {
                if ( disposing )
                {
                    renderWindow = null;
                }
            }

            base.Dispose( disposing );
        }

        protected override void WndProc( ref Message m )
        {
            if ( renderWindow != null )
            {
                if ( !Win32MessageHandling.WndProc( renderWindow, ref m ) )
                    base.WndProc( ref m );
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        public void DefaultForm_Deactivate( object source, EventArgs e )
        {
            if ( renderWindow != null )
            {
                renderWindow.IsActive = false;
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        public void DefaultForm_Activated( object source, EventArgs e )
        {
            if ( renderWindow != null )
            {
                renderWindow.IsActive = true;
            }
        }

        private void InitializeComponent()
        {
            SuspendLayout();
            //
            // DefaultForm
            //
            AutoScaleBaseSize = new Size( 5, 13 );
            BackColor = Color.Black;
            ClientSize = new Size( 640, 480 );
            Name = "DefaultForm";
            Load += DefaultForm_Load;
            ResumeLayout( false );

        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        public void DefaultForm_Close( object source, CancelEventArgs e )
        {
            // set the window to inactive
            if ( renderWindow != null )
            {
                renderWindow.IsActive = false;
            }
        }

        private void DefaultForm_Load( object sender, EventArgs e )
        {
            try
            {
                var strm = ResourceGroupManager.Instance.OpenResource( "AxiomIcon.ico",
                                                                       ResourceGroupManager.BootstrapResourceGroupName );
                if ( strm != null )
                {
                    Icon = new Icon( strm );
                }
            }
            catch ( FileNotFoundException )
            {
            }
        }

        private void DefaultForm_Resize( object sender, EventArgs e )
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
                return renderWindow;
            }
            set
            {
                renderWindow = value;
            }
        }
    }
}