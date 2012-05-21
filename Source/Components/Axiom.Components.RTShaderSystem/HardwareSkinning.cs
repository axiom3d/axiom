using System;
using Axiom.Graphics;

namespace Axiom.Components.RTShaderSystem
{
	internal enum SkinningType
	{
		Linear,
		DualQuaternion
	}

	internal class SkinningData
	{
		public bool IsValid = true;
		public int MaxBoneCount = 0;
		public int MaxWeightCount = 0;
		public SkinningType SkinningType = SkinningType.Linear;
		public bool CorrectAntipodalityHandling = false;
		public bool ScalingShearingSupport = false;

		public SkinningData()
		{
		}

		public SkinningData( bool isValid, int maxBoneCount, int maxWeightCount, SkinningType sType,
		                     bool correctAntipodality, bool scalingShearingSupport )
		{
			this.IsValid = isValid;
			this.MaxBoneCount = maxBoneCount;
			this.MaxWeightCount = maxWeightCount;
			this.SkinningType = sType;
			this.CorrectAntipodalityHandling = correctAntipodality;
			this.ScalingShearingSupport = scalingShearingSupport;
		}
	}

	internal class HardwareSkinning : SubRenderState
	{
		public static string SGXType = "SGX_HardwareSkinning";
		public static string HsDataBindName = "HS_SRS_DATA";
		private HardwareSkinningFactory creator;
		private SkinningType skinningType;
		private DualQuaternionSkinning dualQuat;
		private LinearSkinning linear;
		private HardwareSkinningTechnique activeTechnique;

		public HardwareSkinning()
		{
			this.creator = null;
			this.skinningType = RTShaderSystem.SkinningType.Linear;
		}

		public void SetHardwareSkinningParam( int boneCount, int weightCount, SkinningType skinningType,
		                                      bool correctAntipodalityHandling, bool scalingShearingSupport )
		{
			this.skinningType = skinningType;

			if ( skinningType == RTShaderSystem.SkinningType.DualQuaternion )
			{
				if ( this.dualQuat == null )
				{
					this.dualQuat = new DualQuaternionSkinning();
				}
				this.activeTechnique = this.dualQuat;
			}
			else
			{
				if ( this.linear == null )
				{
					this.linear = new LinearSkinning();
				}
				this.activeTechnique = this.linear;
			}

			this.activeTechnique.SetHardwareSkinningParam( boneCount, weightCount, correctAntipodalityHandling,
			                                               scalingShearingSupport );
		}

		public override bool PreAddToRenderState( TargetRenderState targetRenderState, Graphics.Pass srcPass,
		                                          Graphics.Pass dstPass )
		{
			bool isValid = true;
			Technique firstTech = srcPass.Parent.Parent.GetTechnique( 0 );
			//TODO
			//var hsAny = firstTech.UserObjectBindings.GetUserAny(HardwareSkinning.HsDataBindName);
			if ( false ) //hsAny.isEmpty == false)
			{
			}
			//If there is no associated techniqe, default to linear skinning as a pass-through
			if ( this.activeTechnique == null )
			{
				SetHardwareSkinningParam( 0, 0, RTShaderSystem.SkinningType.Linear, false, false );
			}
			int boneCount = this.activeTechnique.BoneCount;
			int weightCount = this.activeTechnique.WeightCount;

			bool doBoneCalculations = isValid && ( boneCount != 0 ) && ( boneCount <= 256 ) && ( weightCount != 0 ) &&
			                          ( weightCount <= 4 ) &&
			                          ( ( this.creator == null ) || ( boneCount <= this.creator.MaxCalculableBoneCount ) );

			this.activeTechnique.DoBoneCalculations = doBoneCalculations;


			if ( ( doBoneCalculations ) && ( this.creator != null ) )
			{
				//update the receiver and caster materials
				if ( dstPass.Parent.ShadowCasterMaterial == null )
				{
					Material mat = this.creator.GetCustomShadowCasterMaterial( this.skinningType, weightCount - 1 );
					dstPass.Parent.SetShadowCasterMaterial( mat.Name );
				}

				if ( dstPass.Parent.ShadowReceiverMaterial == null )
				{
					Material mat = this.creator.GetCustomShadowCasterMaterial( this.skinningType, weightCount - 1 );
					dstPass.Parent.SetShadowReceiverMaterial( mat.Name );
				}
			}
			return true;
		}

		protected override bool ResolveParameters( ProgramSet programSet )
		{
			return this.activeTechnique.ResolveParameters( programSet );
		}

		protected override bool ResolveDependencies( ProgramSet programSet )
		{
			return this.activeTechnique.ResolveDependencies( programSet );
		}

		protected override bool AddFunctionInvocations( ProgramSet programSet )
		{
			return this.activeTechnique.AddFunctionInvocations( programSet );
		}

		public void SetCreator( HardwareSkinningFactory creator )
		{
			this.creator = creator;
		}

		public override string Type
		{
			get
			{
				return "SGX_HardwareSkinning";
			}
		}

		public int BoneCount
		{
			get
			{
				return this.activeTechnique.BoneCount;
			}
		}

		public int WeightCount
		{
			get
			{
				return this.activeTechnique.WeightCount;
			}
		}

		public SkinningType SkinningType
		{
			get
			{
				return this.skinningType;
			}
		}

		public bool HasCorrectAntipodalityHandling
		{
			get
			{
				return this.activeTechnique.HasCorrectAntipodalityHandling;
			}
		}

		public bool HasScalingShearingSupport
		{
			get
			{
				return this.activeTechnique.HasScalingShearingSupport;
			}
		}


		public override int ExecutionOrder
		{
			get
			{
				return (int)FFPRenderState.FFPShaderStage.Transform;
			}
		}
	}
}