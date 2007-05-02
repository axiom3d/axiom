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
#endregion

#region SVN Version Information
// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <id value="$Id$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Reflection;

#endregion Namespace Declarations
			
namespace Axiom.Core
{
    //public abstract class Singleton<T> where T : new()
    //{
    //    public Singleton()
    //    {
    //        if ( !IntPtr.ReferenceEquals( this, SingletonFactory.instance ) )
    //            throw new Exception( String.Format( "Cannot create instances of the {0} class. Use the static Instance property instead.", this.GetType().Name ) );
    //    }

    //    public abstract bool Initialize();

    //    public static T Instance
    //    {
    //        get
    //        {
    //            return SingletonFactory.instance;
    //        }
    //    }

    //    class SingletonFactory
    //    {
    //        static SingletonFactory()
    //        {

    //        }

    //        internal static readonly T instance = new T();
    //    }
    //}

    /// <summary>
    /// A generic singleton
    /// </summary>
    /// <remarks>
    /// Although this class will allow it, don't try to do this: Singleton< interface >
    /// </remarks>
    /// <typeparam name="T">a class</typeparam>
    public abstract class Singleton<T> : IDisposable where T : class
    {
        public Singleton()
        {
            if (!IntPtr.ReferenceEquals(this, SingletonFactory.instance))
                throw new Exception(String.Format("Cannot create instances of the {0} class. Use the static Instance property instead.", this.GetType().Name));
        }

        public virtual bool Initialize( params object[] args )
        {
            return true;
        }

        public static T Instance
        {
            get
            {
                return SingletonFactory.instance;
            }
        }

        class SingletonFactory
        {
            static SingletonFactory()
            {
                
            }

            internal static T instance = (T)typeof( T ).InvokeMember( typeof( T ).Name,
                                                                               BindingFlags.CreateInstance | BindingFlags.Instance | BindingFlags.NonPublic,
                                                                               null, null, null );
        }


        #region IDisposable Members

        public virtual void Dispose()
        {
            SingletonFactory.instance = null;
        }

        #endregion
    }
}
