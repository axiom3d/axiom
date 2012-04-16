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
using System.Runtime.InteropServices;
using System.Drawing;
using System.Collections.Generic;
using Axiom.Collections;
using Axiom.Graphics;
using Axiom.Graphics.Collections;
using Axiom.Configuration;
using Axiom.Core;
using OpenTK.Graphics.ES20;

using OpenTK;

using Javax.Microedition.Khronos.Egl;
using NativeDisplayType = Java.Lang.Object;
using EGLCONTEXT = Javax.Microedition.Khronos.Egl.EGLContext;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.OpenGLES.Android
{
	class AndroidSupport : GLESSupport
	{
		protected EGLDisplay _glDisplay;
		protected NativeDisplayType _nativeDislpay;
		protected bool _isExternalDisplay;
		protected bool _randr;

		private List<KeyValuePair<Size, short>> _videoModes;
		private KeyValuePair<Size, short> _originalMode;
		private KeyValuePair<Size, short> _currentMode;
		private List<string> _sampleLevels;
		/// <summary>
		/// 
		/// </summary>
		public EGLDisplay GLDisplay
		{
			get
			{
				int[] majorMinor = new int[ 2 ];
				_glDisplay = Javax.Microedition.Khronos.Egl.EGLContext.EGL11.EglGetDisplay( _nativeDislpay );
				if ( _glDisplay == Javax.Microedition.Khronos.Egl.EGL10Consts.EglNoDisplay )
				{
					throw new AxiomException( "Couldn't open EGLDisplay " + DisplayName );
				}

				if ( !Javax.Microedition.Khronos.Egl.EGLContext.EGL11.EglInitialize( _glDisplay, majorMinor ) )
				{
					throw new AxiomException( "Couldn't open EGLDisplay " + DisplayName );
				}
				return _glDisplay;
			}
			set
			{
				_glDisplay = value;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public string DisplayName
		{
			get
			{
				return "TODO";
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public AndroidSupport()
			: base()
		{
			_videoModes = new List<KeyValuePair<Size, short>>();
			_originalMode = new KeyValuePair<Size, short>();
			_sampleLevels = new List<string>();
			_currentMode = new KeyValuePair<Size, short>();
		}

		public override void AddConfig()
		{
			ConfigOption optFullsreen = new ConfigOption( "Full Screen", "No", false );
			ConfigOption optVideoMode = new ConfigOption( "Video Mode", "640 x 320", false );
			ConfigOption optDisplayFrequenzy = new ConfigOption( "Display Frequency", "60", false );
			ConfigOption optFSAA = new ConfigOption( "FSAA", "1", false );
			ConfigOption optRTTMode = new ConfigOption( "RTT Preferred Mode", "FBO", false );
			optFullsreen.PossibleValues.Add( 0, "Yes" );
			optFullsreen.PossibleValues.Add( 1, "No" );

			optFullsreen.Value = optFullsreen.PossibleValues[ 0 ];
			int index = 0;
			foreach ( KeyValuePair<Size, short> mode in _videoModes )
			{
				string resolution = mode.Key.Width + " x " + mode.Key.Height;
				if ( !optVideoMode.PossibleValues.ContainsValue( resolution ) )
					optVideoMode.PossibleValues.Add( index++, resolution );
			}
			index = 0;
			optVideoMode.Value = _currentMode.Key.Width + " x " + _currentMode.Key.Height;

			if ( _sampleLevels.Count > 0 )
			{
				foreach ( string fssa in _sampleLevels )
					optFSAA.PossibleValues.Add( index++, fssa );

				optFSAA.Value = optFSAA.PossibleValues[ 0 ];
			}

			optRTTMode.PossibleValues.Add( 0, "FBO" );
			optRTTMode.PossibleValues.Add( 1, "PBuffer" );
			optRTTMode.PossibleValues.Add( 2, "Copy" );
			optRTTMode.Value = optRTTMode.PossibleValues[ 0 ];

			_options[ optFullsreen.Name ] = optFullsreen;
			_options[ optVideoMode.Name ] = optVideoMode;
			_options[ optDisplayFrequenzy.Name ] = optDisplayFrequenzy;
			_options[ optFSAA.Name ] = optFSAA;
			_options[ optRTTMode.Name ] = optRTTMode;

			RefreshConfig();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		/// <param name="value"></param>
		public override void SetConfigOption( string name, string value )
		{
			base.SetConfigOption( name, value );
			if ( name == "Video Mode" )
				RefreshConfig();
		}

		public override GLESPBuffer CreatePixelBuffer( Media.PixelComponentType ctype, int width, int height )
		{
			return null;
		}

		/// <summary>
		/// 
		/// </summary>
		protected void RefreshConfig()
		{
			ConfigOption optVideoMode = ConfigOptions[ "Video Mode" ];
			ConfigOption optDisplayFrequency = ConfigOptions[ "Display Frequency" ];

			int vidIndex = 0;
			int freqIndex = 0;
			int addIndex = 0;
			while ( vidIndex < optVideoMode.PossibleValues.Count && freqIndex < optDisplayFrequency.PossibleValues.Count )
			{
				optDisplayFrequency.PossibleValues.Clear();
				foreach ( KeyValuePair<Size, short> value in _videoModes )
				{
					string mode = value.Key.Width + " x " + value.Key.Height;
					if ( mode == optVideoMode.Value )
					{
						string frequenzy = value.Value.ToString() + " MHz";
						optDisplayFrequency.PossibleValues.Add( addIndex++, frequenzy );
					}
				}
				if ( optDisplayFrequency.PossibleValues.Count > 0 )
				{
					optDisplayFrequency.Value = optDisplayFrequency.PossibleValues[ 0 ];
				}
				else
				{
					optVideoMode.Value = _videoModes[ 0 ].Key.Width + " x " + _videoModes[ 0 ].Key.Height;
					optDisplayFrequency.Value = _videoModes[ 0 ].Value.ToString() + " MHz";
				}
				vidIndex++;
				freqIndex++;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="attribList"></param>
		/// <param name="elements"></param>
		/// <returns></returns>
		public EGLConfig[] ChooseGLConfig( int[] attribList, int[] elements )
		{
			EGLConfig[] configs;
			if ( Javax.Microedition.Khronos.Egl.EGLContext.EGL11.EglChooseConfig( _glDisplay, attribList, null, 0, elements ) == false )
			{
				throw new AxiomException( "Failed to choose config" );
			}

			configs = new EGLConfig[ Marshal.SizeOf( typeof( EGLConfig ) ) * elements.Length ];
			if ( Javax.Microedition.Khronos.Egl.EGLContext.EGL11.EglChooseConfig( _glDisplay, attribList, configs, configs.Length, elements ) == false )
			{
				throw new AxiomException( "Failed to choose config" );
			}

			return configs;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="glConfig"></param>
		/// <param name="attribute"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		public bool GetGLConfigAttrib( EGLConfig glConfig, int attribute, int[] value )
		{
			bool status = false;
			status = Javax.Microedition.Khronos.Egl.EGLContext.EGL11.EglGetConfigAttrib( _glDisplay, glConfig, attribute, value );
			return status;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="procname"></param>
		/// <returns></returns>
		public override IntPtr GetProcAddress( string procname )
		{
			return IntPtr.Zero;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="context"></param>
		/// <returns></returns>
		public EGLConfig GetGLConfigFromContext( EGLCONTEXT context )
		{
#warning CAN NOT CAST EGLCONFIG > INT[], how's that possible? :S
			throw new NotSupportedException();
			//EGLConfig glConfig = null;
			//if (!Javax.Microedition.Khronos.Egl.EGLContext.EGL11.EglQueryContext(_glDisplay, context,
			//    Javax.Microedition.Khronos.Egl.EGL10Consts.EglConfigId, null))
			//{
			//    throw new AxiomException("Fail to get config from context");
			//}
			//return glConfig;
		}

		public EGLConfig GetConfigFromDrawable( EGLSurface drawable, int width, int height )
		{
#warning CAN NOT CAST EGLCONFIG > INT[], how's that possible? :S
			throw new NotSupportedException();
			//EGLConfig glConfig = null;
			//if (!EGLCONTEXT.EGL11.EglQuerySurface(_glDisplay, drawable, Javax.Microedition.Khronos.Egl.EGL10Consts.EglConfigId, null))
			//{
			//    throw new AxiomException("Fail to get config from drawable");
			//}
			//EGLCONTEXT.EGL11.EglQuerySurface(_glDisplay, drawable, Javax.Microedition.Khronos.Egl.EGL10Consts.EglWidth, new int[] { width });
			//EGLCONTEXT.EGL11.EglQuerySurface(_glDisplay, drawable, Javax.Microedition.Khronos.Egl.EGL10Consts.EglHeight, new int[] { height });

			//return glConfig;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="eglDisplay"></param>
		/// <param name="glConfig"></param>
		/// <param name="shareList"></param>
		/// <returns></returns>
		public EGLCONTEXT CreateNewContext( EGLDisplay eglDisplay, EGLConfig glConfig, EGLCONTEXT shareList )
		{

			int[] contexAttrs = new int[] { 1, 2, EGL10Consts.EglNone };
			EGLCONTEXT context = null;
			if ( eglDisplay == null )
			{
				context = EGLCONTEXT.EGL11.EglCreateContext( _glDisplay, glConfig, shareList, contexAttrs );
			}
			else
			{
				context = EGLCONTEXT.EGL11.EglCreateContext( _glDisplay, glConfig, null, contexAttrs );
			}

			if ( context == null )
			{
				throw new AxiomException( "Fail to create New context" );
			}

			return context;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public override string ValidateConfig()
		{
			//TODO
			return string.Empty;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="autoCreateWindow"></param>
		/// <param name="renderSystem"></param>
		/// <param name="windowTitle"></param>
		/// <returns></returns>
		public override RenderWindow CreateWindow( bool autoCreateWindow, GLESRenderSystem renderSystem, string windowTitle )
		{
			RenderWindow window = null;
			if ( autoCreateWindow )
			{
				NamedParameterList miscParams = new NamedParameterList();
				bool fullScreen = false;
				int width = 640;
				int height = 480;
				if ( _options[ "Full Screen" ] != null )
				{
					fullScreen = _options[ "Full Screen" ].Value == "Yes";
				}
				if ( _options[ "Display Frequency" ] != null )
				{
					miscParams[ "displayFrequency" ] = _options[ "Display Frequency" ].Value;
				}
				if ( _options[ "Video Mode" ] != null )
				{
					string val = _options[ "Video Mode" ].Value;
					int xIndex = val.IndexOf( "x" );

					if ( xIndex != -1 )
					{
						width = int.Parse( val.Substring( 0, xIndex ) );
						height = int.Parse( val.Substring( xIndex + 1 ) );
					}
				}
				if ( _options[ "FSAA" ] != null )
				{
					miscParams[ "FSAA" ] = _options[ "FSAA" ].Value;
				}

				window = renderSystem.CreateRenderWindow( windowTitle, width, height, fullScreen, miscParams );
			}
			return window;
		}

		public override Graphics.RenderWindow NewWindow( string name, int width, int height, bool fullScreen, Collections.NamedParameterList miscParams = null )
		{
			AndroidWindow window = new AndroidWindow();
			window.Create( name, width, height, fullScreen, miscParams );
			return window;
		}

		/// <summary>
		/// 
		/// </summary>
		public override void Start()
		{
			LogManager.Instance.Write( "*** Starting OpenTKGLES Subsystem ***" );
		}

		/// <summary>
		/// 
		/// </summary>
		public override void Stop()
		{
			LogManager.Instance.Write( "*** Stopping OpenTKGLES Subsystem ***" );
		}
	}
}