using Axiom.Core;
using Axiom.Graphics;

namespace Axiom.Components.RTShaderSystem
{
	internal class HardwareSkinningFactory : SubRenderStateFactory
	{
		private static int HsMaxWeightCount = 4;

		//A set of custom shadow caster materials
		private readonly Material[] customShadowCasterMaterialsLinear =
			new Material[HardwareSkinningFactory.HsMaxWeightCount];

		private readonly Material[] customShadowCasterMaerialsDualQuaternion =
			new Material[HardwareSkinningFactory.HsMaxWeightCount];

		//A set of custom shadow receiver materials
		private readonly Material[] customShadowReceiverMaterialsLinear =
			new Material[HardwareSkinningFactory.HsMaxWeightCount];

		private readonly Material[] customShadowReceiverMaterialsDualQuaternion =
			new Material[HardwareSkinningFactory.HsMaxWeightCount];

		public HardwareSkinningFactory()
		{
		}

		public override SubRenderState CreateInstance( Scripting.Compiler.ScriptCompiler compiler,
		                                               Scripting.Compiler.AST.PropertyAbstractNode prop, Pass pass,
		                                               SGScriptTranslator stranslator )
		{
			if ( prop.Name == "hardware_skinning" )
			{
				bool hasError = false;
				int boneCount = 0;
				int weightCount = 0;
				string skinningType = string.Empty;
				SkinningType skinType = SkinningType.Linear;
				bool correctAntipodalityHandling = false;
				bool scalingShearingSupport = false;

				if ( prop.Values.Count >= 2 )
				{
					int it = 0;

					if ( SGScriptTranslator.GetInt( prop.Values[ it ], out boneCount ) == false )
					{
						hasError = true;
					}

					it++;
					if ( SGScriptTranslator.GetInt( prop.Values[ it ], out weightCount ) == false )
					{
						hasError = true;
					}

					if ( prop.Values.Count >= 5 )
					{
						it++;
						SGScriptTranslator.GetString( prop.Values[ it ], out skinningType );

						it++;
						SGScriptTranslator.GetBoolean( prop.Values[ it ], out correctAntipodalityHandling );

						it++;
						SGScriptTranslator.GetBoolean( prop.Values[ it ], out scalingShearingSupport );
					}

					//If the skinningType is not specified or is specified incorretly, default to linear
					if ( skinningType == "dual_quaternion" )
					{
						skinType = SkinningType.DualQuaternion;
					}
					else
					{
						skinType = SkinningType.Linear;
					}
				}
				if ( hasError )
				{
					//TODO
					//compiler.AddError(...);
					return null;
				}
				else
				{
					//create and update the hardware skinning sub render state
					SubRenderState subRenderState = CreateOrRetrieveInstance( stranslator );
					var hardSkinSrs = (HardwareSkinning)subRenderState;
					hardSkinSrs.SetHardwareSkinningParam( boneCount, weightCount, skinType, correctAntipodalityHandling,
					                                      scalingShearingSupport );

					return subRenderState;
				}
			}


			return null;
		}

		public override void WriteInstance( Serialization.MaterialSerializer ser, SubRenderState subRenderState,
		                                    Pass srcPass, Pass dstPass )
		{
			//TODO
			//ser.WriteAttribute(4, "hardware_skinning");
			//HardwareSkinning hardSkkinSrs = subRenderState as HardwareSkinning;
			//ser.WriteValue(hardSkkinSrs.BoneCount.ToString());
			//ser.WriteValue(hardSkkinSrs.WeightCount.ToString());

			////Correct antipodality handling and scaling shearing support are only really valid for dual quaternion skinning
			//if (hardSkkinSrs.SkinningType == SkinningType.DualQuaternion)
			//{
			//    ser.WriteValue("dual_quaternion");
			//    ser.WriteValue(hardSkkinSrs.HasCorrectAntipodalityHandling.ToString());
			//    ser.WriteValue(hardSkkinSrs.HasScalingShearingSupport.ToString());
			//}
		}

		public void SetCustomShadowCasterMaterials( SkinningType skinningType, Material caster1Weight,
		                                            Material caster2Weight, Material caster3Weight,
		                                            Material caster4Weight )
		{
			if ( skinningType == SkinningType.DualQuaternion )
			{
				customShadowCasterMaerialsDualQuaternion[ 0 ] = caster1Weight;
				customShadowCasterMaerialsDualQuaternion[ 1 ] = caster2Weight;
				customShadowCasterMaerialsDualQuaternion[ 2 ] = caster3Weight;
				customShadowCasterMaerialsDualQuaternion[ 3 ] = caster4Weight;
			}
			else
			{
				customShadowCasterMaterialsLinear[ 0 ] = caster1Weight;
				customShadowCasterMaterialsLinear[ 1 ] = caster2Weight;
				customShadowCasterMaterialsLinear[ 2 ] = caster3Weight;
				customShadowCasterMaterialsLinear[ 3 ] = caster4Weight;
			}
		}

		public void SetCustomShadowReceiverMaterials( SkinningType skinningType, Material receiver1Weight,
		                                              Material receiver2Weight, Material receiver3Weight,
		                                              Material receiver4Weight )
		{
			if ( skinningType == SkinningType.DualQuaternion )
			{
				customShadowReceiverMaterialsDualQuaternion[ 0 ] = receiver1Weight;
				customShadowReceiverMaterialsDualQuaternion[ 1 ] = receiver2Weight;
				customShadowReceiverMaterialsDualQuaternion[ 2 ] = receiver3Weight;
				customShadowReceiverMaterialsDualQuaternion[ 3 ] = receiver4Weight;
			}
			else
			{
				customShadowReceiverMaterialsLinear[ 0 ] = receiver1Weight;
				customShadowReceiverMaterialsLinear[ 1 ] = receiver2Weight;
				customShadowReceiverMaterialsLinear[ 2 ] = receiver3Weight;
				customShadowReceiverMaterialsLinear[ 3 ] = receiver4Weight;
			}
		}

		public override string Type
		{
			get
			{
				return HardwareSkinning.SGXType;
			}
		}

		public int MaxCalculableBoneCount { get; set; }

		internal Graphics.Material GetCustomShadowCasterMaterial( SkinningType skinningType, int index )
		{
			if ( skinningType == SkinningType.DualQuaternion )
			{
				return customShadowCasterMaerialsDualQuaternion[ index ];
			}
			else
			{
				return customShadowCasterMaterialsLinear[ index ];
			}
		}

		internal Material GetCustomShadowReceiverMaterial( SkinningType skinningType, int index )
		{
			if ( skinningType == SkinningType.DualQuaternion )
			{
				return customShadowReceiverMaterialsDualQuaternion[ index ];
			}
			else
			{
				return customShadowReceiverMaterialsLinear[ index ];
			}
		}

		public void PrepareEntityForSkinning( Entity entity, SkinningType skinningType, bool correctAntipodalityHandling,
		                                      bool shearScale )
		{
			if ( entity != null )
			{
				//TODO!
				int lodLevels = entity.MeshLodIndex + 1;
				for ( int indexLod = 0; indexLod < lodLevels; indexLod++ )
				{
					Entity curEntity = entity;
					if ( indexLod > 0 )
					{
						// curEntity = entity.GetManualLodLevel(indexLod - 1);
					}

					int boneCount = 0;
					int weightCount = 0;
					bool isValid = ExtractSkeletonData( curEntity, out boneCount, out weightCount );
					int numSubEntites = curEntity.SubEntityCount;
					for ( int indexSub = 0; indexSub < numSubEntites; indexSub++ )
					{
						SubEntity subEntity = curEntity.GetSubEntity( indexSub );
						Material mat = subEntity.Material;
						ImprintSkeletonData( mat, isValid, boneCount, weightCount, skinningType,
						                     correctAntipodalityHandling, shearScale );
					}
				}
			}
		}

		protected bool ExtractSkeletonData( Entity entity, out int boneCount, out int weightCount )
		{
			bool isValidData = false;
			boneCount = 0;
			weightCount = 0;

			//Check if we have pose animation which the HS sub render state does not
			//know how to handle
			bool hasVertexAnim = entity.Mesh.HasVertexAnimation;

			//gather data on the skeleton
			if ( !hasVertexAnim && entity.HasSkeleton )
			{
				//get weights count
				Mesh mesh = entity.Mesh;
				//              nb: OGRE source gets ..blendIndexToBoneIndexMap.Count, is this accurate?
				boneCount = mesh.BoneAssignmentList.Count;

				int totalMeshes = mesh.SubMeshCount;
				for ( int i = 0; i < totalMeshes; i++ )
				{
					var ro = new RenderOperation();
					SubMesh subMesh = mesh.GetSubMesh( i );
					subMesh.GetRenderOperation( ro, 0 );

					//get the largest bone assignment
					if ( boneCount > subMesh.BoneAssignmentList.Count )
					{
					}
					else
					{
						boneCount = subMesh.BoneAssignmentList.Count;
					}

					//go over vertex decleration
					//check that they have blend indices and blend weights
					VertexElement declWeights =
						ro.vertexData.vertexDeclaration.FindElementBySemantic( VertexElementSemantic.BlendWeights, 0 );
					VertexElement declIndexes =
						ro.vertexData.vertexDeclaration.FindElementBySemantic( VertexElementSemantic.BlendIndices, 0 );
					if ( ( declWeights != null ) && ( declIndexes != null ) )
					{
						isValidData = true;
						switch ( declWeights.Type )
						{
							case VertexElementType.Float1:
								weightCount = Axiom.Math.Utility.Clamp( weightCount, weightCount, 1 );
								break;
							case VertexElementType.Float2:
								weightCount = Axiom.Math.Utility.Clamp( weightCount, weightCount, 2 );
								break;
							case VertexElementType.Float3:
								weightCount = Axiom.Math.Utility.Clamp( weightCount, weightCount, 3 );
								break;
							case VertexElementType.Float4:
								weightCount = Axiom.Math.Utility.Clamp( weightCount, weightCount, 4 );
								break;
							default:
								isValidData = false;
								break;
						}
						if ( isValidData == false )
						{
							break;
						}
					}
				}
			}
			return isValidData;
		}

		protected bool ImprintSkeletonData( Material material, bool isValid, int boneCount, int weightCount,
		                                    SkinningType skinningType, bool correctAntipodalityHandling,
		                                    bool scalingShearingSupport )
		{
			bool isUpdated = false;
			if ( material.TechniqueCount > 0 )
			{
				SkinningData data = null;


				//Get the previous skinning data if available
				//TODO!
				/*
                 * 
                dynamic binding = null; //material.GetTechnique(0).getUserObjectBindings();
                
                if (binding != null)
                {
                    if (binding is SkinningData)
                    {
                        data = binding as SkinningData;
                    }
                }
                if (data != null)
                {
                    if ((data.IsValid == true) && (isValid == false) || (data.MaxBoneCount < boneCount) || (data.MaxWeightCount < weightCount))
                    {
                        //update the data
                        isUpdated = true;
                        data.IsValid &= isValid;
                        data.MaxBoneCount = Axiom.Math.Utility.Max(data.MaxBoneCount, boneCount);
                        data.MaxWeightCount = Axiom.Math.Utility.Max(data.MaxWeightCount, weightCount);
                        data.SkinningType = skinningType;
                        data.CorrectAntipodalityHandling = correctAntipodalityHandling;
                        data.ScalingShearingSupport = scalingShearingSupport;

                        //update the data in the material and invalidate it in the RTShader system
                        //do it will be regenerating

                        //TODO
                        //binding.SetUserAny(HardwareSkinning.HsDataBindName, data);

                        int schemeCount = ShaderGenerator.Instance.RTShaderSchemeCount;
                        for (int i = 0; i < schemeCount; i++)
                        {
                            //invalidate the material so it will bre recreated with the correct amount of bones and weights
                            string schemName = ShaderGenerator.Instance.GetRTShaderScheme(i);
                            Axiom.Components.RTShaderSystem.ShaderGenerator.Instance.InvalidateMaterial(schemName, material.Name, material.Group);
                        }
                    }
                }
                */
			}

			return isUpdated;
		}

		protected override SubRenderState CreateInstanceImpl()
		{
			var skin = new HardwareSkinning();
			skin.SetCreator( this );
			return skin;
		}
	}
}