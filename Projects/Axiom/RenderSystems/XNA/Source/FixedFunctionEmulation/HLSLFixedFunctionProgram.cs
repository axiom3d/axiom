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
//     <id value="$Id: HLSLShaderGenerator.cs 1239 2008-03-07 21:54:34Z borrillis $"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using Axiom.Core;
using Axiom.Graphics;
using Axiom.Math;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.Xna.FixedFunctionEmulation
{
    internal class HLSLFixedFunctionProgram : FixedFunctionPrograms
    {
        public HLSLFixedFunctionProgram()
        {
            vertexProgramUsage = new GpuProgramUsage( GpuProgramType.Vertex, null );
            fragmentProgramUsage = new GpuProgramUsage( GpuProgramType.Fragment, null );
            fixedFunctionState = new FixedFunctionState();
        }

        public override void SetFixedFunctionProgramParameters( FixedFunctionProgramsParameters parameters )
        {
            _setProgramParameter( GpuProgramType.Vertex, "World", parameters.WorldMatrix );
            _setProgramParameter( GpuProgramType.Vertex, "View", parameters.ViewMatrix );
            _setProgramParameter( GpuProgramType.Vertex, "Projection", parameters.ProjectionMatrix );

            //maybe we could do an inverse function in shader
            _setProgramParameter( GpuProgramType.Vertex, "ViewIT", parameters.ViewMatrix.Inverse() );
            var WorldViewIT = parameters.ViewMatrix*parameters.WorldMatrix;
            WorldViewIT = WorldViewIT.Inverse();
            _setProgramParameter( GpuProgramType.Vertex, "WorldViewIT", WorldViewIT );


            _setProgramParameter( GpuProgramType.Vertex, "MaterialAmbient", parameters.MaterialAmbient );
            _setProgramParameter( GpuProgramType.Vertex, "MaterialDiffuse", parameters.MaterialDiffuse );
            _setProgramParameter( GpuProgramType.Vertex, "MaterialSpecular", parameters.MaterialSpecular );
            //_setProgramParameter(GpuProgramType.Vertex, "MaterialEmissive", parameters.MaterialEmissive);
            //_setProgramParameter(GpuProgramType.Vertex, "MaterialShininess", parameters.MaterialShininess);

            #region shader Lights parameters

            if ( parameters.LightingEnabled )
            {
                _setProgramParameter( GpuProgramType.Vertex, "BaseLightAmbient", parameters.LightAmbient );

                for ( var i = 0; i < parameters.Lights.Count; i++ )
                {
                    var curLight = parameters.Lights[ i ];
                    var prefix = "";
                    prefix = "Light" + i + "_";

                    _setProgramParameter( GpuProgramType.Vertex, prefix + "Ambient", ColorEx.Black );
                    _setProgramParameter( GpuProgramType.Vertex, prefix + "Diffuse", curLight.Diffuse );
                    _setProgramParameter( GpuProgramType.Vertex, prefix + "Specular", curLight.Specular );

                    switch ( curLight.Type )
                    {
                        case LightType.Point:
                        {
                            _setProgramParameter( GpuProgramType.Vertex, prefix + "Position",
                                                  curLight.GetDerivedPosition() );
                            _setProgramParameter( GpuProgramType.Vertex, prefix + "Range", curLight.AttenuationRange );
                            var attenuation = Vector3.Zero;
                            attenuation.x = curLight.AttenuationConstant;
                            attenuation.y = curLight.AttenuationLinear;
                            attenuation.z = curLight.AttenuationQuadratic;
                            _setProgramParameter( GpuProgramType.Vertex, prefix + "Attenuation", attenuation );
                        }
                            break;
                        case LightType.Directional:
                            _setProgramParameter( GpuProgramType.Vertex, prefix + "Direction", curLight.DerivedDirection );
                            break;
                        case LightType.Spotlight:
                        {
                            _setProgramParameter( GpuProgramType.Vertex, prefix + "Position",
                                                  curLight.GetDerivedPosition() );
                            _setProgramParameter( GpuProgramType.Vertex, prefix + "Direction", curLight.Direction );

                            Vector3 attenuation;
                            attenuation.x = curLight.AttenuationConstant;
                            attenuation.y = curLight.AttenuationLinear;
                            attenuation.z = curLight.AttenuationQuadratic;
                            _setProgramParameter( GpuProgramType.Vertex, prefix + "Attenuation",
                                                  new Vector4( attenuation.x, attenuation.y, attenuation.z, 1 ) );

                            Vector3 spot;
                            spot.x = curLight.SpotlightInnerAngle;
                            spot.y = curLight.SpotlightOuterAngle;
                            spot.z = curLight.SpotlightFalloff;
                            _setProgramParameter( GpuProgramType.Vertex, prefix + "Spot",
                                                  new Vector3( spot.x, spot.y, spot.z ) );
                        }
                            break;
                    } // end of - switch (curLight->getType())
                } // end of - for(size_t i = 0 ; i < params.getLights().size() ; i++)
            } // end of -  if (params.getLightingEnabled())

            #endregion

            switch ( parameters.FogMode )
            {
                case FogMode.None:
                    break;
                case FogMode.Exp:
                case FogMode.Exp2:
                    _setProgramParameter( GpuProgramType.Vertex, "FogDensity", (float)parameters.FogDensity );
                    _setProgramParameter( GpuProgramType.Fragment, "FogColor", parameters.FogColor );
                    break;
                case FogMode.Linear:
                    _setProgramParameter( GpuProgramType.Vertex, "FogStart", (float)parameters.FogStart );
                    _setProgramParameter( GpuProgramType.Vertex, "FogEnd", (float)parameters.FogEnd );
                    _setProgramParameter( GpuProgramType.Fragment, "FogColor", parameters.FogColor );
                    break;
            }

            for ( var i = 0;
                  i < parameters.TextureMatricies.Count && i < fixedFunctionState.TextureLayerStates.Count;
                  i++ )
            {
                if ( parameters.TextureEnabled[ i ] )
                {
                    _setProgramParameter( GpuProgramType.Vertex, "TextureMatrix" + StringConverter.ToString( i ),
                                          parameters.TextureMatricies[ i ] );
                }
            }
        }
    }
}