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
using System.Text;
using Axiom.Core;
using Axiom.Graphics;
using Axiom.Utilities;
using Axiom.Collections;
using Axiom.Graphics.Collections;
using OpenTK.Graphics.ES11;
using System.Collections.ObjectModel;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.OpenGLES
{
	/// <summary>
	/// 
	/// </summary>
	public abstract class GLESSupport //: OpenTK.Graphics.GraphicsBindingsBase
	{
		private string _version;
		private string _vendor;
		private string _videoCard;
		private string _shaderCachePath;
		private string _shaderLibraryPath;

		/// <summary>
		/// Stored options
		/// </summary>
		protected ConfigOptionCollection _options;

		/// <summary>
		/// This contains the complete list of supported extensions
		/// </summary>
		protected List<string> _extensionList;

		/// <summary>
		/// 
		/// </summary>
		public virtual ConfigOptionCollection ConfigOptions
		{
			get
			{
				return _options;
			}
		}

		/// <summary>
		/// Gets vendor information
		/// </summary>
		public string Vendor
		{
			get
			{
				return _vendor;
			}
		}

		/// <summary>
		/// Gets version information
		/// </summary>
		public string Version
		{
			get
			{
				return _version;
			}
		}

		/// <summary>
		/// Gets renderer information
		/// </summary>
		public string VideoCard
		{
			get
			{
				return _videoCard;
			}
		}

		public IList<string> Extensions
		{
			get
			{
				return new ReadOnlyCollection<string>( this._extensionList );
			}
		}

		/// <summary>
		/// Get's the shader cache path.
		/// </summary>
		public string ShaderCachePath
		{
			get
			{
				return _shaderCachePath;
			}
			set
			{
				_shaderCachePath = value;
			}
		}

		/// <summary>
		/// Get's the shader library path
		/// </summary>
		public string ShaderLibraryPath
		{
			get
			{
				return _shaderLibraryPath;
			}
			set
			{
				_shaderLibraryPath = value;
			}
		}

		/// <summary>
		/// Get's the amount of available Monitors.
		/// </summary>
		public int DisplayMonitorCount
		{
			get
			{
				return 1;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public GLESSupport()
		{
			_options = new ConfigOptionCollection();
			_extensionList = new List<string>();
		}

		/// <summary>
		/// 
		/// </summary>
		public abstract void AddConfig();

		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		/// <param name="value"></param>
		public virtual void SetConfigOption( string name, string value )
		{
			if ( _options[ name ] == null )
				throw new AxiomException( string.Format( "Option named {0} does not exist.", name ) );

			_options[ name ].Value = value;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public abstract string ValidateConfig();


		public abstract GLESPBuffer CreatePixelBuffer( Media.PixelComponentType ctype, int width, int height );

		/// <summary>
		/// 
		/// </summary>
		/// <param name="autoCreateWindow"></param>
		/// <param name="renderSystem"></param>
		/// <param name="windowTitle"></param>
		/// <returns></returns>
		public abstract RenderWindow CreateWindow( bool autoCreateWindow, GLESRenderSystem renderSystem, string windowTitle );

		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		/// <param name="width"></param>
		/// <param name="height"></param>
		/// <param name="fullScreen"></param>
		/// <param name="miscParams"></param>
		/// <returns></returns>
		public abstract RenderWindow NewWindow( string name, int width, int height, bool fullScreen, NamedParameterList miscParams = null );

		/// <summary>
		/// Get's the address of a function
		/// </summary>
		/// <param name="procname">name of the function</param>
		/// <returns>address of the named function</returns>
		public abstract IntPtr GetProcAddress( string procname );

		/// <summary>
		/// Initializes GL extensions, must be done AFTER the GL context has been
		/// established.
		/// </summary>
		public virtual void InitializeExtensions()
		{
			//get version
			_version = GL.GetString( All.Version );
			Contract.Requires( !string.IsNullOrEmpty( _version ) );

			//get vendor
			_vendor = GL.GetString( All.Vendor );

			//get renderer
			_videoCard = GL.GetString( All.Renderer );

			// Set extension list
			StringBuilder ext = new StringBuilder();
			_extensionList = new List<string>();
			ext.Append( GL.GetString( All.Extensions ) );
			string[] extSplit = ext.ToString().Split( ' ' );
			for ( int i = 0; i < extSplit.Length; i++ )
			{
				_extensionList.Add( extSplit[ i ] );
			}
		}

		/// <summary>
		/// Check if an extension is available
		/// </summary>
		/// <param name="extension">name of the extension to check</param>
		/// <returns>true if extension is aviable</returns>
		public virtual bool CheckExtension( string extension )
		{
			return _extensionList.Contains( extension );
		}

		/// <summary>
		/// Start anything special.
		/// </summary>
		public abstract void Start();

		/// <summary>
		/// Stop anything special.
		/// </summary>
		public abstract void Stop();

	}

}

