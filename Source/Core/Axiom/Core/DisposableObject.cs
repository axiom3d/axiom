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

#endregion Namespace Declarations

namespace Axiom.Core
{
#if DEBUG
	/// <summary>
	///   Monitors the object lifetime of objects that are in control of unmanaged resources. WARNING: AXIOM_ENABLE_LOG_STACKTRACE may have significant impact on overall engine performances, due to the large amount of <see
	///    cref="DisposableObject" /> s created during Axiom's lifecycle, so please consider to enable it ONLY if really necessary.
	/// </summary>
	internal class ObjectManager : Singleton<ObjectManager>
	{
		private struct ObjectEntry
		{
			public WeakReference Instance;
			public string ConstructionStack;
		};

		private readonly Dictionary<Type, List<ObjectEntry>> _objects = new Dictionary<Type, List<ObjectEntry>>();

		/// <summary>
		///   Add an object to be monitored
		/// </summary>
		/// <param name="instance"> A <see cref="DisposableObject" /> to monitor for proper disposal </param>
		/// <param name="stackTrace"> Creation stacktrace of the <see cref="DisposableObject" /> being tracked. </param>
		[AxiomHelper( 0, 9 )]
		public void Add( DisposableObject instance, string stackTrace )
		{
			var objectList = _getOrCreateObjectList( instance.GetType() );

			objectList.Add( new ObjectEntry
			                {
			                	Instance = new WeakReference( instance ),
			                	ConstructionStack = stackTrace
			                } );
		}

		[AxiomHelper( 0, 9 )]
		private List<ObjectEntry> _getOrCreateObjectList( Type type )
		{
			List<ObjectEntry> objectList;
			if ( !_objects.TryGetValue( type, out objectList ) )
			{
				objectList = new List<ObjectEntry>();
				_objects.Add( type, objectList );
			}
			return objectList;
		}

		[AxiomHelper( 0, 9 )]
		protected override void dispose( bool disposeManagedResources )
		{
			if ( !isDisposed )
			{
				if ( disposeManagedResources )
				{
					long objectCount = 0;
					var perTypeCount = new Dictionary<string, int>();

#if !(SILVERLIGHT || XBOX || XBOX360 || WINDOWS_PHONE || ANDROID) && AXIOM_ENABLE_LOG_STACKTRACE
                    var msg = new StringBuilder();
#endif
					// Dispose managed resources.
					foreach ( var item in _objects )
					{
						var typeName = item.Key.Name;
						var objectList = item.Value;
						foreach ( var objectEntry in objectList )
						{
							if ( objectEntry.Instance.IsAlive && !( (DisposableObject)objectEntry.Instance.Target ).IsDisposed )
							{
								if ( perTypeCount.ContainsKey( typeName ) )
								{
									perTypeCount[ typeName ]++;
								}
								else
								{
									perTypeCount.Add( typeName, 1 );
								}

								objectCount++;

#if !(SILVERLIGHT || XBOX || XBOX360 || WINDOWS_PHONE || ANDROID) && AXIOM_ENABLE_LOG_STACKTRACE
                                msg.AppendLine( string.Format( "An instance of {0} was not disposed properly, creation stacktrace:", typeName ) );
                                msg.AppendLine( objectEntry.ConstructionStack );
                                msg.AppendLine();
#endif
							}
						}
					}

					var report = LogManager.Instance.CreateLog( "AxiomDisposalReport.log" );
					report.Write( "[ObjectManager] Axiom Disposal Report:" );

					if ( objectCount > 0 )
					{
						report.Write( "Total of {0} objects still alive.", objectCount );
						report.Write( "Types of not disposed objects count: " + perTypeCount.Count );

						foreach ( var currentPair in perTypeCount )
						{
							report.Write( "{0} occurrence of type {1}", currentPair.Value, currentPair.Key );
						}

#if !(SILVERLIGHT || XBOX || XBOX360 || WINDOWS_PHONE || ANDROID) && AXIOM_ENABLE_LOG_STACKTRACE
                        report.Write( "Creation Stacktraces:" );
                        report.Write( msg.ToString() );
#else
						report.Write( string.Empty ); // new line
						report.Write( "Cannot get stacktrace informations about undisposed objects." );
						report.Write(
							"Maybe AXIOM_ENABLE_LOG_STACKTRACE directive is not defined or your current platfrom doesn't allow to retrieve them." );
#endif
					}
					else
					{
						report.Write( "Everything went right! Congratulations!!" );
					}
				}

				// There are no unmanaged resources to release, but
				// if we add them, they need to be released here.
			}

			// If it is available, make the call to the
			// base class's Dispose(Boolean) method
			base.dispose( disposeManagedResources );
		}
	};
#endif

	public abstract class DisposableObject : IDisposable
	{
		[AxiomHelper( 0, 9 )]
		protected DisposableObject()
		{
			IsDisposed = false;
#if DEBUG
			var stackTrace = string.Empty;
#if !(SILVERLIGHT || XBOX || XBOX360 || WINDOWS_PHONE || ANDROID) && AXIOM_ENABLE_LOG_STACKTRACE
			stackTrace = Environment.StackTrace;
    #endif
			ObjectManager.Instance.Add( this, stackTrace );
#endif
		}

		[AxiomHelper( 0, 9 )]
		~DisposableObject()
		{
			if ( !IsDisposed )
			{
				dispose( false );
			}
		}

		#region IDisposable Implementation

		/// <summary>
		///   Determines if this instance has been disposed of already.
		/// </summary>
		[AxiomHelper( 0, 9 )]
		public bool IsDisposed { get; set; }

		///<summary>
		///  Class level dispose method
		///</summary>
		///<remarks>
		///  When implementing this method in an inherited class the following template should be used; protected override void dispose( bool disposeManagedResources ) { if ( !IsDisposed ) { if ( disposeManagedResources ) { // Dispose managed resources. } // There are no unmanaged resources to release, but // if we add them, they need to be released here. } // If it is available, make the call to the // base class's Dispose(Boolean) method base.dispose( disposeManagedResources ); }
		///</remarks>
		///<param name="disposeManagedResources"> True if Unmanaged resources should be released. </param>
		[AxiomHelper( 0, 9 )]
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

		[AxiomHelper( 0, 9 )]
		public void Dispose()
		{
			dispose( true );
			GC.SuppressFinalize( this );
		}

		#endregion IDisposable Implementation
	};
}