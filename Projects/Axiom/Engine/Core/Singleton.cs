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
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <id value="$Id: Singleton.cs 1537 2009-03-30 19:25:01Z borrillis $"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Reflection;

#endregion Namespace Declarations

namespace Axiom.Core
{

	public interface ISingleton<T> where T : class
	{
		bool Initialize( params object[] args );
	}

	/// <summary>
	/// A generic singleton
	/// </summary>
	/// <remarks>
	/// Although this class will allow it, don't try to do this: Singleton&lt; interface &gt;
	/// </remarks>
	/// <typeparam name="T">a class</typeparam>
	public abstract class Singleton<T> : IDisposable where T : class, new()
	{

		public Singleton()
		{
			if ( SingletonFactory.instance != null && !IntPtr.ReferenceEquals( this, SingletonFactory.instance ) )
				throw new Exception( String.Format( "Cannot create instances of the {0} class. Use the static Instance property instead.", this.GetType().Name ) );
		}

		~Singleton()
		{
			dispose( false );
		}

		public virtual bool Initialize( params object[] args )
		{
			return true;
		}

		public static T Instance
		{
			get
			{
				try
				{
					if ( SingletonFactory.instance != null )
						return SingletonFactory.instance;
					lock ( SingletonFactory.singletonLock )
					{
						SingletonFactory.instance = new T();
						return SingletonFactory.instance;
					}
				}
				catch ( /*TypeInitialization*/Exception )
				{
					throw new Exception( string.Format( "Type {0} must implement a private parameterless constructor.", typeof( T ) ) );
				}
			}
		}

		class SingletonFactory
		{
			internal static object singletonLock = new object();
			static SingletonFactory()
			{

			}

			internal static T instance = new T();
		}

		public static void Destroy()
		{
			SingletonFactory.instance = null;
		}

		public static void Reinitialize()
		{
		}
		#region IDisposable Implementation


		#region isDisposed Property

		private bool _disposed = false;
		/// <summary>
		/// Determines if this instance has been disposed of already.
		/// </summary>
		protected bool isDisposed
		{
			get
			{
				return _disposed;
			}
			set
			{
				_disposed = value;
			}
		}

		#endregion isDisposed Property

		/// <summary>
		/// Class level dispose method
		/// </summary>
		/// <remarks>
		/// When implementing this method in an inherited class the following template should be used;
		/// protected override void dispose( bool disposeManagedResources )
		/// {
		/// 	if ( !isDisposed )
		/// 	{
		/// 		if ( disposeManagedResources )
		/// 		{
		/// 			// Dispose managed resources.
		/// 		}
		///
		/// 		// There are no unmanaged resources to release, but
		/// 		// if we add them, they need to be released here.
		/// 	}
		///
		/// 	// If it is available, make the call to the
		/// 	// base class's Dispose(Boolean) method
		/// 	base.dispose( disposeManagedResources );
		/// }
		/// </remarks>
		/// <param name="disposeManagedResources">True if Unmanaged resources should be released.</param>
		protected virtual void dispose( bool disposeManagedResources )
		{
			if ( !isDisposed )
			{
				if ( disposeManagedResources )
				{
					Singleton<T>.Destroy();
				}

				// There are no unmanaged resources to release, but
				// if we add them, they need to be released here.
			}
			isDisposed = true;
		}

		public void Dispose()
		{
			dispose( true );
			GC.SuppressFinalize( this );
		}

		#endregion IDisposable Implementation
	}
}