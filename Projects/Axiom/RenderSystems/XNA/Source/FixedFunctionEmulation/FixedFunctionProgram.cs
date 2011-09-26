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
using System.Runtime.InteropServices;
using Axiom.Core;
using Axiom.Graphics;
using Axiom.Math;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.Xna.FixedFunctionEmulation
{
    internal abstract partial class FixedFunctionPrograms
    {
        #region Fields and Properties

        // Vertex program details
        protected GpuProgramUsage vertexProgramUsage;

        public GpuProgramUsage VertexProgramUsage
        {
            get
            {
                return vertexProgramUsage;
            }
            set
            {
                vertexProgramUsage = value;
            }
        }

        // Fragment program details
        protected GpuProgramUsage fragmentProgramUsage;

        public GpuProgramUsage FragmentProgramUsage
        {
            get
            {
                return fragmentProgramUsage;
            }
            set
            {
                fragmentProgramUsage = value;
            }
        }

        protected FixedFunctionState fixedFunctionState;

        public FixedFunctionState FixedFunctionState
        {
            get
            {
                return fixedFunctionState;
            }
            set
            {
                fixedFunctionState = value;
            }
        }

        #endregion Fields and Properties

        #region Construction and Destruction

        #endregion Construction and Destruction

        #region Methods

        public abstract void SetFixedFunctionProgramParameters( FixedFunctionProgramsParameters parameters );

        protected void _setProgramParameter( GpuProgramType type, String paramName, Object value, int sizeInBytes )
        {
            switch ( type )
            {
                case GpuProgramType.Vertex:
                    _updateParameter( vertexProgramUsage.Parameters, paramName, value, sizeInBytes );
                    break;
                case GpuProgramType.Fragment:
                    _updateParameter( fragmentProgramUsage.Parameters, paramName, value, sizeInBytes );
                    break;
            }
        }

        protected void _updateParameter( GpuProgramParameters programParameters, String paramName, Object value,
                                         int sizeInBytes )
        {
            programParameters.AutoAddParamName = true;
            var idx = programParameters.FindNamedConstantDefinition( paramName ).LogicalIndex;

            if ( value is Matrix4 )
            {
                programParameters.SetConstant( idx, (Matrix4)value );
            }
            else if ( value is ColorEx )
            {
                programParameters.SetConstant( idx, (ColorEx)value );
            }
            else if ( value is Vector3 )
            {
                programParameters.SetConstant( idx, ( (Vector3)value ) );
            }
            else if ( value is Vector4 )
            {
                programParameters.SetConstant( idx, (Vector4)value );
            }
            else if ( value is float[] )
            {
                programParameters.SetConstant( idx, (float[])value );
            }
            else if ( value is int[] )
            {
                programParameters.SetConstant( idx, (int[])value );
            }
            else if ( value is float )
            {
                programParameters.SetConstant( idx, new[]
                                                    {
                                                        (float)value, 0.0f, 0.0f, 0.0f
                                                    } );
            }
            else
            {
                programParameters.SetConstant( idx, (float[])value );
            }
        }

        protected void _setProgramParameter( GpuProgramType type, String paramName, int value )
        {
            _setProgramParameter( type, paramName, value, sizeof ( int ) );
        }

        protected void _setProgramParameter( GpuProgramType type, String paramName, float value )
        {
            _setProgramParameter( type, paramName, value, sizeof ( float ) );
        }

        protected void _setProgramParameter( GpuProgramType type, String paramName, Matrix4 value )
        {
            _setProgramParameter( type, paramName, value, Marshal.SizeOf( typeof ( Matrix4 ) ) );
        }

        protected void _setProgramParameter( GpuProgramType type, String paramName, ColorEx value )
        {
            _setProgramParameter( type, paramName, value, sizeof ( float )*4 );
        }

        protected void _setProgramParameter( GpuProgramType type, String paramName, Vector4 value )
        {
            _setProgramParameter( type, paramName, value, sizeof ( float )*4 );
        }

        protected void _setProgramParameter( GpuProgramType type, String paramName, Vector3 value )
        {
            _setProgramParameter( type, paramName, value, sizeof ( float )*3 );
        }

        #endregion Methods
    }
}