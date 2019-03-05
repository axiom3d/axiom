using System;
using System.Collections.Generic;
using System.Linq;

namespace Axiom.Core
{
    /// <summary>
    /// Monitors the object lifetime of objects that are in control of unmanaged resources.
    /// 
    /// WARNING: AXIOM_ENABLE_LOG_STACKTRACE may have significant impact on overall engine performances,
    /// due to the large amount of <see cref="DisposableObject"/>s created during Axiom's lifecycle, so
    /// please consider to enable it ONLY if really necessary.
    /// </summary>
    internal class ObjectManager : Singleton<ObjectManager>
    {
        private class ObjectEntry
        {
            public WeakReference Instance;
            public string ConstructionStack;
        };

        private readonly Dictionary<Type, List<ObjectEntry>> _objects = new Dictionary<Type, List<ObjectEntry>>();

        /// <summary>
        /// Add an object to be monitored
        /// </summary>
        /// <param name="instance">
        /// A <see cref="DisposableObject"/> to monitor for proper disposal
        /// </param>
        /// <param name="stackTrace">Creation stacktrace of the <see cref="DisposableObject"/> being tracked.</param>
        [AxiomHelper( 0, 9 )]
        public void Add( DisposableObject instance, string stackTrace )
        {
            var objectList = GetOrCreateObjectList( instance.GetType() );

            objectList.Add( new ObjectEntry
            {
                Instance = new WeakReference( instance ),
                ConstructionStack = stackTrace
            } );
        }

        /// <summary>
        ///  Remove an object from monitoring
        /// </summary>
        /// <param name="instance"></param>
        public void Remove( DisposableObject instance )
        {
            var objectList = GetOrCreateObjectList( instance.GetType() );
            var objectEntry = ( from entry in objectList
                where entry.Instance.IsAlive && entry.Instance.Target == instance
                select entry ).FirstOrDefault();
            if ( null != objectEntry )
                objectList.Remove( objectEntry );
        }


        [AxiomHelper( 0, 9 )]
        private List<ObjectEntry> GetOrCreateObjectList( Type type )
        {
            List<ObjectEntry> objectList;
            if ( !this._objects.TryGetValue( type, out objectList ) )
            {
                objectList = new List<ObjectEntry>();
                this._objects.Add( type, objectList );
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
					var msg = new System.Text.StringBuilder();
#endif
                    // Dispose managed resources.
                    foreach ( var item in this._objects )
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
}