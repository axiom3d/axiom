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
using System.IO;
using Axiom.Core;
using Axiom.Graphics;
using ResourceHandle = System.UInt64;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.Xna.HLSL
{
    /// <summary>
    /// Summary description for HLSLProgram.
    /// </summary>
    public class HLSLProgram : HighLevelGpuProgram
    {
        /// <summary>
        ///     Shader profile to target for the compile (i.e. vs1.1, etc).
        /// </summary>
        protected string target;

        /// <summary>
        ///     Entry point to compile from the program.
        /// </summary>
        protected string entry;

        /// <summary>
        /// preprocessor defines used to compile the program.
        /// </summary>
        protected string preprocessorDefines;

        public HLSLProgram( ResourceManager parent, string name, ResourceHandle handle, string group, bool isManual,
                            IManualResourceLoader loader )
            : base( parent, name, handle, group, isManual, loader )
        {
            //var device = ( (XnaRenderWindow)Root.Instance.AutoWindow ).Driver.XnaDevice;
            //switch(type)
            //{
            //    case GpuProgramType.Fragment:
            //        assemblerProgram = new XnaFragmentProgram(parent, name, handle, group, isManual, loader, device);
            //        break;
            //    case GpuProgramType.Vertex:
            //        assemblerProgram = new XnaVertexProgram( parent, name, handle, group, isManual, loader, device );
            //        break;
            //    case GpuProgramType.Geometry:
            //        break;
            //}

            preprocessorDefines = string.Empty;
        }

        /// <summary>
        ///     Creates a low level implementation based on the results of the
        ///     high level shader compilation.
        /// </summary>
        protected override void CreateLowLevelImpl()
        {
            if (!highLevelLoaded)
                LoadHighLevelImpl();
            assemblerProgram = GpuProgramManager.Instance.CreateProgramFromString( Name, Group, source, type, target );
            assemblerProgram.IsSkeletalAnimationIncluded = IsSkeletalAnimationIncluded;
        }

        public override GpuProgramParameters CreateParameters()
        {
            var parms = base.CreateParameters();

            return parms;
        }

        /// <summary>
        ///     Compiles the high level shader source to low level microcode.
        /// </summary>
        protected override void LoadFromSource()
        {
        //    if ( !highLevelLoaded )
        //        LoadHighLevel();
        //    assemblerProgram.Source = source;
        //    assemblerProgram.Load();
        }

        protected override void LoadHighLevelImpl()
        {
            using (var stream = ResourceGroupManager.Instance.OpenResource(fileName, Group, true, this))
            using (var reader = new StreamReader(stream, System.Text.Encoding.UTF8))
                source = reader.ReadToEnd();
            highLevelLoaded = true;
        }

        /// <summary>
        ///     Derives parameter names from the constant table.
        /// </summary>
        /// <param name="parms"></param>
        protected override void PopulateParameterNames( GpuProgramParameters parms )
        {
        }

        /// <summary>
        ///     Unloads data that is no longer needed.
        /// </summary>
        protected override void UnloadHighLevelImpl()
        {           
        }

        public override bool IsSupported
        {
            get
            {
                return false;
            }
        }

        protected override void BuildConstantDefinitions()
        {
            throw new NotImplementedException();
        }
    };
}