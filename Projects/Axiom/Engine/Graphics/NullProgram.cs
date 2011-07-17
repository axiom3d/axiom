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
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using Axiom.Core;
using ResourceHandle = System.UInt64;

#endregion Namespace Declarations

namespace Axiom.Graphics
{
    public class NullProgram : HighLevelGpuProgram
    {
        internal NullProgram( ResourceManager creator, string name, ResourceHandle handle, string group )
            : this( creator, name, handle, group, false, null )
        {
        }

        internal NullProgram( ResourceManager creator, string name, ResourceHandle handle, string group, bool isManual, IManualResourceLoader loader )
            : base( creator, name, handle, group, isManual, loader )
        {
        }

        protected override void CreateLowLevelImpl()
        {
        }

        protected override void UnloadImpl()
        {
        }

        protected override void PopulateParameterNames( GpuProgramParameters parms )
        {
        }

        protected override void BuildConstantDefinitions()
        {
        }

        protected override void load()
        {
            // do nothing
        }

        /// <summary>
        /// always silently ignore all parameters so as not to report errors on
        /// unsupported platforms
        /// </summary>
        /// <param name="name"></param>
        /// <param name="val"></param>
        /// <returns></returns>
        public override bool SetParam( string name, string val )
        {
            return true;
        }

        /// <summary>
        /// Overridden from GpuProgram - never supported
        /// </summary>
        public override bool IsSupported
        {
            get
            {
                return false;
            }
        }

        public override int SamplerCount
        {
            get { return 0; }
        }

        protected override void LoadFromSource()
        {
        }
    }

    public class NullProgramFactory : HighLevelGpuProgramFactory
    {
        /// <summary>
        /// Get the name of the language this factory creates programs for
        /// </summary>
        public override string Language
        {
            get { return HighLevelGpuProgramManager.NullLang; }
        }

        public override HighLevelGpuProgram CreateInstance( ResourceManager creator, string name, ulong handle, string group, bool isManual, IManualResourceLoader loader )
        {
            return new NullProgram( creator, name, handle, group, isManual, loader );
        }
    }
}
