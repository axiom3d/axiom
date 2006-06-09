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
using System.IO;

#endregion Namespace Declarations
			
namespace Axiom
{
    /// <summary>
    /// Used by configuration classes to store assembly/class names and instantiate
    /// objects from them.
    /// </summary>
    public class ObjectCreator
    {
        public Type type;
        public string assemblyName;
        public string className;


        public ObjectCreator( Type type )
        {
            this.type = type;
        }
        public ObjectCreator( string assemblyName, string className )
        {
            this.assemblyName = assemblyName;
            this.className = className;
        }
        public Assembly GetAssembly()
        {
            if ( type != null )
                return type.Assembly;
            string assemblyFile = Path.Combine( Environment.CurrentDirectory, assemblyName );

            // load the requested assembly
            return Assembly.LoadFrom( assemblyFile );
        }
        public new Type GetType()
        {
            if ( type != null )
                return type;
            if ( className == null )
                throw new InvalidOperationException( "Cannot get the type from an assembly when the class name is null." );
            return GetAssembly().GetType( className );
        }
        public object CreateInstance()
        {
            return Activator.CreateInstance( GetType() );
        }
    }
}
