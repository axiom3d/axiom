using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Axiom.Core;
using Axiom.Math;

// TODO: InverseTransposeWorldMatrix -> ok

namespace Axiom.Graphics
{
    public partial class GpuProgramParameters
    {
        /// <summary>
        /// Update automatic parameters.
        /// </summary>
        /// <param name="source">The source of the parameters</param>
        /// <param name="mask">A mask of GpuParamVariability which identifies which autos will need updating</param>
        public void UpdateAutoParams( AutoParamDataSource source, GpuParamVariability mask )
        {
            // abort early if no autos
            if (!HasAutoConstantType)
                return;

            if ((mask & _combinedVariability) == 0)
                return; 

            PassIterationNumberIndex = int.MaxValue;

            Matrix3 m3;
            Vector4 vec4;
            Vector3 vec3;

            // loop through and update all constants based on their type
            foreach ( var entry in autoConstants )
            {
                // Only update needed slots
                if ( ( entry.Variability & mask ) == 0 )
                    continue;

                switch ( entry.Type )
                {
                    case AutoConstantType.ViewMatrix:
                        WriteRawConstant( entry.PhysicalIndex, source.ViewMatrix, entry.ElementCount );
                        break;
                    case AutoConstantType.InverseViewMatrix:
                        WriteRawConstant( entry.PhysicalIndex, source.InverseViewMatrix, entry.ElementCount );
                        break;
                        /*
                    case AutoConstantType.TransposeViewMatrix:
                        WriteRawConstant( entry.PhysicalIndex, source.TransposeViewMatrix, entry.ElementCount );
                        break;
                         */
                    case AutoConstantType.InverseTransposeViewMatrix:
                        WriteRawConstant( entry.PhysicalIndex, source.InverseTransposeViewMatrix, entry.ElementCount );
                        break;

                    case AutoConstantType.ProjectionMatrix:
                        WriteRawConstant( entry.PhysicalIndex, source.ProjectionMatrix, entry.ElementCount );
                        break;
                        /*
                    case AutoConstantType.InverseProjectionMatrix:
                        WriteRawConstant(entry.PhysicalIndex, source.InverseProjectionMatrix, entry.ElementCount);
                        break;
                    case AutoConstantType.TransposeProjectionMatrix:
                        WriteRawConstant(entry.PhysicalIndex, source.TransposeProjectionMatrix, entry.ElementCount);
                        break;
                    case AutoConstantType.InverseTransposeProjectionMatrix:
                        WriteRawConstant(entry.PhysicalIndex, source.InverseTransposeProjectionMatrix, entry.ElementCount);
                        break;
                         */

                    case AutoConstantType.ViewProjMatrix:
                        WriteRawConstant( entry.PhysicalIndex, source.ViewProjectionMatrix, entry.ElementCount );
                        break;
                        /*
                    case AutoConstantType.InverseViewProjMatrix:
                        WriteRawConstant(entry.PhysicalIndex, source.InverseViewProjectionMatrix, entry.ElementCount);
                        break;
                    case AutoConstantType.TransposeViewProjMatrix:
                        WriteRawConstant(entry.PhysicalIndex, source.TransposeViewProjectionMatrix, entry.ElementCount);
                        break;
                    case AutoConstantType.InverseTransposeViewProjMatrix:
                        WriteRawConstant(entry.PhysicalIndex, source.InverseTransposeViewProjectionMatrix, entry.ElementCount);
                        break;
                         */


                    case AutoConstantType.RenderTargetFlipping:
                        WriteRawConstant( entry.PhysicalIndex,
                                          source.RenderTarget.RequiresTextureFlipping ? -1.0f : 1.0f );
                        break;

                    case AutoConstantType.VertexWinding:
                    {
                        var rsys = Root.Instance.RenderSystem;
                        WriteRawConstant( entry.PhysicalIndex, rsys.InvertVertexWinding ? -1.0f : 1.0f );
                        break;
                    }

                    case AutoConstantType.AmbientLightColor:
                        WriteRawConstant( entry.PhysicalIndex, source.AmbientLight, entry.ElementCount );
                        break;

                        /*
                    case AutoConstantType.DerivedSceneColor:
                        WriteRawConstant(entry.PhysicalIndex, source.DerivedSceneColor, entry.ElementCount);
                        break;

                    case AutoConstantType.FogColor:
                        WriteRawConstant(entry.PhysicalIndex, source.FogColor);
                        break;
                         */

                    case AutoConstantType.FogParams:
                        WriteRawConstant( entry.PhysicalIndex, source.FogParams, entry.ElementCount );
                        break;

                        /*
                    case AutoConstantType.SurfaceAmbientColor:
                        WriteRawConstant( entry.PhysicalIndex, source.SurfaceAmbientColor, entry.ElementCount );
                        break;
                    case AutoConstantType.SurfaceDiffuseColor:
                        WriteRawConstant( entry.PhysicalIndex, source.SurfaceDiffuseColor, entry.ElementCount );
                        break;
                    case AutoConstantType.SurfaceSpecularColor:
                        WriteRawConstant( entry.PhysicalIndex, source.SurfaceSpecularColor, entry.ElementCount );
                        break;
                    case AutoConstantType.SurfaceEmissiveColor:
                        WriteRawConstant( entry.PhysicalIndex, source.SurfaceEmissiveColor, entry.ElementCount );
                        break;
                    case AutoConstantType.SurfaceShininess:
                        WriteRawConstant( entry.PhysicalIndex, source.SurfaceShininess );
                        break;
                         */

                    case AutoConstantType.CameraPosition:
                        WriteRawConstant( entry.PhysicalIndex, source.CameraPosition, entry.ElementCount );
                        break;

                    case AutoConstantType.Time:
                        WriteRawConstant( entry.PhysicalIndex, source.Time*entry.FData );
                        break;
                    case AutoConstantType.Time_0_X:
                        WriteRawConstant( entry.PhysicalIndex, source.Time%entry.FData );
                        break;
                        /*
                    case AutoConstantType.CosTime_0_X:
                        WriteRawConstant(entry.PhysicalIndex, source->getCosTime_0_X(i->fData));
                        break;
                         */
                    case AutoConstantType.SinTime_0_X:
                        WriteRawConstant( entry.PhysicalIndex, Utility.Sin( source.Time%entry.FData ) );
                        break;
                        /*
                    case AutoConstantType.TanTime_0_X:
                        WriteRawConstant(entry.PhysicalIndex, source->getTanTime_0_X(i->fData));
                        break;
                    case AutoConstantType.Time_0_X_Packed:
                        WriteRawConstant(entry.PhysicalIndex, source->getTime_0_X_packed(i->fData), entry.ElementCount);
                        break;
                         */

                    case AutoConstantType.Time_0_1:
                        WriteRawConstant( entry.PhysicalIndex, ( source.Time%1 ) );
                        break;
                        /*
                    case AutoConstantType.CosTime_0_1:
                        WriteRawConstant(entry.PhysicalIndex, source->getCosTime_0_1(i->fData));
                        break;
                    case AutoConstantType.SinTime_0_1:
                        WriteRawConstant(entry.PhysicalIndex, source->getSinTime_0_1(i->fData));
                        break;
                    case AutoConstantType.TanTime_0_1:
                        WriteRawConstant(entry.PhysicalIndex, source->getTanTime_0_1(i->fData));
                        break;
                    case AutoConstantType.Time_0_1_Packed:
                        WriteRawConstant(entry.PhysicalIndex, source->getTime_0_1_packed(i->fData), entry.ElementCount);
                        */

                        /*
                    case AutoConstantType.Time_0_2PI:
                        WriteRawConstant(entry.PhysicalIndex, source->getTime_0_2Pi(i->fData));
                        break;
                    case AutoConstantType.CosTime_0_2PI:
                        WriteRawConstant(entry.PhysicalIndex, source->getCosTime_0_2Pi(i->fData));
                        break;
                    case AutoConstantType.SinTime_0_2PI:
                        WriteRawConstant(entry.PhysicalIndex, source->getSinTime_0_2Pi(i->fData));
                        break;
                    case AutoConstantType.TanTime_0_2PI:
                        WriteRawConstant(entry.PhysicalIndex, source->getTanTime_0_2Pi(i->fData));
                        break;
                    case AutoConstantType.Time_0_2PI_Packed:
                        WriteRawConstant(entry.PhysicalIndex, source->getTime_0_2Pi_packed(i->fData), entry.ElementCount);
                        break;
                         */

                        /*
                    case AutoConstantType.FrameTime:
                        WriteRawConstant(entry.PhysicalIndex, source.FrameTime * entry.FData);
                        break;
                    case AutoConstantType.FPS:
                        WriteRawConstant(entry.PhysicalIndex, source.FPS);
                        break;
                    case AutoConstantType.ViewportWidth:
                       WriteRawConstant(entry.PhysicalIndex, source.ViewportWidth);
                        break;
                    case AutoConstantType.ViewportHeight:
                        WriteRawConstant(entry.PhysicalIndex, source.ViewportHeight);
                        break;
                    case AutoConstantType.InverseViewportWidth:
                        WriteRawConstant(entry.PhysicalIndex, source.InverseViewportWidth);
                        break;
                    case AutoConstantType.InverseViewportHeight:
                        WriteRawConstant(entry.PhysicalIndex, source.InverseViewportHeight);
                        break;

                    case AutoConstantType.ViewportSize:
                        WriteRawConstants( entry.PhysicalIndex, new float[]
                                                                {
                                                                    source.ViewportWidth,
                                                                    source.ViewportHeight,
                                                                    source.InverseViewportWidth,
                                                                    source.InverseViewportHeight
                                                                },
                                           entry.ElementCount );
                        break;
                    case AutoConstantType.TexelOffsets:
					{
						var rsys = Root.Instance.RenderSystem;
					    WriteRawConstants( entry.PhysicalIndex, new float[]
					                                            {
					                                                rsys.HorizontalTexelOffset,
					                                                rsys.VerticalTexelOffset,
					                                                rsys.HorizontalTexelOffset * source.InverseViewportWidth,
					                                                rsys.VerticalTexelOffset * source.InverseViewportHeight
					                                            },
					                       entry.ElementCount );
					}
					break;

                    case AutoConstantType.TextureSize:
                        WriteRawConstant(entry.PhysicalIndex, source.GetTextureSize(entry.Data), entry.ElementCount);
                        break;
                    case AutoConstantType.InverseTextureSize:
                        WriteRawConstant(entry.PhysicalIndex, source.GetInverseTextureSize(entry.Data), entry.ElementCount);
                        break;
                    case AutoConstantType.PackedTextureSize:
                        WriteRawConstant(entry.PhysicalIndex, source.GetPackedTextureSize(entry.Data), entry.ElementCount);
                        break;
                    case AutoConstantType.SceneDepthRange:
                        WriteRawConstant(entry.PhysicalIndex, source.SceneDepthRange, entry.ElementCount);
                        break;
                        */
                    case AutoConstantType.ViewDirection:
                        WriteRawConstant( entry.PhysicalIndex, source.ViewDirection );
                        break;
                    case AutoConstantType.ViewSideVector:
                        WriteRawConstant( entry.PhysicalIndex, source.ViewSideVector );
                        break;
                    case AutoConstantType.ViewUpVector:
                        WriteRawConstant( entry.PhysicalIndex, source.ViewUpVector );
                        break;
                        /*
                    case AutoConstantType.FOV:
                        WriteRawConstant(entry.PhysicalIndex, source.FOV);
                        break;
                         */
                    case AutoConstantType.NearClipDistance:
                        WriteRawConstant( entry.PhysicalIndex, source.NearClipDistance );
                        break;
                    case AutoConstantType.FarClipDistance:
                        WriteRawConstant( entry.PhysicalIndex, source.FarClipDistance );
                        break;
                    case AutoConstantType.PassNumber:
                        WriteRawConstant( entry.PhysicalIndex, (float)source.PassNumber );
                        break;
                    case AutoConstantType.PassIterationNumber:
                        // this is actually just an initial set-up, it's bound separately, so still global
                        WriteRawConstant( entry.PhysicalIndex, 0.0f );
                        _activePassIterationIndex = entry.PhysicalIndex;
                        break;
                        /*
                    case AutoConstantType.TextureMatrix:
                        WriteRawConstant(entry.PhysicalIndex, source.GetTextureTransformMatrix(entry.Data), entry.ElementCount);
                        break;
                    case AutoConstantType.LODCameraPosition:
                        WriteRawConstant(entry.PhysicalIndex, source.LodCameraPosition, entry.ElementCount);
                        break;


                    case AutoConstantType.TextureWorldViewProjMatrix:
                        // can also be updated in lights
                        WriteRawConstant(entry.PhysicalIndex, source.GetTextureWorldViewProjMatrix(entry.Data), entry.ElementCount);
                        break;
                    case AutoConstantType.TextureWorldViewProjMatrixArray:
                        for (var l = 0; l < entry.Data; ++l)
                        {
                            // can also be updated in lights
                            WriteRawConstant(entry.PhysicalIndex + l * entry.ElementCount,
                                source.GetTextureWorldViewProjMatrix(l), entry.ElementCount);
                        }
                        break;
                    case AutoConstantType.SpotLightViewProjMatrix:
                        WriteRawConstant(entry.PhysicalIndex, source.GetSpotlightWorldViewProjMatrix(entry.Data), entry.ElementCount);
                        break;
                        */

                    case AutoConstantType.LightPositionObjectSpace:
					vec4 = source.GetLightAs4DVector(entry.Data);
					vec3 = new Vector3(vec4.x, vec4.y, vec4.z);
					if( vec4.w > 0.0f )
					{
						// point light
						vec3 = source.InverseWorldMatrix.TransformAffine(vec3);
					}
					else
					{
						// directional light
						// We need the inverse of the inverse transpose 
						source.InverseTransposeWorldMatrix.Inverse().Extract3x3Matrix(out m3);
						vec3 = (m3 * vec3);
					    vec3.Normalize();
					}
					WriteRawConstant(entry.PhysicalIndex, 
						new Vector4(vec3.x, vec3.y, vec3.z, vec4.w),
						entry.ElementCount);
					break;
                        /*
				case AutoConstantType.LightDirectionObjectSpace:
					// We need the inverse of the inverse transpose 
					source.InverseTransposeWorldMatrix.Inverse().Extract3x3Matrix(out m3);
					vec3 = m3 * source.GetLightDirection(entry.Data);
					vec3.Normalize();
					// Set as 4D vector for compatibility
					WriteRawConstant(entry.PhysicalIndex, new Vector4(vec3.x, vec3.y, vec3.z, 0.0f), entry.ElementCount);
					break;
				case AutoConstantType.LightDistanceObjectSpace:
					vec3 = source.InverseWorldMatrix.TransformAffine(source.GetLightPosition(entry.Data));
					WriteRawConstant(entry.PhysicalIndex, vec3.Length);
					break;
                         */
				case AutoConstantType.LightPositionObjectSpaceArray:
					// We need the inverse of the inverse transpose 
					source.InverseTransposeWorldMatrix.Inverse().Extract3x3Matrix(out m3);
					for (var l = 0; l < entry.Data; ++l)
					{
						vec4 = source.GetLightAs4DVector(l);
						vec3 = new Vector3(vec4.x, vec4.y, vec4.z);
						if( vec4.w > 0.0f )
						{
							// point light
							vec3 = source.InverseWorldMatrix.TransformAffine(vec3);
						}
						else
						{
							// directional light
							vec3 = (m3 * vec3);
                            vec3.Normalize();
						}
						WriteRawConstant(entry.PhysicalIndex + l*entry.ElementCount, 
							new Vector4(vec3.x, vec3.y, vec3.z, vec4.w),
							entry.ElementCount);
					}
					break;
                        /*
				case AutoConstantType.LightDirectionObjectSpaceArray:
					// We need the inverse of the inverse transpose 
					source.InverseTransposeWorldMatrix.Inverse().Extract3x3Matrix(out m3);
					for (var l = 0; l < entry.Data; ++l)
					{
						vec3 = m3 * source.GetLightDirection(l);
						vec3.Normalize();
						WriteRawConstant(entry.PhysicalIndex + l*entry.ElementCount, 
							new Vector4(vec3.x, vec3.y, vec3.z, 0.0f), entry.ElementCount); 
					}
					break;
				case AutoConstantType.LightDistanceObjectSpaceArray:
					for (var l = 0; l < entry.Data; ++l)
					{
						vec3 = source.InverseWorldMatrix.TransformAffine(source.GetLightPosition(l));
						WriteRawConstant(entry.PhysicalIndex + l*entry.ElementCount, vec3.Length);
					}
					break;*/

                    case AutoConstantType.WorldMatrix:
                        WriteRawConstant( entry.PhysicalIndex, source.WorldMatrix, entry.ElementCount );
                        break;
                    case AutoConstantType.InverseWorldMatrix:
                        WriteRawConstant( entry.PhysicalIndex, source.InverseWorldMatrix, entry.ElementCount );
                        break;
                        /*
                    case AutoConstantType.TransposeWorldMatrix:
                        WriteRawConstant( entry.PhysicalIndex, source.TransposeWorldMatrix, entry.ElementCount );
                        break;
                    case AutoConstantType.InverseTransposeWorldMatrix:
                        WriteRawConstant( entry.PhysicalIndex, source.InverseTransposeWorldMatrix, entry.ElementCount );
                        break;

                    case AutoConstantType.WorldMatrixArray3x4:
                        // Loop over matrices
                        pMatrix = source->getWorldMatrixArray();
                        numMatrices = source->getWorldMatrixCount();
                        index = entry.PhysicalIndex;
                        for ( m = 0; m < numMatrices; ++m )
                        {
                            WriteRawConstants( index, ( *pMatrix )[ 0 ], 12 );
                            index += 12;
                            ++pMatrix;
                        }
                        break;
                         */

                    case AutoConstantType.WorldMatrixArray:
                        WriteRawConstant( entry.PhysicalIndex, source.WorldMatrixArray, source.WorldMatrixCount );
                        break;
                    case AutoConstantType.WorldViewMatrix:
                        WriteRawConstant( entry.PhysicalIndex, source.WorldViewMatrix, entry.ElementCount );
                        break;
                    case AutoConstantType.InverseWorldViewMatrix:
                        WriteRawConstant( entry.PhysicalIndex, source.InverseWorldViewMatrix, entry.ElementCount );
                        break;
                        /*
                    case AutoConstantType.TransposeWorldViewMatrix:
                        WriteRawConstant( entry.PhysicalIndex, source.TransposeWorldViewMatrix, entry.ElementCount );
                        break;
                         */
                    case AutoConstantType.InverseTransposeWorldViewMatrix:
                        WriteRawConstant( entry.PhysicalIndex, source.InverseTransposeWorldViewMatrix, entry.ElementCount );
                        break;

                    case AutoConstantType.WorldViewProjMatrix:
                        WriteRawConstant( entry.PhysicalIndex, source.WorldViewProjMatrix, entry.ElementCount );
                        break;
                        /*
                    case AutoConstantType.InverseWorldViewProjMatrix:
                        WriteRawConstant( entry.PhysicalIndex, source.InverseWorldViewProjMatrix, entry.ElementCount );
                        break;
                    case AutoConstantType.TransposeWorldViewProjMatrix:
                        WriteRawConstant( entry.PhysicalIndex, source.TransposeWorldViewProjMatrix, entry.ElementCount );
                        break;
                    case AutoConstantType.InverseTransposeWorldViewProjMatrix:
                        WriteRawConstant( entry.PhysicalIndex, source.InverseTransposeWorldViewProjMatrix, entry.ElementCount );
                        break;
                         */
                    case AutoConstantType.CameraPositionObjectSpace:
                        WriteRawConstant( entry.PhysicalIndex, source.CameraPositionObjectSpace, entry.ElementCount );
                        break;
                        /*
                    case AutoConstantType.LODCameraPositionObjectSpace:
                        WriteRawConstant( entry.PhysicalIndex, source.LodCameraPositionObjectSpace, entry.ElementCount );
                        break;
                        */
                    case AutoConstantType.Custom:
                    case AutoConstantType.AnimationParametric:
                        source.CurrentRenderable.UpdateCustomGpuParameter( entry, this );
                        break;
                        /*
                    case AutoConstantType.LightCustom:
                        source.UpdateLightCustomGpuParameter( entry, this );
                        break;
                    case AutoConstantType.LightCount:
                        WriteRawConstant( entry.PhysicalIndex, source.LightCount );
                        break;
                         */
                    case AutoConstantType.LightDiffuseColor:
                        WriteRawConstant( entry.PhysicalIndex, source.GetLightDiffuseColor( entry.Data ),
                                          entry.ElementCount );
                        break;
                    case AutoConstantType.LightSpecularColor:
                        WriteRawConstant( entry.PhysicalIndex, source.GetLightSpecularColor( entry.Data ),
                                          entry.ElementCount );
                        break;
                    case AutoConstantType.LightPosition:
                        // Get as 4D vector, works for directional lights too
                        // Use element count in case uniform slot is smaller
                        WriteRawConstant( entry.PhysicalIndex,
                                          source.GetLightAs4DVector( entry.Data ), entry.ElementCount );
                        break;
                        /*
                    case AutoConstantType.LightDirection:
                        vec3 = source.GetLightDirection( entry.Data );
                        // Set as 4D vector for compatibility
                        // Use element count in case uniform slot is smaller
                        WriteRawConstant( entry.PhysicalIndex, new Vector4( vec3.x, vec3.y, vec3.z, 1.0f ),
                                          entry.ElementCount );
                        break;*/
                    case AutoConstantType.LightPositionViewSpace:
                        WriteRawConstant( entry.PhysicalIndex,
                                          source.ViewMatrix.TransformAffine(
                                              source.GetLightAs4DVector( entry.Data ) ), entry.ElementCount );
                        break;
                        /*
                    case AutoConstantType.LightDirectionViewSpace:
                        source.InverseTransposeViewMatrix.Extract3x3Matrix( m3 );
                        // inverse transpose in case of scaling
                        vec3 = m3*source->getLightDirection( entry.Data );
                        vec3.normalise();
                        // Set as 4D vector for compatibility
                        WriteRawConstant( entry.PhysicalIndex, Vector4( vec3.x, vec3.y, vec3.z, 0.0f ),
                                          entry.ElementCount );
                        break;
                    case AutoConstantType.ShadowExtrusionDistance:
                        // extrusion is in object-space, so we have to rescale by the inverse
                        // of the world scaling to deal with scaled objects
                        source.WorldMatrix.Extract3x3Matrix( m3 );
                        WriteRawConstant( entry.PhysicalIndex, source.ShadowExtrusionDistance/
                                                               Math.Sqrt(
                                                                   Utility.Max(
                                                                       Utility.Max(m3.GetColumn(0).squaredLength(),
                                                                                 m3.GetColumn( 1 ).squaredLength() ),
                                                                       m3.GetColumn( 2 ).squaredLength() ) ) );
                        break;
                    case AutoConstantType.ShadowSceneDepthRange:
                        WriteRawConstant( entry.PhysicalIndex, source.GetShadowSceneDepthRange( entry.Data ) );
                        break;
                    case AutoConstantType.ShadowColor:
                        WriteRawConstant( entry.PhysicalIndex, source.ShadowColor(), entry.ElementCount );
                        break;
                        */
                    case AutoConstantType.LightPowerScale:
                        WriteRawConstant( entry.PhysicalIndex, source.GetLightPowerScale( entry.Data ) );
                        break;
                        /*
                    case AutoConstantType.LightDiffuseColorPowerScaled:
                        WriteRawConstant( entry.PhysicalIndex, source.GetLightDiffuseColorWithPower( entry.Data ),
                                          entry.ElementCount );
                        break;
                    case AutoConstantType.LightSpecularColorPowerScaled:
                        WriteRawConstant( entry.PhysicalIndex, source.GetLightSpecularColorWithPower( entry.Data ),
                                          entry.ElementCount );
                        break;
                    case AutoConstantType.LightNumber:
                        WriteRawConstant( entry.PhysicalIndex, source.GetLightNumber( entry.Data ) );
                        break;
                    case AutoConstantType.LightCastsShadows:
                        WriteRawConstant( entry.PhysicalIndex, source.GetLightCastsShadows( entry.Data ) );
                        break;
                    case AutoConstantType.LightAttenuation:
                        WriteRawConstant( entry.PhysicalIndex, source.GetLightAttenuation( entry.Data ),
                                          entry.ElementCount );
                        break;
                    case AutoConstantType.SpotLightParams:
                        WriteRawConstant( entry.PhysicalIndex, source.GetSpotlightParams( entry.Data ),
                                          entry.ElementCount );
                        break;
                    case AutoConstantType.LightDiffuseColorArray:
                        for ( var l = 0; l < entry.Data; ++l )
                            WriteRawConstant( entry.PhysicalIndex + l*entry.ElementCount,
                                              source.GetLightDiffuseColor( l ), entry.ElementCount );
                        break;

                    case AutoConstantType.LightSpecularColorArray:
                        for ( var l = 0; l < entry.Data; ++l )
                            WriteRawConstant( entry.PhysicalIndex + l*entry.ElementCount,
                                              source.GetLightSpecularColor( l ), entry.ElementCount );
                        break;
                    case AutoConstantType.LightDiffuseColorPowerScaledArray:
                        for ( var l = 0; l < entry.Data; ++l )
                            WriteRawConstant( entry.PhysicalIndex + l*entry.ElementCount,
                                              source.GetLightDiffuseColorWithPower( l ), entry.ElementCount );
                        break;

                    case AutoConstantType.LightSpecularColorPowerScaledArray:
                        for ( var l = 0; l < entry.Data; ++l )
                            WriteRawConstant( entry.PhysicalIndex + l*entry.ElementCount,
                                              source.GetLightSpecularColorWithPower( l ), entry.ElementCount );
                        break;

                    case AutoConstantType.LightPositionArray:
                        // Get as 4D vector, works for directional lights too
                        for ( var l = 0; l < entry.Data; ++l )
                            WriteRawConstant( entry.PhysicalIndex + l*entry.ElementCount,
                                              source.GetLightAs4DVector( l ), entry.ElementCount );
                        break;

                    case AutoConstantType.LightDirectionArray:
                        for ( var l = 0; l < entry.Data; ++l )
                        {
                            vec3 = source.GetLightDirection( l );
                            // Set as 4D vector for compatibility
                            WriteRawConstant( entry.PhysicalIndex + l*entry.ElementCount,
                                              Vector4( vec3.x, vec3.y, vec3.z, 0.0f ), entry.ElementCount );
                        }
                        break;

                    case AutoConstantType.LightPositionViewSpaceArray:
                        for ( var l = 0; l < entry.Data; ++l )
                            WriteRawConstant( entry.PhysicalIndex + l*entry.ElementCount,
                                              source.ViewMatrix.TransformAffine(
                                                  source.GetLightAs4DVector( l ) ),
                                              entry.ElementCount );
                        break;

                    case AutoConstantType.LightDirectionViewSpaceArray:
                        source.InverseTransposeViewMatrix().Extract3x3Matrix( m3 );
                        for ( var l = 0; l < entry.Data; ++l )
                        {
                            vec3 = m3*source.GetLightDirection( l );
                            vec3.Normalize();
                            // Set as 4D vector for compatibility
                            WriteRawConstant( entry.PhysicalIndex + l*entry.ElementCount,
                                              Vector4( vec3.x, vec3.y, vec3.z, 0.0f ), entry.ElementCount );
                        }
                        break;*/

                    case AutoConstantType.LightPowerScaleArray:
                        for ( var l = 0; l < entry.Data; ++l )
                            WriteRawConstant( entry.PhysicalIndex + l*entry.ElementCount,
                                              source.GetLightPowerScale( l ) );
                        break;
                        /*
                    case AutoConstantType.LightAttenuationArray:
                        for ( var l = 0; l < entry.Data; ++l )
                        {
                            WriteRawConstant( entry.PhysicalIndex + l*entry.ElementCount,
                                              source.GetLightAttenuation( l ), entry.ElementCount );
                        }
                        break;
                    case AutoConstantType.SpotLightParamsArray:
                        for ( var l = 0; l < entry.Data; ++l )
                        {
                            WriteRawConstant( entry.PhysicalIndex + l*entry.ElementCount,
                                              source.GetSpotlightParams( l ),
                                              entry.ElementCount );
                        }
                        break;
                    case AutoConstantType.DerivedLightDiffuseColor:
                        WriteRawConstant( entry.PhysicalIndex,
                                          source.GetLightDiffuseColorWithPower( entry.Data )*
                                          sourceSurfaceDiffuseColor,
                                          entry.ElementCount );
                        break;
                    case AutoConstantType.DerivedLightSpecularColor:
                        WriteRawConstant( entry.PhysicalIndex,
                                          source.GetLightSpecularColorWithPower( entry.Data )*
                                          source.SurfaceSpecularColor,
                                          entry.ElementCount );
                        break;
                    case AutoConstantType.DerivedLightDiffuseColorArray:
                        for ( var l = 0; l < entry.Data; ++l )
                        {
                            WriteRawConstant( entry.PhysicalIndex + l*entry.ElementCount,
                                              source.GetLightDiffuseColorWithPower( l )*
                                              source.SurfaceDiffuseColor,
                                              entry.ElementCount );
                        }
                        break;
                    case AutoConstantType.DerivedLightSpecularColorArray:
                        for ( var l = 0; l < entry.Data; ++l )
                        {
                            WriteRawConstant( entry.PhysicalIndex + l*entry.ElementCount,
                                              source.GetLightSpecularColorWithPower( l )*
                                              source.SurfaceSpecularColor,
                                              entry.ElementCount );
                        }
                        break;
                    case AutoConstantType.TextureViewProjMatrix:
                        // can also be updated in lights
                        WriteRawConstant( entry.PhysicalIndex, source.GetTextureViewProjMatrix( entry.Data ),
                                          entry.ElementCount );
                        break;
                    case AutoConstantType.TextureViewProjMatrixArray:
                        for ( var l = 0; l < entry.Data; ++l )
                        {
                            // can also be updated in lights
                            WriteRawConstant( entry.PhysicalIndex + l*entry.ElementCount,
                                              source.GetTextureViewProjMatrix( l ), entry.ElementCount );
                        }
                        break;
                    case AutoConstantType.SpotLightViewProjMatrix:
                        WriteRawConstant( entry.PhysicalIndex, source.GetSpotlightViewProjMatrix( entry.Data ),
                                          entry.ElementCount );
                        break;
                    case AutoConstantType.SpotLightViewProjMatrixArray:
                        for ( var l = 0; l < entry.Data; ++l )
                        {
                            // can also be updated in lights
                            WriteRawConstant( entry.PhysicalIndex + l*entry.ElementCount,
                                              source.GetSpotlightViewProjMatrix( l ), entry.ElementCount );
                        }
                        break;*/




                    default:
                        throw new NotImplementedException();
                }
            }
        }
    }
}
