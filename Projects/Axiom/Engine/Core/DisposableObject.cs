#region LGPL License

/*
Axiom Graphics Engine Library
Copyright (C) 2003-2006  Axiom Project Team

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

#endregion Namespace Declarations

namespace Axiom.Core
{
    internal class ObjectManager : Singleton<ObjectManager>
    {
        private readonly Dictionary<Type, List<WeakReference>> _objects = new Dictionary<Type, List<WeakReference>>();

        public void Add( DisposableObject instance )
        {
            List<WeakReference> objectList = GetOrCreateObjectList( instance.GetType() );

            objectList.Add( new WeakReference( instance ) );
        }

        private List<WeakReference> GetOrCreateObjectList( Type type )
        {
            List<WeakReference> objectList;
            if ( _objects.ContainsKey( type ) )
            {
                objectList = _objects[ type ];
            }
            else
            {
                objectList = new List<WeakReference>();
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
                    // Dispose managed resources.
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
            ObjectManager.Instance.Add( this );
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
        protected bool IsDisposed { get; set; }

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