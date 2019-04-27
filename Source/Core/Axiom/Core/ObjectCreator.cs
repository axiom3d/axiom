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
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.IO;
using System.Linq;
using System.Reflection;

#endregion Namespace Declarations

namespace Axiom.Core
{
    /// <summary>
    /// Used by configuration classes to store assembly/class names and instantiate
    /// objects from them.
    /// </summary>
    public class ObjectCreator
    {
        private readonly Assembly _assembly;
        private readonly Type _type;

        public Type CreatedType
        {
            get
            {
                return this._type;
            }
        }

        public ObjectCreator(Type type)
            : this(type.Assembly, type)
        {
        }

        public ObjectCreator(Assembly assembly, Type type)
        {
            this._assembly = assembly;
            this._type = type;
        }

        public ObjectCreator(string assemblyName, string className)
        {
            var assemblyFile = Path.Combine(System.IO.Directory.GetCurrentDirectory(), assemblyName);
            try
            {
#if SILVERLIGHT || NETFX_CORE
				_assembly = Assembly.Load(assemblyFile);
#else
                this._assembly = Assembly.LoadFrom(assemblyFile);
#endif
            }
            catch (Exception)
            {
                this._assembly = Assembly.GetExecutingAssembly();
            }

            this._type = this._assembly.GetType(className);
        }

        public ObjectCreator(string className)
        {
            this._assembly = Assembly.GetExecutingAssembly();
            this._type = this._assembly.GetType(className);
        }

        public string GetAssemblyTitle()
        {
#if NETFX_CORE
            var title = (from attr in this._assembly.CustomAttributes where attr.AttributeType == typeof(AssemblyTitleAttribute) select attr).FirstOrDefault().;
#else
            var title = Attribute.GetCustomAttribute(this._assembly, typeof(AssemblyTitleAttribute));
            if (title == null)
            {
                return this._assembly.GetName().Name;
            }
            return ((AssemblyTitleAttribute)title).Title;
#endif
        }

        public T CreateInstance<T>() where T : class
        {
            var type = this._type;
            var assembly = this._assembly;
#if !(XBOX || XBOX360)
            // Check interfaces or Base type for casting purposes
            if (type.GetInterface(typeof(T).Name, false) != null || type.BaseType.Name == typeof(T).Name)
#else
			bool typeFound = false;
			for (int i = 0; i < type.GetInterfaces().GetLength(0); i++)
			{
				if ( type.GetInterfaces()[ i ] == typeof( T ) )
				{
					typeFound = true;
					break;
				}
			}

			if ( typeFound )
#endif
            {
                try
                {
                    return (T)Activator.CreateInstance(type);
                }
                catch (Exception e)
                {
                    LogManager.Instance.Write("Failed to create instance of {0} of type {0} from assembly {1}", typeof(T).Name, type, assembly.FullName);
                    LogManager.Instance.Write(LogManager.BuildExceptionString(e));
                }
            }
            return null;
        }
    }
}