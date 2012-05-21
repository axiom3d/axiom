#region MIT/X11 License

//Copyright © 2003-2012 Axiom 3D Rendering Engine Project
//
//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in
//all copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//THE SOFTWARE.

#endregion License

#region SVN Version Information

// <file>
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Axiom.Configuration;
using Axiom.Graphics;
using Axiom.Collections;
using Axiom.Graphics.Collections;

using GL = OpenTK.Graphics.ES20.GL;

using Axiom.Core;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.OpenGLES2
{
	public abstract class GLES2Support : IDisposable
	{
		private string _version, _vendor;

		protected ConfigOptionMap Options;
		protected string ExtensionList;

		public GLES2Support()
		{
			this.Options = new ConfigOptionMap();
		}

		public virtual void AddConfig() {}

		/// <summary>
		/// Makes sure all the extra options are valid
		/// </summary>
		/// <returns>string with error message</returns>
		public abstract string ValidateConfig();

		public virtual void Start() {}

		public virtual Graphics.RenderWindow CreateWindow( bool autoCreateWindow, GLES2RenderSystem gLES2RenderSystem, string windowTitle )
		{
			//Meant to be overridden
			throw new NotImplementedException();
		}

		public virtual RenderWindow NewWindow( string name, int width, int height, bool fullScreen, NamedParameterList miscParams )
		{
			//Meant to be overridden
			throw new NotImplementedException();
		}

		internal virtual bool CheckExtension( string ext )
		{
			return this.ExtensionList.Contains( ext );
		}

		public virtual void Stop() {}

		internal virtual void InitializeExtensions()
		{
			//Set version string
			var pcVer = GL.GetString( OpenTK.Graphics.ES20.All.Version );
			if ( pcVer == null || pcVer.Length == 0 )
			{
				pcVer = "UNKOWN";
			}
			string tmpStr = pcVer;
			LogManager.Instance.Write( "GL_VERSION = " + tmpStr );
			int spacePos = -1;
			for ( int i = 0; i < tmpStr.Length; i++ )
			{
				if ( tmpStr[ i ] == ' ' )
				{
					spacePos = i;
					break;
				}
			}
			if ( spacePos != -1 )
			{
				this._version = tmpStr.Substring( 0, spacePos );
			}
			else if ( tmpStr.Contains( ' ' ) )
			{
				this._version = tmpStr.Remove( ' ' );
			}
			else
			{
				this._version = tmpStr;
			}
			//Get vendor
			tmpStr = GL.GetString( OpenTK.Graphics.ES20.All.Vendor );
			if ( tmpStr == null || tmpStr == string.Empty )
			{
				tmpStr = "UNKOWN";
			}

			LogManager.Instance.Write( "GL_VENDOR = " + tmpStr );
			spacePos = -1;
			for ( int i = 0; i < tmpStr.Length; i++ )
			{
				if ( tmpStr[ i ] == ' ' )
				{
					spacePos = i;
					break;
				}
			}
			if ( spacePos != -1 )
			{
				this._vendor = tmpStr.Substring( 0, spacePos );
			}
			else if ( tmpStr.Contains( ' ' ) )
			{
				this._vendor = tmpStr.Remove( ' ' );
			}
			else
			{
				this._vendor = tmpStr;
			}
			//Get renderer
			tmpStr = GL.GetString( OpenTK.Graphics.ES20.All.Vendor );
			if ( tmpStr == null || tmpStr == string.Empty )
			{
				tmpStr = "UNKOWN";
			}

			LogManager.Instance.Write( "GL_RENDERER = " + tmpStr );

			//Set extension list

			var pcExt = GL.GetString( OpenTK.Graphics.ES20.All.Extensions );
			if ( pcExt == null )
			{
				pcExt = string.Empty;
			}

			LogManager.Instance.Write( "GL_EXTENSIONS = " + pcExt );
			this.ExtensionList = pcExt;
		}

		public virtual void Dispose() {}

		public string GLVersion
		{
			get { return this._version; }
		}

		public string GLVendor
		{
			get { return this._vendor; }
		}

		public string ShaderCachePath { get; set; }

		public string ShaderLibraryPath { get; set; }

		/// <summary>
		/// Gets/Sets config options
		/// </summary>
		public virtual ConfigOptionMap ConfigOptions
		{
			get { return this.Options; }
			set { this.Options = value; }
		}

		public virtual int DisplayMonitorCount
		{
			get { return 1; }
		}
	}
}
