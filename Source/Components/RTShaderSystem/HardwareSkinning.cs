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
            IsValid = isValid;
            MaxBoneCount = maxBoneCount;
            MaxWeightCount = maxWeightCount;
            SkinningType = sType;
            CorrectAntipodalityHandling = correctAntipodality;
            ScalingShearingSupport = scalingShearingSupport;
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
            creator = null;
            skinningType = RTShaderSystem.SkinningType.Linear;
        }

        public void SetHardwareSkinningParam( int boneCount, int weightCount, SkinningType skinningType,
                                              bool correctAntipodalityHandling, bool scalingShearingSupport )
        {
            this.skinningType = skinningType;

            if ( skinningType == RTShaderSystem.SkinningType.DualQuaternion )
            {
                if ( dualQuat == null )
                {
                    dualQuat = new DualQuaternionSkinning();
                }
                activeTechnique = dualQuat;
            }
            else
            {
                if ( linear == null )
                {
                    linear = new LinearSkinning();
                }
                activeTechnique = linear;
            }

            activeTechnique.SetHardwareSkinningParam( boneCount, weightCount, correctAntipodalityHandling,
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
            if ( activeTechnique == null )
            {
                SetHardwareSkinningParam( 0, 0, RTShaderSystem.SkinningType.Linear, false, false );
            }
            int boneCount = activeTechnique.BoneCount;
            int weightCount = activeTechnique.WeightCount;

            bool doBoneCalculations = isValid && ( boneCount != 0 ) && ( boneCount <= 256 ) && ( weightCount != 0 ) &&
                                      ( weightCount <= 4 ) &&
                                      ( ( creator == null ) || ( boneCount <= creator.MaxCalculableBoneCount ) );

            activeTechnique.DoBoneCalculations = doBoneCalculations;


            if ( ( doBoneCalculations ) && ( creator != null ) )
            {
                //update the receiver and caster materials
                if ( dstPass.Parent.ShadowCasterMaterial == null )
                {
                    Material mat = creator.GetCustomShadowCasterMaterial( skinningType, weightCount - 1 );
                    dstPass.Parent.SetShadowCasterMaterial( mat.Name );
                }

                if ( dstPass.Parent.ShadowReceiverMaterial == null )
                {
                    Material mat = creator.GetCustomShadowCasterMaterial( skinningType, weightCount - 1 );
                    dstPass.Parent.SetShadowReceiverMaterial( mat.Name );
                }
            }
            return true;
        }

        protected override bool ResolveParameters( ProgramSet programSet )
        {
            return activeTechnique.ResolveParameters( programSet );
        }

        protected override bool ResolveDependencies( ProgramSet programSet )
        {
            return activeTechnique.ResolveDependencies( programSet );
        }

        protected override bool AddFunctionInvocations( ProgramSet programSet )
        {
            return activeTechnique.AddFunctionInvocations( programSet );
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
                return activeTechnique.BoneCount;
            }
        }

        public int WeightCount
        {
            get
            {
                return activeTechnique.WeightCount;
            }
        }

        public SkinningType SkinningType
        {
            get
            {
                return skinningType;
            }
        }

        public bool HasCorrectAntipodalityHandling
        {
            get
            {
                return activeTechnique.HasCorrectAntipodalityHandling;
            }
        }

        public bool HasScalingShearingSupport
        {
            get
            {
                return activeTechnique.HasScalingShearingSupport;
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