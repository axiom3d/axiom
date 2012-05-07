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

namespace Axiom.RenderSystems.OpenGLES2
{
	public abstract class GLES2Support : IDisposable
	{
		private string version, vendor;

		protected Dictionary<string, ConfigOption> options;
		protected string extensionList;

		public GLES2Support() {}

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
			return this.extensionList.Contains( ext );
		}

		internal virtual void Stop() {}

		internal virtual void InitializeExtensions()
		{
			//Set version string
			var pcVer = GL.GetString( OpenTK.Graphics.ES20.All.Version );

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
				this.version = tmpStr.Substring( 0, spacePos );
			}
			else
			{
				this.version = tmpStr.Remove( ' ' );
			}

			//Get vendor
			tmpStr = GL.GetString( OpenTK.Graphics.ES20.All.Vendor );
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
				this.vendor = tmpStr.Substring( 0, spacePos );
			}
			else
			{
				this.vendor = tmpStr.Remove( ' ' );
			}

			//Get renderer
			tmpStr = GL.GetString( OpenTK.Graphics.ES20.All.Vendor );
			LogManager.Instance.Write( "GL_RENDERER = " + tmpStr );

			//Set extension list

			var pcExt = GL.GetString( OpenTK.Graphics.ES20.All.Extensions );
			LogManager.Instance.Write( "GL_EXTENSIONS = " + pcExt );
			this.extensionList = pcExt;
		}

		public virtual void Dispose() {}

		public string GLVersion
		{
			get { return this.version; }
		}

		public string GLVendor
		{
			get { return this.vendor; }
		}

		public string ShaderCachePath { get; set; }

		public string ShaderLibraryPath { get; set; }

		/// <summary>
		/// Gets/Sets config options
		/// </summary>
		public virtual ConfigOptionMap ConfigOptions
		{
			get { return (ConfigOptionMap) this.options; }
			set { this.options = value; }
		}

		public virtual int DisplayMonitorCount
		{
			get { return 1; }
		}
	}
}
