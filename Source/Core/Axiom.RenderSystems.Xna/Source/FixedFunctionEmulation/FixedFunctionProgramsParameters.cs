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
//     <id value="$Id:"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System.Collections.Generic;
using Axiom.Core;
using Axiom.Graphics;
using Axiom.Math;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.Xna.FixedFunctionEmulation
{
    internal partial class FixedFunctionPrograms
    {
        public class FixedFunctionProgramsParameters
        {
            #region Fields and Properties

            protected ColorEx materialAmbient;

            public ColorEx MaterialAmbient
            {
                get
                {
                    return materialAmbient;
                }
                set
                {
                    materialAmbient = value;
                }
            }

            protected ColorEx materialDiffuse;

            public ColorEx MaterialDiffuse
            {
                get
                {
                    return materialDiffuse;
                }
                set
                {
                    materialDiffuse = value;
                }
            }

            protected ColorEx materialSpecular;

            public ColorEx MaterialSpecular
            {
                get
                {
                    return materialSpecular;
                }
                set
                {
                    materialSpecular = value;
                }
            }

            protected ColorEx materialEmissive;

            public ColorEx MaterialEmissive
            {
                get
                {
                    return materialEmissive;
                }
                set
                {
                    materialEmissive = value;
                }
            }

            protected float materialShininess;

            public float MaterialShininess
            {
                get
                {
                    return materialShininess;
                }
                set
                {
                    materialShininess = value;
                }
            }

            protected Matrix4 worldMatrix;

            public Matrix4 WorldMatrix
            {
                get
                {
                    return worldMatrix;
                }
                set
                {
                    worldMatrix = value;
                }
            }

            protected Matrix4 projectionMatrix;

            public Matrix4 ProjectionMatrix
            {
                get
                {
                    return projectionMatrix;
                }
                set
                {
                    projectionMatrix = value;
                }
            }

            protected Matrix4 viewMatrix;

            public Matrix4 ViewMatrix
            {
                get
                {
                    return viewMatrix;
                }
                set
                {
                    viewMatrix = value;
                }
            }


            protected bool lightingEnabled;

            public bool LightingEnabled
            {
                get
                {
                    return lightingEnabled;
                }
                set
                {
                    lightingEnabled = value;
                }
            }


            protected List<Light> lights = new List<Light>();

            public List<Light> Lights
            {
                get
                {
                    return lights;
                }
                set
                {
                    lights = value;
                }
            }

            protected ColorEx lightAmbient;

            public ColorEx LightAmbient
            {
                get
                {
                    return lightAmbient;
                }
                set
                {
                    lightAmbient = value;
                }
            }

            protected FogMode fogMode;

            public FogMode FogMode
            {
                get
                {
                    return fogMode;
                }
                set
                {
                    fogMode = value;
                }
            }

            protected ColorEx fogColor;

            public ColorEx FogColor
            {
                get
                {
                    return fogColor;
                }
                set
                {
                    fogColor = value;
                }
            }

            protected Real fogDensity;

            public Real FogDensity
            {
                get
                {
                    return fogDensity;
                }
                set
                {
                    fogDensity = value;
                }
            }

            protected Real fogStart;

            public Real FogStart
            {
                get
                {
                    return fogStart;
                }
                set
                {
                    fogStart = value;
                }
            }

            protected Real fogEnd;

            public Real FogEnd
            {
                get
                {
                    return fogEnd;
                }
                set
                {
                    fogEnd = value;
                }
            }

            protected List<Matrix4> textureMatricies = new List<Matrix4>();

            public List<Matrix4> TextureMatricies
            {
                get
                {
                    return textureMatricies;
                }
            }

            protected List<bool> textureEnabled = new List<bool>();

            public List<bool> TextureEnabled
            {
                get
                {
                    return textureEnabled;
                }
            }

            #endregion Fields and Properties

            #region Construction and Destruction

            public FixedFunctionProgramsParameters()
            {
                fogMode = FogMode.None;
                fogColor = ColorEx.Black;
                fogDensity = 0.0f;
                fogStart = 0.0f;
                fogEnd = 0.0f;
            }

            #endregion Construction and Destruction

            #region Methods

            public void SetTextureMatrix( int index, Matrix4 matrix )
            {
                while ( index >= textureMatricies.Count )
                    textureMatricies.Add( Matrix4.Identity );

                textureMatricies[ index ] = matrix;
            }

            public void SetTextureEnabled( int index, bool value )
            {
                while ( index >= textureEnabled.Count )
                    textureEnabled.Add( false );

                textureEnabled[ index ] = value;
            }

            public bool IsTextureEnabled( int index )
            {
                if ( index > textureEnabled.Count )
                    return false;

                return textureEnabled[ index ];
            }

            #endregion Methods
        }
    }
}