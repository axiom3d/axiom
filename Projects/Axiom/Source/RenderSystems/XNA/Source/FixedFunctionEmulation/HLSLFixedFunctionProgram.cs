#region LGPL License
/*
Axiom Graphics Engine Library
Copyright (C) 2003-2006 Axiom Project Team

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
//     <license see="http://axiomengine.sf.net/wiki/index.php/license.txt"/>
//     <id value="$Id: HLSLShaderGenerator.cs 1239 2008-03-07 21:54:34Z borrillis $"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;

using Axiom.Graphics;
using Axiom.RenderSystems.Xna.HLSL;

using XNA = Microsoft.Xna.Framework;
using XFG = Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Axiom.Core;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.Xna.FixedFunctionEmulation
{
    class HLSLFixedFunctionProgram : Axiom.RenderSystems.Xna.FixedFunctionEmulation.FixedFunctionPrograms
    {
        public HLSLFixedFunctionProgram()
        {
            vertexProgramUsage = new GpuProgramUsage(GpuProgramType.Vertex);
            fragmentProgramUsage = new GpuProgramUsage(GpuProgramType.Fragment);
            fixedFunctionState = new FixedFunctionState();
        }

        public override void SetFixedFunctionProgramParameters(Axiom.RenderSystems.Xna.FixedFunctionEmulation.FixedFunctionPrograms.FixedFunctionProgramsParameters parameters)
        {
            _setProgramMatrix4Parameter(GpuProgramType.Vertex, "World", parameters.WorldMatrix);
            _setProgramMatrix4Parameter(GpuProgramType.Vertex, "View", parameters.ViewMatrix);
            _setProgramMatrix4Parameter(GpuProgramType.Vertex, "Projection", parameters.ProjectionMatrix);
            _setProgramMatrix4Parameter(GpuProgramType.Vertex, "ViewIT", parameters.ViewMatrix.Inverse().Transpose());


            Axiom.Math.Matrix4 WorldViewIT = parameters.ViewMatrix * parameters.WorldMatrix;
            WorldViewIT = WorldViewIT.Inverse().Transpose();
            _setProgramMatrix4Parameter(GpuProgramType.Vertex, "WorldViewIT", WorldViewIT);

            #region shaderLights
            if (parameters.LightingEnabled && parameters.Lights.Count > 0)
            {
                _setProgramColorParameter(GpuProgramType.Vertex, "BaseLightAmbient", parameters.LightAmbient);

                uint pointLightCount = 0;
                uint directionalLightCount = 0;
                uint spotLightCount = 0;
                for (int i = 0; i < parameters.Lights.Count; i++)
                {
                    Axiom.Core.Light curLight = parameters.Lights[i];
                    String prefix = "";

                    switch (curLight.Type)
                    {
                        case LightType.Point:
                            prefix = "PointLight" + Axiom.Core.StringConverter.ToString(pointLightCount) + "_";
                            pointLightCount++;
                            break;
                        case LightType.Directional:
                            prefix = "DirectionalLight" + Axiom.Core.StringConverter.ToString(directionalLightCount) + "_";
                            directionalLightCount++;
                            break;
                        case LightType.Spotlight:
                            prefix = "SpotLight" + Axiom.Core.StringConverter.ToString(spotLightCount) + "_";
                            spotLightCount++;
                            break;
                    }

                    _setProgramColorParameter(GpuProgramType.Vertex, prefix + "Ambient", Axiom.Core.ColorEx.Black);
                    _setProgramColorParameter(GpuProgramType.Vertex, prefix + "Diffuse", curLight.Diffuse);
                    _setProgramColorParameter(GpuProgramType.Vertex, prefix + "Specular", curLight.Specular);

                    switch (curLight.Type)
                    {
                        case LightType.Point:
                            {
                                _setProgramVector3Parameter(GpuProgramType.Vertex, prefix + "Position", new Microsoft.Xna.Framework.Vector3(curLight.Position.x, curLight.Position.y, curLight.Position.z));
                                _setProgramFloatParameter(GpuProgramType.Vertex, prefix + "Range", curLight.AttenuationRange);

                                Axiom.Math.Vector3 attenuation = new Axiom.Math.Vector3();
                                attenuation[0] = curLight.AttenuationConstant;
                                attenuation[1] = curLight.AttenuationLinear;
                                attenuation[2] = curLight.AttenuationQuadratic;
                                _setProgramVector3Parameter(GpuProgramType.Vertex, prefix + "Attenuation", new Microsoft.Xna.Framework.Vector3(attenuation.x, attenuation.y, attenuation.z));
                            }
                            break;
                        case LightType.Directional:
                            _setProgramVector3Parameter(GpuProgramType.Vertex, prefix + "Direction", new Microsoft.Xna.Framework.Vector3(curLight.Direction.x, curLight.Direction.y, curLight.Direction.z));

                            break;
                        case LightType.Spotlight:
                            {

                                _setProgramVector3Parameter(GpuProgramType.Vertex, prefix + "Direction", new Microsoft.Xna.Framework.Vector3(curLight.Direction.x, curLight.Direction.y, curLight.Direction.z));
                                _setProgramVector3Parameter(GpuProgramType.Vertex, prefix + "Position", new Microsoft.Xna.Framework.Vector3(curLight.Position.x, curLight.Position.y, curLight.Position.z));

                                Axiom.Math.Vector3 attenuation = new Axiom.Math.Vector3();
                                attenuation[0] = curLight.AttenuationConstant;
                                attenuation[1] = curLight.AttenuationLinear;
                                attenuation[2] = curLight.AttenuationQuadratic;
                                _setProgramVector3Parameter(GpuProgramType.Vertex, prefix + "Attenuation", new Microsoft.Xna.Framework.Vector3(attenuation.x, attenuation.y, attenuation.z));

                                Axiom.Math.Vector3 spot = new Axiom.Math.Vector3();
                                spot[0] = curLight.SpotlightInnerAngle;//.valueRadians() ;
                                spot[1] = curLight.SpotlightOuterAngle;//..valueRadians();
                                spot[2] = curLight.SpotlightFalloff;
                                _setProgramVector3Parameter(GpuProgramType.Vertex, prefix + "Spot", new Microsoft.Xna.Framework.Vector3(spot.x, spot.y, spot.z));
                            }
                            break;
                    } // end of - switch (curLight->getType())
                } // end of - for(size_t i = 0 ; i < params.getLights().size() ; i++) 
            } // end of -  if (params.getLightingEnabled())
            #endregion

            switch (parameters.FogMode)
            {
                case FogMode.None:
                    break;
                case FogMode.Exp:
                case FogMode.Exp2:
                    _setProgramFloatParameter(GpuProgramType.Vertex, "FogDensity", parameters.FogDensity);
                    break;
                case FogMode.Linear:
                    _setProgramFloatParameter(GpuProgramType.Vertex, "FogStart", parameters.FogStart);
                    _setProgramFloatParameter(GpuProgramType.Vertex, "FogEnd", parameters.FogEnd);
                    break;
            }

            if (parameters.FogMode != FogMode.None)
            {
                _setProgramColorParameter(GpuProgramType.Vertex, "FogColor", parameters.FogColor);
            }


            for(int i = 0 ; i < parameters.TextureMatricies.Count && i <fixedFunctionState.TextureLayerStates.Count; i++)
            {
                if (parameters.TextureEnabled[i])
                {
                    _setProgramMatrix4Parameter(GpuProgramType.Vertex, "TextureMatrix" + Axiom.Core.StringConverter.ToString(i), parameters.TextureMatricies[i]);
                }
            }
        }

        public void _setProgramParameter(GpuProgramType type, String paramName, Object value, int sizeInBytes)
        {
            switch (type)
            {
                case GpuProgramType.Vertex:
                    _updateParameter(vertexProgramUsage.Params, paramName, value, sizeInBytes);
                    break;
                case GpuProgramType.Fragment:
                    _updateParameter(fragmentProgramUsage.Params, paramName, value, sizeInBytes);
                    break;

            }
        }

        public void _updateParameter(GpuProgramParameters programParameters, String paramName, Object value, int sizeInBytes)
        {
            try
            {
                programParameters.AutoAddParamName = true;


                if (value is Axiom.Math.Matrix4)
                {
                    //if (paramName != "ViewIT" && paramName != "WorldViewIT")
                        programParameters.SetConstant(programParameters.GetParamIndex(paramName), (Axiom.Math.Matrix4)value);
                } 
                else if (value is Axiom.Core.ColorEx)
                {
                    programParameters.SetConstant(programParameters.GetParamIndex(paramName), (Axiom.Core.ColorEx)value);
                }
                else if (value is Axiom.Math.Vector3)
                {
                    programParameters.SetConstant(programParameters.GetParamIndex(paramName), (Axiom.Math.Vector3)value);
                }
                else if (value is Axiom.Math.Vector4)
                {
                    programParameters.SetConstant(programParameters.GetParamIndex(paramName), (Axiom.Math.Vector4)value);
                }
                else if (value is float[])
                {
                    programParameters.SetConstant(programParameters.GetParamIndex(paramName), (float[])value);
                }
                else if ( value is int[] )
                {
                    programParameters.SetConstant(programParameters.GetParamIndex(paramName), (int[])value);
                }
                else if ( value is float )
                {
                    programParameters.SetConstant( programParameters.GetParamIndex( paramName ), new float[] { (float)value } );
                }
                else
                {
                    programParameters.SetConstant( programParameters.GetParamIndex( paramName ), (float[])value );
                }
            }
            catch ( Exception e )
            {
                LogManager.Instance.Write( LogManager.BuildExceptionString( e ) );
            }

           /* GpuConstantDefinition def = programParameters.GetFloatConstant(GetParamIndex(paramName));//.->getConstantDefinition(paramName);
		    if (def.isFloat())
		    {
			    memcpy((programParameters->getFloatPointer(def.physicalIndex)), value, sizeInBytes);
		    }
		    else
		    {
			    memcpy((programParameters->getIntPointer(def.physicalIndex)), value, sizeInBytes);
		    }*/
        }

        public void _setProgramintParameter(GpuProgramType type, String paramName, int value)
        {
            _setProgramParameter(type, paramName, value, sizeof(int));
        }

        public void _setProgramFloatParameter(GpuProgramType type, String paramName, float value)
        {
            _setProgramParameter(type, paramName, value, sizeof(float));
        }

        unsafe public void _setProgramMatrix4Parameter(GpuProgramType type, String paramName, Axiom.Math.Matrix4 value)
        {
            _setProgramParameter(type, paramName, value, sizeof(Axiom.Math.Matrix4));
        }

        public void _setProgramColorParameter(GpuProgramType type, String paramName, Axiom.Core.ColorEx value)
        {
            float[] valueAsFloat4=new float[4];
		    valueAsFloat4[0] = value.a;
		    valueAsFloat4[1] = value.r;
		    valueAsFloat4[2] = value.g;
		    valueAsFloat4[3] = value.b;
		    _setProgramParameter(type, paramName, valueAsFloat4[0], sizeof(float) * 4);
        }

        public void _setProgramVector3Parameter(GpuProgramType type, String paramName, Microsoft.Xna.Framework.Vector3 value)
        {
            float[] valueAsFloat3=new float[3];
		    valueAsFloat3[0] = value.X;
		    valueAsFloat3[1] = value.Y;
		    valueAsFloat3[2] = value.Z;
		    _setProgramParameter(type, paramName, valueAsFloat3, sizeof(float) * 3);
        }

	}
}
