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

using System;
using System.Collections.Generic;
using System.Text;

namespace Axiom
{
    /// <summary>
    /// Describes a subsystem 
    /// </summary>
    /// <remarks>
    /// Describing a class as a subsystem allows Axiom to initialize it upong the 
    /// creation of Root. This facilitates easier subsystem addition and provides
    /// a way to control the order in which subsystems are created by the engine.
    /// </remarks>
    // TODO: arilou: create a SubsystemManager and refactor PluginManager to remove subsystem
    // code from it
    [AttributeUsage(AttributeTargets.Class, AllowMultiple=false)]
    public class SubsystemAttribute : Attribute
    {
        private string _name = string.Empty;
        /// <summary>
        /// Subsystem name
        /// </summary>
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        /// <summary>
        /// Describes a subsystem
        /// </summary>
        /// <param name="subsystemName"></param>
        public SubsystemAttribute(string subsystemName)
        {
            _name = subsystemName;
        }
    }
}
