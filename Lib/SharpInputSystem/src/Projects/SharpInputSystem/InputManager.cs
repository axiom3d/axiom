#region LGPL License
/*
Sharp Input System Library
Copyright (C) 2007 Michael Cummings

The overall design, and a majority of the core code contained within 
this library is a derivative of the open source Open Input System ( OIS ) , 
which can be found at http://www.sourceforge.net/projects/wgois.  
Many thanks to the Phillip Castaneda for maintaining such a high quality project.

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

#region Namespace Declarations

using System;
using System.Reflection;
using System.Collections.Generic;

using log4net;

#endregion Namespace Declarations

namespace SharpInputSystem
{
    public class Pair<K, T>
    {
        public K first;
        public T second;
    }

    public class Parameter : Pair<string, object>
    {
        public Parameter( string key, object value)
        {
            first = key;
            second = value;
        }
    }

    public class ParameterList : System.Collections.Generic.List<Parameter>
    {
    }

    abstract public class InputManager
    {
		private static readonly ILog log = LogManager.GetLogger( typeof( InputManager ) );

		private List<InputObjectFactory> _factories = new List<InputObjectFactory>();
		private Dictionary<InputObject, InputObjectFactory> _createdInputObjects = new Dictionary<InputObject, InputObjectFactory>();

		/// <summary>
		/// Initializes the static instance of the class
		/// </summary>
		static InputManager()
		{
			log.Info( "Static initialization complete." );
		}

        /// <summary>
        /// Creates appropriate input system dependent on platform.
        /// </summary>
        /// <param name="windowHandle">Contains OS specific window handle (such as HWND or X11 Window)</param>
        /// <returns>A reference to the created manager, or raises an Exception</returns>
        /// <exception cref="Exception">Exception</exception>
        /// <exception cref="ArgumentException">ArgumentException</exception>
        static public InputManager CreateInputSystem( object windowHandle )
        {
            ParameterList args = new ParameterList();
            args.Add( new Parameter( "WINDOW", windowHandle ) );
            return CreateInputSystem( args );
        }

        /// <summary>
        /// Creates appropriate input system dependent on platform. 
        /// </summary>
        /// <param name="args">contains OS specific info (such as HWND and HINSTANCE for window apps), and access mode.</param>
        /// <returns>A reference to the created manager, or raises an Exception</returns>
        /// <exception cref="Exception">Exception</exception>
        /// <exception cref="ArgumentException">ArgumentException</exception>
        static public InputManager CreateInputSystem( ParameterList args )
        {
            InputManager im;

            // Since this is a required paramter for all InputManagers, check it here instead of having each 
            if ( !args.Exists( delegate( Parameter p ) { return p.first.ToUpper() == "WINDOW"; } ) )
            {
				ArgumentException ae = new ArgumentException( "Cannot initialize InputManager instance, no 'WINDOW' parameter present." );
				log.Error( "", ae);
				throw ae;
            }

			log.Info( "Creating platform specific InputManager." );

#if SIS_DX_PLATFORM 
            im = new DirectXInputManager();
#elif SIS_SDL_PLATFORM
            im = new SdlInputManager();
#elif SIS_XNA_PLATFORM
            im = new XnaInputManager();
#else
			Exception ex = new Exception( "No platform library .. check build platform defines." );
			log.Error( "", ex );
            throw ex;
#endif
			im._initialize( args );
            return im;
        }

        /// <summary>
        /// Gets version of the Assembly
        /// </summary>
        virtual public string Version
        {
            get
            {
#if !XBOX360
                return ((AssemblyFileVersionAttribute)(Assembly.GetExecutingAssembly().GetCustomAttributes( typeof(AssemblyFileVersionAttribute), false )[ 0 ])).Version;
#else
                return "0.3.0.0";
#endif
            }
        }

        /// <summary>
        /// Gets the name of the current input system.. eg. "DirectX", "Sdl", "Xna", etc
        /// </summary>
        virtual public string InputSystemName
        {
            get
            {
                return ( (AssemblyConfigurationAttribute)( Assembly.GetExecutingAssembly().GetCustomAttributes( typeof( AssemblyConfigurationAttribute ), false )[ 0 ] ) ).Configuration;
            }
        }

		/// <summary>
		/// Returns the number of the specified devices discovered by OIS
		/// </summary>
		/// <typeparam name="T">Type that you are interested in</typeparam>
		/// <returns></returns>
		public int DeviceCount<T>() where T : InputObject
		{
			int deviceCount = 0;
			foreach ( InputObjectFactory factory in _factories )
			{
				deviceCount += factory.DeviceCount<T>();
			}
			return deviceCount;
		}

		/// <summary>
		/// Lists all unused devices
		/// </summary>
		/// <returns></returns>
		public IEnumerable<KeyValuePair<Type, string>> FreeDevices
		{
			get
			{
				List<KeyValuePair<Type, string>> freeDevices = new List<KeyValuePair<Type, string>>();
				foreach ( InputObjectFactory factory in _factories )
				{
					freeDevices.AddRange( factory.FreeDevices );
				}
				return freeDevices;
			}
		}


        /// <summary>
        /// Returns the type of input requested or raises Exception
        /// </summary>
        /// <param name="type"></param>
        /// <param name="buffermode"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
		public T CreateInputObject<T>( bool bufferMode, string vendor ) where T : InputObject
		{
			InputObject obj = null;

			foreach ( InputObjectFactory factory in _factories )
			{
				if ( factory.FreeDeviceCount<T>() > 0 )
				{
					if ( vendor == null || vendor == String.Empty || factory.VendorExists<T>( vendor ) )
					{
						obj = factory.CreateInputObject<T>( this, bufferMode, vendor );
						_createdInputObjects.Add( obj, factory );
					}
				}
			}

			if ( obj == null )
				throw new Exception( "No devices match requested type." );

			try
			{
				obj.initialize();
			}
			catch ( Exception e )
			{
				obj.Dispose();
				obj = null;
				throw e; //rethrow
			}

			return (T)obj;
		}

        /// <summary>
        /// Destroys Input Object
        /// </summary>
        /// <param name="inputObject"></param>
		virtual public void DestroyInputObject( InputObject inputObject )
		{
			if ( inputObject != null )
			{
				if ( _createdInputObjects.ContainsKey( inputObject ) )
				{
					( (InputObjectFactory)_createdInputObjects[ inputObject ] ).DestroyInputObject( inputObject );
					_createdInputObjects.Remove( inputObject );
				}
			}
		}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        abstract protected void _initialize( ParameterList args );

        protected InputManager()
        {
        }

		public void RegisterFactory( InputObjectFactory factory)
		{
			_factories.Add( factory );
		}

		public void UnregisterFactory( InputObjectFactory factory)
		{
			_factories.Remove( factory );
		}
    }
}
