namespace Axiom.Components.RTShaderSystem
{
	internal class HardwareSkinningTechnique
	{
		protected int boneCount, weightCount;
		protected bool correctAntipodalityHandling, scalingShearingSupport;

		protected bool doBoneCalculations;

		protected Parameter paramInPosition;
		protected Parameter paramInNormal;
		protected Parameter paramInBiNormal;
		protected Parameter paramInTangent;
		protected Parameter paramInIndices;
		protected Parameter paramInWeights;
		protected UniformParameter paramInWorldMatrices;
		protected UniformParameter paramInInvWorldMatrix;
		protected UniformParameter paramInViewProjMatrix;
		protected UniformParameter paramInWorldMatrix;
		protected UniformParameter paramInWorldViewProjMatrix;

		protected Parameter paramTempFloat4, paramTempFloat3;
		protected Parameter paramLocalPositionWorld;
		protected Parameter paramLocalNormalWorld;
		protected Parameter paramLocalTangentWorld;
		protected Parameter paramLocalBiNormalWorld;
		protected Parameter paramOutPositionProj;

		public HardwareSkinningTechnique()
		{
			boneCount = 0;
			weightCount = 0;
			correctAntipodalityHandling = false;
			scalingShearingSupport = false;
			doBoneCalculations = false;
		}

		internal void SetHardwareSkinningParam( int boneCount, int weightCount, bool correctAntipodalityHandling,
		                                        bool scalingShearingSupport )
		{
			this.boneCount = boneCount;
			this.weightCount = weightCount;
			this.correctAntipodalityHandling = correctAntipodalityHandling;
			this.scalingShearingSupport = scalingShearingSupport;
		}

		public int BoneCount
		{
			get
			{
				return boneCount;
			}
		}

		public int WeightCount
		{
			get
			{
				return weightCount;
			}
		}

		public bool HasCorrectAntipodalityHandling
		{
			get
			{
				return correctAntipodalityHandling;
			}
		}

		public bool HasScalingShearingSupport
		{
			get
			{
				return scalingShearingSupport;
			}
		}

		public bool DoBoneCalculations
		{
			get
			{
				return doBoneCalculations;
			}
			set
			{
				doBoneCalculations = value;
			}
		}

		internal virtual bool ResolveParameters( ProgramSet programSet )
		{
			return false;
		}

		internal virtual bool ResolveDependencies( ProgramSet programSet )
		{
			return false;
		}

		internal virtual bool AddFunctionInvocations( ProgramSet programSet )
		{
			return false;
		}

		protected Operand.OpMask IndexToMask( int index )
		{
			switch ( index )
			{
				case 0:
					return Operand.OpMask.X;
				case 1:
					return Operand.OpMask.Y;
				case 2:
					return Operand.OpMask.Z;
				case 3:
					return Operand.OpMask.W;
				default:
					throw new Axiom.Core.AxiomException( "Illegal Value" );
			}
		}
	}
}