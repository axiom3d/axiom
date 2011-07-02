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

#endregion

#region SVN Version Information
// <file>
// <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
// <id value="$Id:$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using Axiom;
using Axiom.Core;
using Axiom.Math;
using Axiom.Collections;

using System.Collections;

#endregion Namespace Declarations

namespace Axiom.SceneManagers.PortalConnected
{

    /// <summary>
    /// Portal data structure for connecting zones. 
    /// </summary>
    public class Portal : PortalBase
    {

        /// <summary>
        /// name generator
        /// </summary>
        private static NameGenerator<Portal> _nameGenerator = new NameGenerator<Portal>("Portal");

        private string _typeName = "Portal";
        public override string TypeName
        {
            get { return _typeName; }
        }


        /// <summary>
        /// Portal Constructor, auto name
        /// </summary>
        public Portal()
            : this(_nameGenerator.GetNextUniqueName(), PortalType.Quad)
        {
        }

        /// <summary>
        /// Portal Constructor, supply name
        /// </summary>
        /// <param name="name"></param>
        public Portal(string name)
            : this(name, PortalType.Quad)
        {
        }

        /// <summary>
        /// Portal Constructor, supply name and portal type
        /// </summary>
        /// <param name="name">string</param>
        /// <param name="type">PortalType</param>
        public Portal(string name, PortalType type)
            : base(name, type)
        {
            _targetZone = null;
            _targetZone = null;
        }


        ///connected Zone
        private PCZone _targetZone;
        public PCZone TargetZone
        {
            get { return _targetZone; }
            set { _targetZone = value; }

        }

        // Matching Portal in the target zone (usually in same world space 
        //as this portal, but pointing the opposite direction)
        private Portal _targetPortal;
        public Portal TargetPortal
        {
            get { return _targetPortal; }
            set { _targetPortal = value; }
        }
    }

    //* Factory object for creating Portal instances 
    public class PortalFactory : MovableObjectFactory
    {

        public new const string TypeName = "Portal";

        public PortalFactory()
        {

            base.Type = PortalFactory.TypeName;
            base.TypeFlag = (uint)SceneQueryTypeMask.WorldGeometry;
        }

        public void Dispose()
        {
        }

        protected override MovableObject _createInstance(string name, NamedParameterList param)
        {

            Portal portal = new Portal(name);

            if (param != null)
            {
                if (param.ContainsKey("Type"))
                {
                    switch (param["Type"].ToString())
                    {
                        case "Quad":
                            portal.Type = PortalType.Quad;
                            break;
                        case "AABB":
                            portal.Type = PortalType.AABB;
                            break;
                        case "Sphere":
                            portal.Type = PortalType.Sphere;
                            break;
                        default:
                            throw new AxiomException("Invalid portal type '" + param["type"] + "'.");
                    }

                }

                // TODO CHECK THIS
                // Common Properties ????
            }

            return portal;
        }

        public override void DestroyInstance(ref MovableObject obj)
        {
            obj = null;
        }

        //* Return true here as we want to get a unique type flag. 
        //ORIGINAL LINE: bool requestTypeFlags() const
        public bool RequestTypeFlags()
        {
            return true;
        }

    }

}