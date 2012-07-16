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
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Axiom.Collections;
using Axiom.Configuration;
using Axiom.Graphics;
using Axiom.Media;
using Tao.OpenGl;
using Axiom.Math;
using Axiom.Graphics.Collections;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.OpenGL
{
	/// <summary>
	/// Summary description for GLHelper.
	/// </summary>
	internal abstract class BaseGLSupport
	{
		#region Fields and Properties

		#region Extensions Property

		/// <summary>
		///		Collection of extensions supported by the current hardware.
		/// </summary>
		private static List<String> _extensionList;

		/// <summary>
		///		Gets a collection of strings listing all the available extensions.
		/// </summary>
		public List<String> Extensions
		{
			get
			{
				return _extensionList;
			}
		}

		#endregion Extensions Property

		#region Version Property

		/// <summary>
		///		OpenGL version string.
		/// </summary>
		private static string _glVersion;

		/// <summary>
		///		Version string for the current OpenGL driver.
		/// </summary>
		public string Version
		{
			get
			{
				return _glVersion;
			}
		}

		#endregion Version Property

		#region Vendor Property

		/// <summary>
		///		Vendor of the current hardware.
		/// </summary>
		private static string _vendor;

		/// <summary>
		///		Name of the vendor for the current video hardware.
		/// </summary>
		public string Vendor
		{
			get
			{
				return _vendor;
			}
		}

		#endregion Vendor Property

		#region VideoCard Property

		/// <summary>
		///		Name of the video card in use.
		/// </summary>
		private static string _videoCard;

		/// <summary>
		///		Name/brand of the current video hardware.
		/// </summary>
		public string VideoCard
		{
			get
			{
				return _videoCard;
			}
		}

		#endregion VideoCard Property

		#region ConfigOptions Property

		/// <summary>
		///		Config options.
		/// </summary>
		protected ConfigOptionMap _engineConfig = new ConfigOptionMap();

		/// <summary>
		///		Gets the options currently set by the current GL implementation.
		/// </summary>
		public ConfigOptionMap ConfigOptions
		{
			get
			{
				return this._engineConfig;
			}
		}

		#endregion ConfigOptions Property

		#endregion Fields and Properties

		#region Methods

		/// <summary>
		///		Handy check to see if the current GL version is at least what is supplied.
		/// </summary>
		/// <param name="version">What you want to check for, i.e. "1.3" </param>
		/// <returns></returns>
		public bool CheckMinVersion( string version )
		{
			return Utility.ParseReal( version ) <= Utility.ParseReal( _glVersion.Substring( 0, version.Length ) );
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="extention"></param>
		/// <returns></returns>
		public bool CheckExtension( string extention )
		{
			// check if the extension is supported
			return _extensionList.Contains( "GL_" + extention ) || _extensionList.Contains( extention );
		}

		/// <summary>
		///
		/// </summary>
		public void InitializeExtensions()
		{
			if ( _extensionList == null )
			{
				Gl.ReloadFunctions();

				// get the OpenGL version string and vendor name
				_glVersion = Gl.glGetString( Gl.GL_VERSION ); 
				_videoCard = Gl.glGetString( Gl.GL_RENDERER ); 
				_vendor = Gl.glGetString( Gl.GL_VENDOR ); 

				// parse out the first piece of the vendor string if there are spaces in it
				if ( _vendor.IndexOf( " " ) != -1 )
				{
					_vendor = _vendor.Substring( 0, _vendor.IndexOf( " " ) );
				}

				// create a new extension list
				_extensionList = new List<String>();

				string allExt = Gl.glGetString( Gl.GL_EXTENSIONS ); 
				string[] splitExt = allExt.Split( Char.Parse( " " ) );

				// store the parsed extension list
				for ( int i = 0; i < splitExt.Length; i++ )
				{
					_extensionList.Add( splitExt[ i ] );
				}
			}
		}

		public virtual bool SupportsPBuffers
		{
			get
			{
				return CheckExtension( "GL_ARB_pixel_buffer_object" ) || CheckExtension( "GL_EXT_pixel_buffer_object" );
			}
		}

		public int DisplayMonitorCount
		{
			get
			{
				return Screen.AllScreens.Length;
			}
		}

		public virtual GLPBuffer CreatePBuffer( PixelComponentType format, int width, int height )
		{
			return null;
		}

		#endregion Methods

		#region Abstract Members

		/// <summary>
		/// Start anything speciual
		/// </summary>
		public abstract void Start();

		/// <summary>
		/// Stop anything special
		/// </summary>
		public abstract void Stop();

		/// <summary>
		///		Add any special config values to the system.
		/// </summary>
		public abstract void AddConfig();

		/// <summary>
		///
		/// </summary>
		/// <param name="autoCreateWindow"></param>
		/// <param name="renderSystem"></param>
		/// <param name="windowTitle"></param>
		/// <returns></returns>
		public abstract RenderWindow CreateWindow( bool autoCreateWindow, GLRenderSystem renderSystem, string windowTitle );

		/// <summary>
		///		Subclasses need to implement a means to return the pointer to the extension function
		///		for OpenGL calls.
		/// </summary>
		/// <param name="extension">Name of the extension to retreive the pointer for.</param>
		/// <returns>Pointer to the location of the function in the OpenGL driver modules.</returns>
		public abstract IntPtr GetProcAddress( string extension );

		/// <summary>
		///		Creates a specific instance of a render window.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="width"></param>
		/// <param name="height"></param>
		/// <param name="colorDepth"></param>
		/// <param name="fullScreen"></param>
		/// <param name="left"></param>
		/// <param name="top"></param>
		/// <param name="depthBuffer"></param>
		/// <param name="parent"></param>
		/// <param name="vsync"></param>
		/// <returns></returns>
		public abstract RenderWindow NewWindow( string name, int width, int height, bool fullScreen, NamedParameterList miscParams );

		#endregion Abstract Members

		public string ValidateConfig()
		{
			return "";
		}
	}
}