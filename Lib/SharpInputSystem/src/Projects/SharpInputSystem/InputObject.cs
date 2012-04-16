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

#endregion Namespace Declarations

namespace SharpInputSystem
{
    /// <summary>
    /// 
    /// </summary>
    public enum InputType
    {
        Unknown,
        Keyboard,
        Mouse,
        Joystick,
        Tablet
    }

	public interface IInputObjectInterface
	{
	}

    abstract public class InputObjectEventArgs
    {
        private InputObject _device;
        public InputObject Device
        {
            get
            {
                return _device;
            }
            protected set
            {
                _device = value;
            }
        }

        public InputObjectEventArgs( InputObject obj )
        {
            _device = obj;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    abstract public class InputObject : IDisposable
    {

        private InputType _type;
        /// <summary>
        /// Get the type of device
        /// </summary>
        public InputType Type
        {
            get
            {
                return _type;
            }
            protected set
            {
                _type = value;
            }
        }

        private string _vendor;
        /// <summary>
        /// Get the vender string name
        /// </summary>
        public String Vendor
        {
            get
            {
                return _vendor;
            }
            protected set
            {
                _vendor = value;
            }
        }

        private InputManager _creator;
        /// <summary>
        /// Returns this input object's creator
        /// </summary>
        public InputManager Creator
        {
            get
            {
                return _creator;
            }
            protected set
            {
                _creator = value;
            }
        }

        private bool _isBuffered;
        /// <summary>
        /// Get buffered mode - true is buffered, false otherwise
        /// </summary>
        public virtual bool IsBuffered
        {
            get
            {
                return _isBuffered;
            }
            protected set
            {
                _isBuffered = value;
            }
        }

        private string _deviceID;
        /// <summary>
        /// Not fully implemented yet
        /// </summary>
        public string DeviceID
        {
            get
            {
                return _deviceID;
            }
            protected set
            {
                _deviceID = value;
            }
        }

        /// <summary>
        /// Used for updating call once per frame before checking state or to update events
        /// </summary>
        abstract public void Capture();

        /// <summary>
        /// 
        /// </summary>
        abstract internal void initialize();

		virtual public IInputObjectInterface QueryInterface<T>() where T : IInputObjectInterface
		{
			return default( T );
		}

        #region IDisposable Implementation

        private bool _disposed = false;
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

        protected virtual void _dispose( bool disposeManagedResources )
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
            isDisposed = true;

            // If it is available, make the call to the
            // base class's Dispose(Boolean) method
            //base._dispose( disposeManagedResources );
        }

        public void Dispose()
        {
            _dispose( true );
            GC.SuppressFinalize( this );
        }

        ~InputObject()
        {
            _dispose( false );
        }

        #endregion IDisposable Implementation
    }
}
