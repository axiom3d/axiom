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
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections.Generic;
using System.Text;

#endregion Namespace Declarations

namespace Axiom.Core
{
	/// <summary>
	/// Monitors the object lifetime of objects that are in control of unmanaged resources
	/// </summary>
	internal class ObjectManager : Singleton<ObjectManager>
	{
		private struct ObjectEntry
		{
			public WeakReference Instance;
			public string ConstructionStack;
		}

		private readonly Dictionary<Type, List<ObjectEntry>> _objects = new Dictionary<Type, List<ObjectEntry>>();

	    /// <summary>
	    /// Add an object to be monitored
	    /// </summary>
	    /// <param name="instance">
	    /// A <see cref="DisposableObject"/> to monitor for proper disposal
	    /// </param>
	    /// <param name="stackTrace"></param>
	    public void Add( DisposableObject instance, string stackTrace )
		{
			var objectList = GetOrCreateObjectList( instance.GetType() );

			objectList.Add( new ObjectEntry
			{
				Instance = new WeakReference( instance ),
				ConstructionStack = stackTrace
			} );
		}

		private List<ObjectEntry> GetOrCreateObjectList( Type type )
		{
			List<ObjectEntry> objectList;
			if ( _objects.ContainsKey( type ) )
			{
				objectList = _objects[ type ];
			}
			else
			{
				objectList = new List<ObjectEntry>();
				_objects.Add( type, objectList );
			}
			return objectList;
		}

		#region Singleton<ObjectManager> Implementation

		#endregion Singleton<ObjectManager> Implementation

		#region IDisposable Implementation

		protected override void dispose( bool disposeManagedResources )
		{
			if ( !isDisposed )
			{
				if ( disposeManagedResources )
				{
					long objectCount = 0;
					var perTypeCount = new Dictionary<string, int>();

#if !(XBOX || XBOX360 || WINDOWS_PHONE)
					var msg = new StringBuilder();
#endif

                    // Dispose managed resources.
					foreach ( var item in this._objects )
					{
                        var typeName = item.Key.Name;
                        var objectList = item.Value;
                        foreach (var objectEntry in objectList)
                        {
                            if (objectEntry.Instance.IsAlive && !((DisposableObject)objectEntry.Instance.Target).IsDisposed)
                            {
                                if (perTypeCount.ContainsKey(typeName))
                                    perTypeCount[typeName]++;
                                else
                                    perTypeCount.Add(typeName, 1);

                                objectCount++;

#if !(XBOX || XBOX360 || WINDOWS_PHONE)
                                msg.AppendLine( string.Format( "An instance of {0} was not disposed properly, creation stacktrace:", typeName ) );
                                msg.AppendLine( objectEntry.ConstructionStack );
                                msg.AppendLine();
#endif
                            }
                        }
                    }

                    Log report = LogManager.Instance.CreateLog( "AxiomDisposalReport.log" );
                    report.Write( "[ObjectManager] Axiom Disposal Report:" );

                    if ( objectCount > 0 )
                    {
                        report.Write( "Total of {0} objects still alive.", objectCount );
                        report.Write( "Types of not disposed objects count: " + perTypeCount.Count );

                        foreach ( var currentPair in perTypeCount )
                            report.Write( "{0} occurrence of type {1}", currentPair.Value, currentPair.Key );

#if !(XBOX || XBOX360 || WINDOWS_PHONE)
                        report.Write( "Creation Stacktraces:" );
                        report.Write( msg.ToString() );
#endif
                    }
                    else
                        report.Write( "Everything went right! Congratulations!!" );
				}

				// There are no unmanaged resources to release, but
				// if we add them, they need to be released here.
			}

			// If it is available, make the call to the
			// base class's Dispose(Boolean) method
			base.dispose( disposeManagedResources );
		}

		#endregion IDisposable Implementation
	}

	public abstract class DisposableObject : IDisposable
	{
		protected DisposableObject()
		{
			IsDisposed = false;
#if DEBUG
#if !(SILVERLIGHT || XBOX || XBOX360 || WINDOWS_PHONE || ANDROID)
			ObjectManager.Instance.Add( this, Environment.StackTrace );
#else
			ObjectManager.Instance.Add( this, String.Empty );
#endif
#endif
		}

		~DisposableObject()
		{
			if ( !IsDisposed )
				dispose( false );
		}

		#region IDisposable Implementation

		/// <summary>
		/// Determines if this instance has been disposed of already.
		/// </summary>
		public bool IsDisposed
		{
			get;
			set;
		}

		/// <summary>
		/// Class level dispose method
		/// </summary>
		/// <remarks>
		/// When implementing this method in an inherited class the following template should be used;
		/// protected override void dispose( bool disposeManagedResources )
		/// {
		/// 	if ( !IsDisposed )
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
			if ( !IsDisposed )
			{
				if ( disposeManagedResources )
				{
					// Dispose managed resources.
				}

				// There are no unmanaged resources to release, but
				// if we add them, they need to be released here.
			}
			IsDisposed = true;
		}

		public void Dispose()
		{
			dispose( true );
			GC.SuppressFinalize( this );
		}

		#endregion IDisposable Implementation
	}
}