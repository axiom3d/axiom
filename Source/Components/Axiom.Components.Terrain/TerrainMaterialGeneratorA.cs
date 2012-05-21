#region MIT/X11 License

//Copyright © 2003-2012 Axiom 3D Rendering Engine Project
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
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections.Generic;
using Axiom.Core;
using Axiom.Math;
using Axiom.Graphics;
using Axiom.Media;
using Axiom.Core.Collections;

#endregion Namespace Declarations

namespace Axiom.Components.Terrain
{
	/// <summary>
	/// A TerrainMaterialGenerator which can cope with normal mapped, specular mapped
	///	terrain.
	/// </summary>
	public class TerrainMaterialGeneratorA : TerrainMaterialGenerator
	{
		#region - constructor -

		/// <summary>
		/// 
		/// </summary>
		public TerrainMaterialGeneratorA()
		{
			// define the layers
			// We expect terrain textures to have no alpha, so we use the alpha channel
			// in the albedo texture to store specular reflection
			// similarly we double-up the normal and height (for parallax)
			mLayerDecl.Samplers = new List<TerrainLayerSampler>();
			mLayerDecl.Samplers.Add( new TerrainLayerSampler( "albedo_specular", PixelFormat.BYTE_RGBA ) );
			mLayerDecl.Samplers.Add( new TerrainLayerSampler( "normal_height", PixelFormat.BYTE_RGBA ) );

			mLayerDecl.Elements = new List<TerrainLayerSamplerElement>();
			mLayerDecl.Elements.Add( new TerrainLayerSamplerElement( 0, TerrainLayerSamplerSemantic.Albedo, 0, 3 ) );
			mLayerDecl.Elements.Add( new TerrainLayerSamplerElement( 0, TerrainLayerSamplerSemantic.Specular, 3, 1 ) );
			mLayerDecl.Elements.Add( new TerrainLayerSamplerElement( 1, TerrainLayerSamplerSemantic.Normal, 0, 3 ) );
			mLayerDecl.Elements.Add( new TerrainLayerSamplerElement( 1, TerrainLayerSamplerSemantic.Height, 3, 1 ) );

			mProfiles.Add( new SM2Profile( this, "SM2", "Profile for rendering on Shader Model 2 capable cards" ) );
			// TODO - check hardware capabilities & use fallbacks if required (more profiles needed)
			SetActiveProfile( "SM2" );
		}

		#endregion

		#region - SM2Profile class -

		/// <summary>
		/// Shader model 2 profile target.
		/// </summary>
		public class SM2Profile : TerrainMaterialGenerator.Profile
		{
			#region - enumeration -

			/// <summary>
			/// 
			/// </summary>
			protected enum TechniqueType
			{
				HighLod,
				LowLod,
				RenderCompositeMap,
			}

			#endregion

			#region - fields -

			/// <summary>
			/// 
			/// </summary>
			protected ShaderHelper mShaderGen;

			/// <summary>
			/// 
			/// </summary>
			protected bool mLayerNormalMappingEnabled;

			/// <summary>
			/// 
			/// </summary>
			protected bool mLayerParallaxMappingEnabled;

			/// <summary>
			/// 
			/// </summary>
			protected bool mLayerSpecularMappingEnabled;

			/// <summary>
			/// 
			/// </summary>
			protected bool mGlobalColorMapEnabled;

			/// <summary>
			/// 
			/// </summary>
			protected bool mLightMapEnabled;

			/// <summary>
			/// 
			/// </summary>
			protected bool mCompositeMapEnabled;

			#endregion

			#region - properties -

			/// <summary>
			///  Whether to support normal mapping per layer in the shader (default true).
			/// </summary>
			public bool IsLayerNormalMappingEnabled
			{
				set
				{
					if ( value != this.mLayerNormalMappingEnabled )
					{
						this.mLayerNormalMappingEnabled = value;
						mParent.MarkChanged();
					}
				}
				get
				{
					return this.mLayerNormalMappingEnabled;
				}
			}

			/// <summary>
			/// Whether to support parallax mapping per layer in the shader (default true).
			/// </summary>
			public bool IsLayerParallaxMappingEnabled
			{
				set
				{
					if ( value != this.mLayerParallaxMappingEnabled )
					{
						this.mLayerParallaxMappingEnabled = value;
						mParent.MarkChanged();
					}
				}
				get
				{
					return this.mLayerParallaxMappingEnabled;
				}
			}

			/// <summary>
			/// Whether to support specular mapping per layer in the shader (default true).
			/// </summary>
			public bool IsLayerSpecularMappingEnabled
			{
				set
				{
					if ( value != this.mLayerSpecularMappingEnabled )
					{
						this.mLayerSpecularMappingEnabled = value;
						mParent.MarkChanged();
					}
				}
				get
				{
					return this.mLayerSpecularMappingEnabled;
				}
			}

			/// <summary>
			/// Whether to support a global colour map over the terrain in the shader,
			///	if it's present (default true).
			/// </summary>
			public bool IsGlobalColorMapEnabled
			{
				set
				{
					if ( value != this.mGlobalColorMapEnabled )
					{
						this.mGlobalColorMapEnabled = value;
						mParent.MarkChanged();
					}
				}
				get
				{
					return this.mGlobalColorMapEnabled;
				}
			}

			/// <summary>
			/// Whether to support a light map over the terrain in the shader,
			/// if it's present (default true).
			/// </summary>
			public bool IsLightMapEnabled
			{
				set
				{
					if ( value != this.mLightMapEnabled )
					{
						this.mLightMapEnabled = value;
						mParent.MarkChanged();
					}
				}
				get
				{
					return this.mLightMapEnabled;
				}
			}

			/// <summary>
			/// Whether to use the composite map to provide a lower LOD technique
			///	in the distance (default true).
			/// </summary>
			public bool IsCompositeMapEnabled
			{
				set
				{
					if ( value != this.mCompositeMapEnabled )
					{
						this.mCompositeMapEnabled = value;
						mParent.MarkChanged();
					}
				}
				get
				{
					return this.mCompositeMapEnabled;
				}
			}

			#endregion

			#region - constructor -

			/// <summary>
			/// 
			/// </summary>
			/// <param name="parent"></param>
			/// <param name="name"></param>
			/// <param name="description"></param>
			public SM2Profile( TerrainMaterialGenerator parent, string name, string description )
				: base( parent, name, description )
			{
				this.mLayerNormalMappingEnabled = true;
				this.mLayerParallaxMappingEnabled = true;
				this.mLayerSpecularMappingEnabled = true;
				this.mGlobalColorMapEnabled = true;
				this.mLightMapEnabled = false;
				this.mCompositeMapEnabled = true;
			}

			#endregion

			#region - functions -

			/// <summary>
			/// 
			/// </summary>
			/// <param name="mat"></param>
			/// <param name="terrain"></param>
			/// <param name="tt"></param>
			protected void AddTechnique( Material mat, Terrain terrain, TechniqueType tt )
			{
				string ttStr = string.Empty;
				switch ( tt )
				{
					case TechniqueType.HighLod:
						ttStr += "hl";
						break;
					case TechniqueType.LowLod:
						ttStr += "ll";
						break;
					case TechniqueType.RenderCompositeMap:
						ttStr += "rc";
						break;
				}
				LogManager.Instance.Write( "AddTechique:" + ttStr, null );

				Technique tech = mat.CreateTechnique();

				//only supporting one pass
				Pass pass = tech.CreatePass();

				GpuProgramManager gmgr = GpuProgramManager.Instance;
				HighLevelGpuProgramManager hmgr = HighLevelGpuProgramManager.Instance;

				if ( this.mShaderGen == null )
				{
					bool check2x = this.mLayerNormalMappingEnabled || this.mLayerParallaxMappingEnabled;

					/* if (hmgr.IsLanguageSupported("cg") &&
                         (check2x && (gmgr.IsSyntaxSupported("fp40") || gmgr.IsSyntaxSupported("ps_2_x"))) ||
                         (gmgr.IsSyntaxSupported("ps_2_0")))
                         mShaderGen = new ShaderHelperCG();
                     else*/
					if ( hmgr.IsLanguageSupported( "hlsl" ) )
					{
						this.mShaderGen = new ShaderHelperHLSL();
					}
					else if ( hmgr.IsLanguageSupported( "glsl" ) )
					{
						this.mShaderGen = new ShaderHelperGLSL();
					}
					else
					{
						//TODO
					}
				}
				HighLevelGpuProgram vprog = this.mShaderGen.GenerateVertexProgram( this, terrain, tt );
				HighLevelGpuProgram fprog = this.mShaderGen.GenerateFragmentProgram( this, terrain, tt );

				pass.SetVertexProgram( vprog.Name );
				pass.SetFragmentProgram( fprog.Name );

				if ( tt == TechniqueType.HighLod || tt == TechniqueType.RenderCompositeMap )
				{
					//global normal map
					TextureUnitState tu = pass.CreateTextureUnitState();
					tu.SetTextureName( terrain.TerrainNormalMap.Name );
					tu.SetTextureAddressingMode( TextureAddressing.Clamp );

					//global color map
					if ( terrain.IsGlobalColorMapEnabled && IsGlobalColorMapEnabled )
					{
						tu = pass.CreateTextureUnitState( terrain.GlobalColorMap.Name );
						tu.SetTextureAddressingMode( TextureAddressing.Clamp );
					}

					//light map
					if ( IsLightMapEnabled )
					{
						tu = pass.CreateTextureUnitState( terrain.LightMap.Name );
						tu.SetTextureAddressingMode( TextureAddressing.Clamp );
					}

					//blend maps
					uint maxLayers = GetMaxLayers( terrain );

					uint numBlendTextures = Utility.Min( terrain.GetBlendTextureCount( (byte)maxLayers ),
					                                     terrain.GetBlendTextureCount() );
					uint numLayers = Utility.Min( maxLayers, (uint)terrain.LayerCount );
					for ( uint i = 0; i < numBlendTextures; ++i )
					{
						tu = pass.CreateTextureUnitState( terrain.GetBlendTextureName( (byte)i ) );
						tu.SetTextureAddressingMode( TextureAddressing.Clamp );
					}

					//layer textures
					for ( uint i = 0; i < numLayers; ++i )
					{
						//diffuse / specular
						string name = terrain.GetLayerTextureName( (byte)i, 0 );
						tu = pass.CreateTextureUnitState( terrain.GetLayerTextureName( (byte)i, 0 ) );
						//normal / height
						tu = pass.CreateTextureUnitState( terrain.GetLayerTextureName( (byte)i, 1 ) );
					}
				} //end if
				else if ( this.mCompositeMapEnabled )
				{
					// LOW_LOD textures
					// composite map
					TextureUnitState tu = pass.CreateTextureUnitState();
					tu.SetTextureName( terrain.CompositeMap.Name );
					tu.SetTextureAddressingMode( TextureAddressing.Clamp );


					// That's it!
				}
			}

			/// <summary>
			/// 
			/// </summary>
			/// <param name="terrain"></param>
			/// <returns></returns>
			public override Material Generate( Terrain terrain )
			{
				// re-use old material if exists
				Material mat = terrain._Material;
				if ( mat == null )
				{
					MaterialManager matMgr = MaterialManager.Instance;
					// it's important that the names are deterministic for a given terrain, so
					// use the terrain pointer as an ID+
					string matName = terrain.MaterialName;
					mat = (Material)matMgr.GetByName( matName );
					if ( mat == null )
					{
						mat = (Material)matMgr.Create( matName, ResourceGroupManager.DefaultResourceGroupName );
					}
				}

				// clear everything
				mat.RemoveAllTechniques();
				AddTechnique( mat, terrain, TechniqueType.HighLod );

				//LOD
				if ( this.mCompositeMapEnabled )
				{
					AddTechnique( mat, terrain, TechniqueType.LowLod );
					var lodValues = new LodValueList();
					lodValues.Add( 3000 ); //TerrainGlobalOptions.CompositeMapDistance);
					mat.SetLodLevels( lodValues );
					Technique lowLodTechnique = mat.GetTechnique( 1 );
					lowLodTechnique.LodIndex = 1;
				}
				UpdateParams( mat, terrain );
				//mat.Compile(true);
				return mat;
			}

			/// <summary>
			/// 
			/// </summary>
			/// <param name="terrain"></param>
			/// <returns></returns>
			public override Material GenerateForCompositeMap( Terrain terrain )
			{
				// re-use old material if exists
				Material mat = terrain._CompositeMapMaterial;
				if ( mat == null )
				{
					MaterialManager matMgr = MaterialManager.Instance;

					// it's important that the names are deterministic for a given terrain, so
					// use the terrain pointer as an ID
					string matName = terrain.MaterialName + "/comp";
					mat = (Material)matMgr.GetByName( matName );
					if ( mat == null )
					{
						mat = (Material)matMgr.Create( matName, ResourceGroupManager.DefaultResourceGroupName );
					}
				}
				// clear everything
				mat.RemoveAllTechniques();

				AddTechnique( mat, terrain, TechniqueType.RenderCompositeMap );
				mat.Compile( true );
				UpdateParamsForCompositeMap( mat, terrain );

				return mat;
			}

			public override byte GetMaxLayers( Terrain terrain )
			{
				// count the texture units free
				byte freeTextureUnits = 16;
				// lightmap
				--freeTextureUnits;
				// normalmap
				--freeTextureUnits;
				// colourmap
				if ( terrain.IsGlobalColorMapEnabled )
				{
					--freeTextureUnits;
				}
				// TODO shadowmaps

				// each layer needs 2.25 units (1xdiffusespec, 1xnormalheight, 0.25xblend)
				return (byte)( freeTextureUnits/2.25f );
			}

			/// <summary>
			/// 
			/// </summary>
			/// <param name="mat"></param>
			/// <param name="terrain"></param>
			public override void UpdateParams( Material mat, Terrain terrain )
			{
				this.mShaderGen.UpdateParams( this, mat, terrain, false );
			}

			/// <summary>
			/// 
			/// </summary>
			/// <param name="mat"></param>
			/// <param name="terrain"></param>
			public override void UpdateParamsForCompositeMap( Material mat, Terrain terrain )
			{
				this.mShaderGen.UpdateParams( this, mat, terrain, true );
			}

			/// <summary>
			/// 
			/// </summary>
			/// <param name="terrain"></param>
			public override void RequestOption( Terrain terrain )
			{
				terrain.IsMorphRequired = true;
				terrain.NormalMapRequired = true;
				terrain.SetLightMapRequired( this.mLightMapEnabled, true );
				terrain.CompositeMapRequired = this.mCompositeMapEnabled;
			}

			#endregion

			#region - shaderhelper -

			/// <summary>
			/// Interface definition for helper class to generate shaders
			/// </summary>
			protected abstract class ShaderHelper
			{
				/// <summary>
				/// 
				/// </summary>
				/// <param name="prof"></param>
				/// <param name="terrain"></param>
				/// <param name="tt"></param>
				/// <returns></returns>
				public virtual HighLevelGpuProgram GenerateVertexProgram( SM2Profile prof, Terrain terrain, TechniqueType tt )
				{
					HighLevelGpuProgram ret = CreateVertexProgram( prof, terrain, tt );

					string sourceStr = string.Empty;
					GenerateVertexProgramSource( prof, terrain, tt, ref sourceStr );
					ret.Source = sourceStr;
					DefaultVpParams( prof, terrain, tt, ret );
#if AXIOM_DEBUG_MODE
                    LogManager.Instance.Write(LogMessageLevel.Trivial, false, "*** Terrain Vertex Program: "
                        + ret.Name + " ***\n" + ret.Source + "\n***  ***");
#endif
					return ret;
				}

				/// <summary>
				/// 
				/// </summary>
				/// <param name="prof"></param>
				/// <param name="terrain"></param>
				/// <param name="tt"></param>
				/// <returns></returns>
				public virtual HighLevelGpuProgram GenerateFragmentProgram( SM2Profile prof, Terrain terrain, TechniqueType tt )
				{
					HighLevelGpuProgram ret = CreateFragmentProgram( prof, terrain, tt );

					string sourceStr = string.Empty;
					GenerateFragmetProgramSource( prof, terrain, tt, ref sourceStr );
					ret.Source = sourceStr;
					DefaultFpParams( prof, terrain, tt, ret );
#if AXIOM_DEBUG_MODE
                    LogManager.Instance.Write(LogMessageLevel.Trivial, false, "*** Terrain Fragment Program: "
                        + ret.Name + " ***\n" + ret.Source + "\n***  ***");
#endif
					return ret;
				}

				/// <summary>
				/// 
				/// </summary>
				/// <param name="prof"></param>
				/// <param name="mat"></param>
				/// <param name="terrain"></param>
				/// <param name="compositeMap"></param>
				public virtual void UpdateParams( SM2Profile prof, Material mat, Terrain terrain, bool compositeMap )
				{
					Pass p = mat.GetTechnique( 0 ).GetPass( 0 );
					if ( compositeMap )
					{
						UpdateVpParams( prof, terrain, TechniqueType.RenderCompositeMap, p.VertexProgramParameters );
						UpdateFpParams( prof, terrain, TechniqueType.RenderCompositeMap, p.FragmentProgramParameters );
					}
					else
					{
						//high lod
						UpdateVpParams( prof, terrain, TechniqueType.HighLod, p.VertexProgramParameters );
						UpdateFpParams( prof, terrain, TechniqueType.HighLod, p.FragmentProgramParameters );

						//low lod
						p = mat.GetTechnique( 1 ).GetPass( 0 );
						UpdateVpParams( prof, terrain, TechniqueType.LowLod, p.VertexProgramParameters );
						UpdateFpParams( prof, terrain, TechniqueType.LowLod, p.FragmentProgramParameters );
					}
				}

				/// <summary>
				/// 
				/// </summary>
				/// <param name="prof"></param>
				/// <param name="terrain"></param>
				/// <param name="tt"></param>
				/// <returns></returns>
				protected virtual string GetVertexProgramName( SM2Profile prof, Terrain terrain, TechniqueType tt )
				{
					string progName = terrain.MaterialName + "/sm2/vp";

					switch ( tt )
					{
						case TechniqueType.HighLod:
							progName += "/hlod";
							break;
						case TechniqueType.LowLod:
							progName += "/llod";
							break;
						case TechniqueType.RenderCompositeMap:
							progName += "/comp";
							break;
					}

					return progName;
				}

				/// <summary>
				/// 
				/// </summary>
				/// <param name="prof"></param>
				/// <param name="terrain"></param>
				/// <param name="tt"></param>
				/// <returns></returns>
				protected virtual string GetFragmentProgramName( SM2Profile prof, Terrain terrain, TechniqueType tt )
				{
					string progName = terrain.MaterialName + "/sm2/fp";

					switch ( tt )
					{
						case TechniqueType.HighLod:
							progName += "/hlod";
							break;
						case TechniqueType.LowLod:
							progName += "/llod";
							break;
						case TechniqueType.RenderCompositeMap:
							progName += "/comp";
							break;
					}

					return progName;
				}

				/// <summary>
				/// 
				/// </summary>
				/// <param name="prof"></param>
				/// <param name="terrain"></param>
				/// <param name="tt"></param>
				/// <returns></returns>
				protected abstract HighLevelGpuProgram CreateVertexProgram( SM2Profile prof, Terrain terrain, TechniqueType tt );

				/// <summary>
				/// 
				/// </summary>
				/// <param name="prof"></param>
				/// <param name="terrain"></param>
				/// <param name="tt"></param>
				/// <returns></returns>
				protected abstract HighLevelGpuProgram CreateFragmentProgram( SM2Profile prof, Terrain terrain, TechniqueType tt );

				/// <summary>
				/// 
				/// </summary>
				/// <param name="prof"></param>
				/// <param name="terrain"></param>
				/// <param name="tt"></param>
				/// <param name="source"></param>
				protected virtual void GenerateVertexProgramSource( SM2Profile prof, Terrain terrain, TechniqueType tt,
				                                                    ref string source )
				{
					GenerateVpHeader( prof, terrain, tt, ref source );

					if ( tt != TechniqueType.LowLod )
					{
						uint maxLayers = prof.GetMaxLayers( terrain );
						uint numLayers = Utility.Min( maxLayers, (uint)terrain.LayerCount );

						for ( uint i = 0; i < numLayers; ++i )
						{
							GenerateVpLayer( prof, terrain, tt, i, ref source );
						}
					}

					GenerateVpFooter( prof, terrain, tt, ref source );
				}

				/// <summary>
				/// 
				/// </summary>
				/// <param name="prof"></param>
				/// <param name="terrain"></param>
				/// <param name="tt"></param>
				/// <param name="source"></param>
				protected virtual void GenerateFragmetProgramSource( SM2Profile prof, Terrain terrain, TechniqueType tt,
				                                                     ref string source )
				{
					GenerateFpHeader( prof, terrain, tt, ref source );

					if ( tt != TechniqueType.LowLod )
					{
						uint maxLayers = prof.GetMaxLayers( terrain );
						uint numLayers = Utility.Min( maxLayers, (uint)terrain.LayerCount );

						for ( uint i = 0; i < numLayers; ++i )
						{
							GenerateFpLayer( prof, terrain, tt, i, ref source );
						}
					}

					GenerateFpFooter( prof, terrain, tt, ref source );
				}

				/// <summary>
				/// 
				/// </summary>
				/// <param name="prof"></param>
				/// <param name="terrain"></param>
				/// <param name="tt"></param>
				/// <param name="source"></param>
				protected abstract void GenerateVpHeader( SM2Profile prof, Terrain terrain, TechniqueType tt, ref string source );

				/// <summary>
				/// 
				/// </summary>
				/// <param name="prof"></param>
				/// <param name="terrain"></param>
				/// <param name="tt"></param>
				/// <param name="source"></param>
				protected abstract void GenerateFpHeader( SM2Profile prof, Terrain terrain, TechniqueType tt, ref string source );

				/// <summary>
				/// 
				/// </summary>
				/// <param name="prof"></param>
				/// <param name="terrain"></param>
				/// <param name="tt"></param>
				/// <param name="source"></param>
				protected abstract void GenerateVpLayer( SM2Profile prof, Terrain terrain, TechniqueType tt, uint layer,
				                                         ref string source );

				/// <summary>
				/// 
				/// </summary>
				/// <param name="prof"></param>
				/// <param name="terrain"></param>
				/// <param name="tt"></param>
				/// <param name="source"></param>
				protected abstract void GenerateFpLayer( SM2Profile prof, Terrain terrain, TechniqueType tt, uint layer,
				                                         ref string source );

				/// <summary>
				/// 
				/// </summary>
				/// <param name="prof"></param>
				/// <param name="terrain"></param>
				/// <param name="tt"></param>
				/// <param name="source"></param>
				protected abstract void GenerateFpFooter( SM2Profile prof, Terrain terrain, TechniqueType tt, ref string source );

				/// <summary>
				/// 
				/// </summary>
				/// <param name="prof"></param>
				/// <param name="terrain"></param>
				/// <param name="tt"></param>
				/// <param name="source"></param>
				protected abstract void GenerateVpFooter( SM2Profile prof, Terrain terrain, TechniqueType tt, ref string source );

				/// <summary>
				/// /
				/// </summary>
				/// <param name="prof"></param>
				/// <param name="terrain"></param>
				/// <param name="tt"></param>
				/// <param name="prog"></param>
				public virtual void DefaultVpParams( SM2Profile prof, Terrain terrain, TechniqueType tt, HighLevelGpuProgram prog )
				{
					GpuProgramParameters gparams = prog.DefaultParameters;
					gparams.IgnoreMissingParameters = true;
					gparams.SetNamedAutoConstant( "worldMatrix", GpuProgramParameters.AutoConstantType.WorldMatrix, 0 );
					gparams.SetNamedAutoConstant( "viewProjMatrix", GpuProgramParameters.AutoConstantType.ViewProjMatrix, 0 );
					gparams.SetNamedAutoConstant( "lodMorph", GpuProgramParameters.AutoConstantType.Custom,
					                              Terrain.LOD_MORPH_CUSTOM_PARAM );
					gparams.SetNamedAutoConstant( "fogParams", GpuProgramParameters.AutoConstantType.FogParams, 0 );
				}

				/// <summary>
				/// /
				/// </summary>
				/// <param name="prof"></param>
				/// <param name="terrain"></param>
				/// <param name="tt"></param>
				/// <param name="prog"></param>
				public virtual void DefaultFpParams( SM2Profile prof, Terrain terrain, TechniqueType tt, HighLevelGpuProgram prog )
				{
					GpuProgramParameters gparams = prog.DefaultParameters;
					gparams.IgnoreMissingParameters = true;
#if true
					gparams.SetNamedAutoConstant( "ambient", GpuProgramParameters.AutoConstantType.AmbientLightColor, 0 );
					gparams.SetNamedAutoConstant( "lightPosObjSpace", GpuProgramParameters.AutoConstantType.LightPositionObjectSpace, 0 );
					gparams.SetNamedAutoConstant( "lightDiffuseColor", GpuProgramParameters.AutoConstantType.LightDiffuseColor, 0 );
					gparams.SetNamedAutoConstant( "lightSpecularColor", GpuProgramParameters.AutoConstantType.LightSpecularColor, 0 );
					gparams.SetNamedAutoConstant( "eyePosObjSpace", GpuProgramParameters.AutoConstantType.CameraPositionObjectSpace, 0 );
#warning missing auto constant type "FogColor"
					//gparams.SetNamedAutoConstant("fogColor", GpuProgramParameters.AutoConstantType.FogParams, 0);
#endif
				}

				/// <summary>
				/// 
				/// </summary>
				/// <param name="prof"></param>
				/// <param name="terrain"></param>
				/// <param name="tt"></param>
				/// <param name="gpuparams"></param>
				public virtual void UpdateVpParams( SM2Profile prof, Terrain terrain, TechniqueType tt,
				                                    GpuProgramParameters gpuparams )
				{
					gpuparams.IgnoreMissingParameters = true;
					uint maxLayers = prof.GetMaxLayers( terrain );
					uint numLayers = Utility.Min( maxLayers, (uint)terrain.LayerCount );
					uint numUVMul = numLayers/4;
					if ( numUVMul%4 == 0 )
					{
						++numUVMul;
					}
					for ( uint i = 0; i < numUVMul; ++i )
					{
						var uvMul = new Vector4( terrain.GetLayerUVMultiplier( (byte)( i*4 ) ),
						                         terrain.GetLayerUVMultiplier( (byte)( i*4 + 1 ) ),
						                         terrain.GetLayerUVMultiplier( (byte)( i*4 + 2 ) ),
						                         terrain.GetLayerUVMultiplier( (byte)( i*4 + 3 ) ) );
#if true
						gpuparams.SetNamedConstant( "uvMul" + i.ToString(), uvMul );
#endif
					}
				}

				/// <summary>
				/// 
				/// </summary>
				/// <param name="prof"></param>
				/// <param name="terrain"></param>
				/// <param name="tt"></param>
				/// <param name="gpuparams"></param>
				public virtual void UpdateFpParams( SM2Profile prof, Terrain terrain, TechniqueType tt,
				                                    GpuProgramParameters gpuparams )
				{
					gpuparams.IgnoreMissingParameters = true;
					// TODO - parameterise this?
					var scaleBiasSpecular = new Vector4( 0.03f, -0.04f, 32, 1 );
					gpuparams.SetNamedConstant( "scaleBiasSpecular", scaleBiasSpecular );
				}

				/// <summary>
				/// 
				/// </summary>
				/// <param name="idx"></param>
				public virtual string GetChannel( uint idx )
				{
					uint rem = idx%4;
					switch ( rem )
					{
						case 0:
						default:
							return "r";
						case 1:
							return "g";
						case 2:
							return "b";
						case 3:
							return "a";
					}
				}
			}

			#endregion

			#region - ShaderHelperCg -

			protected class ShaderHelperCG : ShaderHelper
			{
				/// <summary>
				/// 
				/// </summary>
				/// <param name="prof"></param>
				/// <param name="terrain"></param>
				/// <param name="tt"></param>
				/// <returns></returns>
				protected override HighLevelGpuProgram CreateVertexProgram( SM2Profile prof, Terrain terrain, TechniqueType tt )
				{
					HighLevelGpuProgramManager mgr = HighLevelGpuProgramManager.Instance;
					string progName = GetVertexProgramName( prof, terrain, tt );
					var ret = (HighLevelGpuProgram)mgr.GetByName( progName );
					if ( ret == null )
					{
						ret = mgr.CreateProgram( progName, ResourceGroupManager.DefaultResourceGroupName, "cg", GpuProgramType.Vertex );
					}
					else
					{
						ret.Unload();
					}

					//ret.SetParam( "profiles", "vs_2_0 arbvp1" );
					//ret.SetParam( "entry_point", "main_vp" );
					ret.Properties[ "profiles" ] = "vs_2_0 arbvp1";
					ret.Properties[ "entry_point" ] = "main_vp";

					return ret;
				}

				/// <summary>
				/// 
				/// </summary>
				/// <param name="prof"></param>
				/// <param name="terrain"></param>
				/// <param name="tt"></param>
				/// <returns></returns>
				protected override HighLevelGpuProgram CreateFragmentProgram( SM2Profile prof, Terrain terrain, TechniqueType tt )
				{
					HighLevelGpuProgramManager mgr = HighLevelGpuProgramManager.Instance;
					string progName = GetFragmentProgramName( prof, terrain, tt );

					var ret = (HighLevelGpuProgram)mgr.GetByName( progName );
					if ( ret == null )
					{
						ret = mgr.CreateProgram( progName, ResourceGroupManager.DefaultResourceGroupName, "cg", GpuProgramType.Fragment );
					}
					else
					{
						ret.Unload();
					}

					if ( prof.IsLayerNormalMappingEnabled || prof.IsLayerParallaxMappingEnabled )
					{
						//ret.SetParam( "profiles", "ps_2_x fp40" );
						ret.Properties[ "profiles" ] = "ps_2_x fp40";
					}
					else
					{
						//ret.SetParam( "profiles", "ps_2_0 fp30" );
						ret.Properties[ "profiles" ] = "ps_2_0 fp30";
					}

					//ret.SetParam( "entry_point", "main_fp" );
					ret.Properties[ "entry_point" ] = "main_fp";

					return ret;
				}

				/// <summary>
				/// 
				/// </summary>
				/// <param name="prof"></param>
				/// <param name="terrain"></param>
				/// <param name="tt"></param>
				/// <param name="source"></param>
				protected override void GenerateVpHeader( SM2Profile prof, Terrain terrain, TechniqueType tt, ref string source )
				{
					source += "void main_vp(\n" + "float4 pos : POSITION,\n" + "float2 uv  : TEXCOORD0,\n";
					if ( tt != TechniqueType.RenderCompositeMap )
					{
						source += "float2 delta  : TEXCOORD1,\n"; // lodDelta, lodThreshold
					}

					source += "uniform float4x4 worldMatrix,\n" + "uniform float4x4 viewProjMatrix,\n" + "uniform float2   lodMorph,\n";
					// morph amount, morph LOD target

					// uv multipliers
					uint maxLayers = prof.GetMaxLayers( terrain );
					uint numLayers = Utility.Min( maxLayers, (uint)terrain.LayerCount );
					uint numUVMutipliers = ( numLayers/4 );
					if ( numLayers%4 != 0 )
					{
						++numUVMutipliers;
					}
					for ( uint i = 0; i < numUVMutipliers; ++i )
					{
						source += "uniform float4 uvMul" + i + ", \n";
					}


					source += "out float4 oPos : POSITION,\n" + "out float2 oUV : TEXCOORD0, \n" + "out float4 oPosObj : TEXCOORD1 \n";

					// layer UV's premultiplied, packed as xy/zw
					uint numUVSets = numLayers/2;
					if ( numLayers%2 != 0 )
					{
						++numUVSets;
					}
					uint texCoordSet = 2;
					if ( tt != TechniqueType.LowLod )
					{
						for ( uint i = 0; i < numUVSets; ++i )
						{
							source += ", out float4 oUV" + i + " : TEXCOORD" + texCoordSet++ + "\n";
						}
					}

					if ( prof.Parent.DebugLevel != 0 && tt != TechniqueType.RenderCompositeMap )
					{
						source += ", out float2 lodInfo : TEXCOORD" + texCoordSet++ + "\n";
					}

					source += ")\n" + "{\n" + "	float4 worldPos = mul(worldMatrix, pos);\n" + "	oPosObj = pos;\n";

					if ( tt != TechniqueType.RenderCompositeMap )
					{
						// determine whether to apply the LOD morph to this vertex
						// we store the deltas against all vertices so we only want to apply 
						// the morph to the ones which would disappear. The target LOD which is
						// being morphed to is stored in lodMorph.y, and the LOD at which 
						// the vertex should be morphed is stored in uv.w. If we subtract
						// the former from the latter, and arrange to only morph if the
						// result is negative (it will only be -1 in fact, since after that
						// the vertex will never be indexed), we will achieve our aim.
						// sign(vertexLOD - targetLOD) == -1 is to morph
						source += "	float toMorph = -min(0, sign(delta.y - lodMorph.y));\n";
						// this will either be 1 (morph) or 0 (don't morph)
						if ( prof.Parent.DebugLevel != 0 )
						{
							// x == LOD level (-1 since value is target level, we want to display actual)
							source += "lodInfo.x = (lodMorph.y - 1) / " + terrain.NumLodLevels + ";\n";
							// y == LOD morph
							source += "lodInfo.y = toMorph * lodMorph.x;\n";
						}

						//morph
						switch ( terrain.Alignment )
						{
							case Alignment.Align_X_Y:
								break;
							case Alignment.Align_X_Z:
								source += "	worldPos.y += delta.x * toMorph * lodMorph.x;\n";
								break;
							case Alignment.Align_Y_Z:
								break;
						}
					}

					// generate UVs
					if ( tt == TechniqueType.LowLod )
					{
						//passtrough
						source += "	oUV = uv;\n";
					}
					else
					{
						for ( uint i = 0; i < numUVSets; ++i )
						{
							uint layer = i*2;
							uint uvMulIdx = layer/4;

							source += "	oUV" + i + ".xy = " + " uv.xy * uvMul" + uvMulIdx + "." + GetChannel( layer ) + ";\n";
							source += "	oUV" + i + ".zw = " + " uv.xy * uvMul" + uvMulIdx + "." + GetChannel( layer + 1 ) + ";\n";
						}
					}
				}

				/// <summary>
				/// 
				/// </summary>
				/// <param name="prof"></param>
				/// <param name="terrain"></param>
				/// <param name="tt"></param>
				/// <param name="source"></param>
				protected override void GenerateFpHeader( SM2Profile prof, Terrain terrain, TechniqueType tt, ref string source )
				{
					// Main header
					source += // helpers
						"float4 expand(float4 v)\n" + "{ \n" + "	return v * 2 - 1;\n" + "}\n\n\n" + "float4 main_fp(\n" +
						"float2 uv : TEXCOORD0,\n" + "float4 position : TEXCOORD1,\n";

					// UV's premultiplied, packed as xy/zw
					uint maxLayers = prof.GetMaxLayers( terrain );
					uint numBlendTextures = Utility.Min( terrain.GetBlendTextureCount( (byte)maxLayers ),
					                                     terrain.GetBlendTextureCount() );
					uint numLayers = Utility.Min( maxLayers, (uint)terrain.LayerCount );
					uint numUVSets = numLayers/2;
					if ( numLayers%2 != 0 )
					{
						++numUVSets;
					}

					uint texCoordSet = 2;
					if ( tt != TechniqueType.LowLod )
					{
						for ( uint i = 0; i < numUVSets; ++i )
						{
							source += "float4 layerUV" + i + " : TEXCOORD" + texCoordSet++ + ", \n";
						}
					}
					if ( prof.Parent.DebugLevel != 0 && tt != TechniqueType.RenderCompositeMap )
					{
						source += "float2 lodInfo : TEXCOORD" + texCoordSet++ + ", \n";
					}

					source += // Only 1 light supported in this version
						// deferred shading profile / generator later, ok? :)
						"uniform float4 ambient,\n" + "uniform float4 lightPosObjSpace,\n" + "uniform float3 lightDiffuseColor,\n" +
						"uniform float3 lightSpecularColor,\n" + "uniform float3 eyePosObjSpace,\n" + // pack scale, bias and specular
						"uniform float4 scaleBiasSpecular,\n";

					if ( tt == TechniqueType.LowLod )
					{
						// single composite map covers all the others below
						source += "uniform sampler2D compositeMap : register(s0)\n";
					}
					else
					{
						source += "uniform sampler2D globalNormal : register(s0)\n";

						uint currentSamplerIdx = 1;
						if ( terrain.IsGlobalColorMapEnabled && prof.IsGlobalColorMapEnabled )
						{
							source += ", uniform sampler2D globalColorMap : register(s" + currentSamplerIdx++ + ")\n";
						}
						if ( prof.IsLightMapEnabled )
						{
							source += ", uniform sampler2D lightMap : register(s" + currentSamplerIdx++ + ")\n";
						}
						// Blend textures - sampler definitions
						for ( uint i = 0; i < numBlendTextures; ++i )
						{
							source += ", uniform sampler2D blendTex" + i + " : register(s" + currentSamplerIdx++ + ")\n";
						}

						// Layer textures - sampler definitions & UV multipliers
						for ( uint i = 0; i < numLayers; ++i )
						{
							source += ", uniform sampler2D difftex" + i + " : register(s" + currentSamplerIdx++ + ")\n";
							source += ", uniform sampler2D normtex" + i + " : register(s" + currentSamplerIdx++ + ")\n";
						}
					}

					source += ") : COLOR\n" + "{\n" + "	float4 outputCol;\n" + "	float shadow = 1.0;\n" + // base colour
					          "	outputCol = float4(0,0,0,1);\n";

					if ( tt != TechniqueType.LowLod )
					{
						source += // global normal
							"	float3 normal = expand(tex2D(globalNormal, uv)).rgb;\n";
					}

					source += "	float3 lightDir = \n" + "		lightPosObjSpace.xyz -  (position.xyz * lightPosObjSpace.w);\n" +
					          "	float3 eyeDir = eyePosObjSpace - position.xyz;\n" + // set up accumulation areas
					          "	float3 diffuse = float3(0,0,0);\n" + "	float specular = 0;\n";

					if ( tt == TechniqueType.LowLod )
					{
						// we just do a single calculation from composite map
						source += "	float4 composite = tex2D(compositeMap, uv);\n" + "	diffuse = composite.rgb;\n";
						// TODO - specular; we'll need normals for this!
					}
					else
					{
						// set up the blend values
						for ( uint i = 0; i < numBlendTextures; ++i )
						{
							source += "	float4 blendTexVal" + i + " = tex2D(blendTex" + i + ", uv);\n";
						}

						if ( prof.IsLayerNormalMappingEnabled )
						{
							// derive the tangent space basis
							// we do this in the pixel shader because we don't have per-vertex normals
							// because of the LOD, we use a normal map
							// tangent is always +x or -z in object space depending on alignment
							switch ( terrain.Alignment )
							{
								case Alignment.Align_X_Y:
								case Alignment.Align_X_Z:
									source += "	float3 tangent = float3(1, 0, 0);\n";
									break;
								case Alignment.Align_Y_Z:
									source += "	float3 tangent = float3(0, 0, -1);\n";
									break;
							}

							source += "	float3 binormal = normalize(cross(tangent, normal));\n";
							// note, now we need to re-cross to derive tangent again because it wasn't orthonormal
							source += "	tangent = normalize(cross(normal, binormal));\n";
							// derive final matrix
							source += "	float3x3 TBN = float3x3(tangent, binormal, normal);\n";

							// set up lighting result placeholders for interpolation
							source += "	float4 litRes, litResLayer;\n";
							source += "	float3 TSlightDir, TSeyeDir, TShalfAngle, TSnormal;\n";
							if ( prof.IsLayerParallaxMappingEnabled )
							{
								source += "	float displacement;\n";
							}
							// move 
							source += "	TSlightDir = normalize(mul(TBN, lightDir));\n";
							source += "	TSeyeDir = normalize(mul(TBN, eyeDir));\n";
						}
						else
						{
							// simple per-pixel lighting with no normal mapping
							source += "	lightDir = normalize(lightDir);\n";
							source += "	eyeDir = normalize(eyeDir);\n";
							source += "	float3 halfAngle = normalize(lightDir + eyeDir);\n";
							source += "	float4 litRes = lit(dot(lightDir, normal), dot(halfAngle, normal), scaleBiasSpecular.z);\n";
						}
					}
				}

				/// <summary>
				/// 
				/// </summary>
				/// <param name="prof"></param>
				/// <param name="terrain"></param>
				/// <param name="tt"></param>
				/// <param name="layer"></param>
				/// <param name="source"></param>
				protected override void GenerateVpLayer( SM2Profile prof, Terrain terrain, TechniqueType tt, uint layer,
				                                         ref string source )
				{
					// nothing to do
				}

				/// <summary>
				/// 
				/// </summary>
				/// <param name="prof"></param>
				/// <param name="terrain"></param>
				/// <param name="tt"></param>
				/// <param name="layer"></param>
				/// <param name="source"></param>
				protected override void GenerateFpLayer( SM2Profile prof, Terrain terrain, TechniqueType tt, uint layer,
				                                         ref string source )
				{
					uint uvIdx = layer/2;
					string uvChannels = layer%2 != 0 ? ".zw" : ".xy";
					uint blendIdx = ( layer - 1 )/4;

					// generate UV
					source += "	float2 uv" + layer + " = layerUV" + uvIdx + uvChannels + ";\n";

					// calculate lighting here if normal mapping
					if ( prof.IsLayerNormalMappingEnabled )
					{
						if ( prof.IsLayerParallaxMappingEnabled && tt != TechniqueType.RenderCompositeMap )
						{
							// modify UV - note we have to sample an extra time
							source += "	displacement = tex2D(normtex" + layer + ", uv" + layer + ").a\n" +
							          "		* scaleBiasSpecular.x + scaleBiasSpecular.y;\n";
							source += "	uv" + layer + " += TSeyeDir.xy * displacement;\n";
						}

						// access TS normal map
						source += "	TSnormal = expand(tex2D(normtex" + layer + ", uv" + layer + ")).rgb;\n";
						source += "	TShalfAngle = normalize(TSlightDir + TSeyeDir);\n";
						source += "	litResLayer = lit(dot(TSlightDir, TSnormal), dot(TShalfAngle, TSnormal), scaleBiasSpecular.z);\n";
						if ( layer == 0 )
						{
							source += "	litRes = litResLayer;\n";
						}
						else
						{
							source += "	litRes = lerp(litRes, litResLayer, blendTexVal" + blendIdx + "." + GetChannel( layer - 1 ) + ");\n";
						}
					}

					// sample diffuse texture
					source += "	float4 diffuseSpecTex" + layer + " = tex2D(difftex" + layer + ", uv" + layer + ");\n";

					// apply to common
					if ( layer == 0 )
					{
						source += "	diffuse = diffuseSpecTex0.rgb;\n";
						if ( prof.IsLayerSpecularMappingEnabled )
						{
							source += "	specular = diffuseSpecTex0.a;\n";
						}
					}
					else
					{
						source += "	diffuse = lerp(diffuse, diffuseSpecTex" + layer + ".rgb, blendTexVal" + blendIdx + "." +
						          GetChannel( layer - 1 ) + ");\n";
						if ( prof.IsLayerSpecularMappingEnabled )
						{
							source += "	specular = lerp(specular, diffuseSpecTex" + layer + ".a, blendTexVal" + blendIdx + "." +
							          GetChannel( layer - 1 ) + ");\n";
						}
					}
				}

				/// <summary>
				/// 
				/// </summary>
				/// <param name="prof"></param>
				/// <param name="terrain"></param>
				/// <param name="tt"></param>
				/// <param name="source"></param>
				protected override void GenerateVpFooter( SM2Profile prof, Terrain terrain, TechniqueType tt, ref string source )
				{
					source += "	oPos = mul(viewProjMatrix, worldPos);\n" + "	oUV = uv.xy;\n" + "}\n";
				}

				/// <summary>
				/// 
				/// </summary>
				/// <param name="prof"></param>
				/// <param name="terrain"></param>
				/// <param name="tt"></param>
				/// <param name="source"></param>
				protected override void GenerateFpFooter( SM2Profile prof, Terrain terrain, TechniqueType tt, ref string source )
				{
					if ( tt == TechniqueType.LowLod )
					{
						source += "	outputCol.rgb = diffuse;\n";
					}
					else
					{
						if ( terrain.IsGlobalColorMapEnabled && prof.IsGlobalColorMapEnabled )
						{
							// sample colour map and apply to diffuse
							source += "	diffuse *= tex2D(globalColorMap, uv).rgb;\n";
						}
						if ( prof.IsLightMapEnabled )
						{
							// sample lightmap
							source += "	shadow = tex2D(lightMap, uv).r;\n";
						}

						// diffuse lighting
						source += "	outputCol.rgb += ambient * diffuse + litRes.y * lightDiffuseColor * diffuse * shadow;\n";

						// specular default
						if ( !prof.IsLayerSpecularMappingEnabled )
						{
							source += "	specular = 1.0;\n";
						}

						if ( tt == TechniqueType.RenderCompositeMap )
						{
							// Raw specular is embedded in composite map alpha
							source += "	outputCol.a = specular * shadow;\n";
						}
						else
						{
							// Apply specular
							source += "	outputCol.rgb += litRes.z * lightSpecularColor * specular * shadow;\n";

							if ( prof.Parent.DebugLevel != 0 )
							{
								source += "	outputCol.rg += lodInfo.xy;\n";
							}
						}
					}
					// Final return
					source += "	return outputCol;\n" + "}\n";
				}
			}

			#endregion

			#region - shaderhelperHLSL -

			/// <summary>
			/// 
			/// </summary>
			protected class ShaderHelperHLSL : ShaderHelperCG
			{
				/// <summary>
				/// 
				/// </summary>
				/// <param name="prof"></param>
				/// <param name="terrain"></param>
				/// <param name="tt"></param>
				/// <returns></returns>
				protected override HighLevelGpuProgram CreateVertexProgram( SM2Profile prof, Terrain terrain, TechniqueType tt )
				{
					HighLevelGpuProgramManager mgr = HighLevelGpuProgramManager.Instance;
					string progName = GetVertexProgramName( prof, terrain, tt );

					var ret = (HighLevelGpuProgram)mgr.GetByName( progName );
					if ( ret == null )
					{
						ret = mgr.CreateProgram( progName, ResourceGroupManager.DefaultResourceGroupName, "hlsl", GpuProgramType.Vertex );
					}
					else
					{
						ret.Unload();
					}

					//ret.SetParam("target", "vs_2_0");
					//ret.SetParam("entry_point", "main_vp");
					ret.Properties[ "target" ] = "vs_2_0";
					ret.Properties[ "entry_point" ] = "main_vp";

					return ret;
				}

				/// <summary>
				/// 
				/// </summary>
				/// <param name="prof"></param>
				/// <param name="terrain"></param>
				/// <param name="tt"></param>
				/// <returns></returns>
				protected override HighLevelGpuProgram CreateFragmentProgram( SM2Profile prof, Terrain terrain, TechniqueType tt )
				{
					HighLevelGpuProgramManager mgr = HighLevelGpuProgramManager.Instance;
					string progName = GetFragmentProgramName( prof, terrain, tt );

					var ret = (HighLevelGpuProgram)mgr.GetByName( progName );
					if ( ret == null )
					{
						ret = mgr.CreateProgram( progName, ResourceGroupManager.DefaultResourceGroupName, "hlsl", GpuProgramType.Fragment );
					}
					else
					{
						ret.Unload();
					}
#warning very high shader version
					//ret.SetParam( "target", "ps_3_0" );
					//ret.SetParam( "entry_point", "main_fp" );
					ret.Properties[ "target" ] = "ps_3_0";
					ret.Properties[ "entry_point" ] = "main_fp";

					return ret;
				}
			}

			#endregion

			#region - ShaderHelperGLSL -

			/// <summary>
			/// 
			/// </summary>
			protected class ShaderHelperGLSL : ShaderHelper
			{
				/// <summary>
				/// 
				/// </summary>
				/// <param name="prof"></param>
				/// <param name="terrain"></param>
				/// <param name="tt"></param>
				/// <returns></returns>
				protected override HighLevelGpuProgram CreateVertexProgram( SM2Profile prof, Terrain terrain, TechniqueType tt )
				{
					HighLevelGpuProgramManager mgr = HighLevelGpuProgramManager.Instance;
					string progName = GetVertexProgramName( prof, terrain, tt );
					switch ( tt )
					{
						case TechniqueType.HighLod:
							progName += "/hlod";
							break;
						case TechniqueType.LowLod:
							progName += "/llod";
							break;
						case TechniqueType.RenderCompositeMap:
							progName += "/comp";
							break;
					}

					var ret = (HighLevelGpuProgram)mgr.GetByName( progName );
					if ( ret == null )
					{
						ret = mgr.CreateProgram( progName, ResourceGroupManager.DefaultResourceGroupName, "glsl", GpuProgramType.Vertex );
					}
					else
					{
						ret.Unload();
					}

					return ret;
				}

				/// <summary>
				/// 
				/// </summary>
				/// <param name="prof"></param>
				/// <param name="terrain"></param>
				/// <param name="tt"></param>
				/// <returns></returns>
				protected override HighLevelGpuProgram CreateFragmentProgram( SM2Profile prof, Terrain terrain, TechniqueType tt )
				{
					HighLevelGpuProgramManager mgr = HighLevelGpuProgramManager.Instance;
					string progName = GetVertexProgramName( prof, terrain, tt );

					var ret = (HighLevelGpuProgram)mgr.GetByName( progName );

					if ( ret == null )
					{
						ret = mgr.CreateProgram( progName, ResourceGroupManager.DefaultResourceGroupName, "glsl", GpuProgramType.Fragment );
					}
					else
					{
						ret.Unload();
					}

					return ret;
				}

				/// <summary>
				/// 
				/// </summary>
				/// <param name="prof"></param>
				/// <param name="terrain"></param>
				/// <param name="tt"></param>
				/// <param name="source"></param>
				protected override void GenerateVpHeader( SM2Profile prof, Terrain terrain, TechniqueType tt, ref string source )
				{
					//not implemted yet
				}

				/// <summary>
				/// 
				/// </summary>
				/// <param name="prof"></param>
				/// <param name="terrain"></param>
				/// <param name="tt"></param>
				/// <param name="source"></param>
				protected override void GenerateFpFooter( SM2Profile prof, Terrain terrain, TechniqueType tt, ref string source )
				{
					//not implemted yet
				}

				/// <summary>
				/// 
				/// </summary>
				/// <param name="prof"></param>
				/// <param name="terrain"></param>
				/// <param name="tt"></param>
				/// <param name="source"></param>
				protected override void GenerateFpHeader( SM2Profile prof, Terrain terrain, TechniqueType tt, ref string source )
				{
					//not implemted yet
				}

				/// <summary>
				/// 
				/// </summary>
				/// <param name="prof"></param>
				/// <param name="terrain"></param>
				/// <param name="tt"></param>
				/// <param name="layer"></param>
				/// <param name="source"></param>
				protected override void GenerateVpLayer( SM2Profile prof, Terrain terrain, TechniqueType tt, uint layer,
				                                         ref string source )
				{
					//not implemted yet
				}

				/// <summary>
				/// 
				/// </summary>
				/// <param name="prof"></param>
				/// <param name="terrain"></param>
				/// <param name="tt"></param>
				/// <param name="layer"></param>
				/// <param name="source"></param>
				protected override void GenerateFpLayer( SM2Profile prof, Terrain terrain, TechniqueType tt, uint layer,
				                                         ref string source )
				{
					//not implemted yet
				}

				/// <summary>
				/// 
				/// </summary>
				/// <param name="prof"></param>
				/// <param name="terrain"></param>
				/// <param name="tt"></param>
				/// <param name="source"></param>
				protected override void GenerateVpFooter( SM2Profile prof, Terrain terrain, TechniqueType tt, ref string source )
				{
					//not implemted yet
				}
			}

			#endregion
		}

		#endregion
	}
}