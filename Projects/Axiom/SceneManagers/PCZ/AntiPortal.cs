#region MIT/X11 License
//Copyright (c) 2009 Axiom 3D Rendering Engine Project
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
// <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
// <id value="$Id:$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections.Generic;
using Axiom.Core;
using Axiom.Math;
using Axiom.Collections;

#endregion Namespace Declarations

namespace Axiom.SceneManagers.PortalConnected
{
    public class AntiPortal : PortalBase
    {
        private static NameGenerator<AntiPortal> _nameGenerator = new NameGenerator<AntiPortal>("AntiPortal");

        public AntiPortal(string name)
            : this(name, PortalType.AABB)
        {

        }

        public AntiPortal()
            : this(_nameGenerator.GetNextUniqueName(), PortalType.AABB)
        {

        }

        public AntiPortal(PortalType type)
            : this(_nameGenerator.GetNextUniqueName(), type)
        {

        }

        public AntiPortal(string name, PortalType type)
            : base(name, type)
        {

        }

    }

    public class AntiPortalFactory : MovableObjectFactory
    {
        public new const string TypeName = "AntiPortalFactory";

        public AntiPortalFactory()
        {
            base.Type = AntiPortalFactory.TypeName;
        }

        protected override MovableObject _createInstance(string name, NamedParameterList param)
        {
            PortalType portalType = PortalType.AABB;
            // optional parameters
            if (param != null)
            {
                if (param.ContainsKey("type"))
                {
                    portalType = (PortalType)Convert.ToInt32(param["type"]);
                }
            }
            return new AntiPortal(name, portalType);
        }

    }
}
