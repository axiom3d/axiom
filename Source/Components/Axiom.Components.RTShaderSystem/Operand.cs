namespace Axiom.Components.RTShaderSystem
{
	/// <summary>
	///   Represents a function operand (its the combination of a parameter the in/out semantic and the used fields
	/// </summary>
	public class Operand
	{
		public enum OpSemantic
		{
			In,
			Out,
			InOut
		}

		//Used field mask
		public enum OpMask
		{
			All = 1 << 0,
			X = 1 << 1,
			Y = 1 << 2,
			Z = 1 << 3,
			W = 1 << 4
		}

		private readonly Parameter _parameter;
		private readonly OpSemantic semantic;
		private readonly int mask;
		private readonly int _indirectionalLevel;

		public Operand( Parameter parameter, OpSemantic opSemantic, int opMask, int indirectionalLevel )
		{
			this._parameter = parameter;
			this.semantic = opSemantic;
			this.mask = opMask;
			this._indirectionalLevel = indirectionalLevel;
		}

		public Operand( Operand operand )
			: this( operand._parameter, operand.semantic, operand.mask, operand._indirectionalLevel )
		{
		}

		public Parameter Parameter
		{
			get
			{
				return this._parameter;
			}
		}

		public bool HasFreeFields
		{
			get
			{
				return ( ( this.mask & (int)OpMask.All ) == 0 &&
				         ( ( this.mask & (int)OpMask.X ) == 1 || ( this.mask & (int)OpMask.Y ) == 1 ||
				           ( this.mask & (int)OpMask.Z ) == 1 || ( this.mask & (int)OpMask.W ) == 1 ) );
			}
		}

		public int Mask
		{
			get
			{
				return this.mask;
			}
		}

		public OpSemantic Semantic
		{
			get
			{
				return this.semantic;
			}
		}

		public ushort IndirectionLevel
		{
			get
			{
				return IndirectionLevel;
			}
		}

		public override string ToString()
		{
			string retVal = this._parameter.ToString();
			if ( ( this.mask & (int)OpMask.All ) == 1 ||
			     ( ( this.mask & (int)OpMask.X ) == 1 && ( this.mask & (int)OpMask.Z ) == 1 && ( this.mask & (int)OpMask.W ) == 1 ) )
			{
				return retVal;
			}

			retVal += "." + GetMaskAsString( this.mask );
			return retVal;
		}

		public static string GetMaskAsString( int mask )
		{
			string retVal = string.Empty;
			if ( ( mask & (int)OpMask.All ) == 0 )
			{
				if ( ( mask & (int)OpMask.X ) == 1 )
				{
					retVal += "x";
				}
				if ( ( mask & (int)OpMask.Y ) == 1 )
				{
					retVal += "y";
				}
				if ( ( mask & (int)OpMask.Z ) == 1 )
				{
					retVal += "z";
				}
				if ( ( mask & (int)OpMask.W ) == 1 )
				{
					retVal += "w";
				}
			}

			return retVal;
		}

		public static int GetFloatCount( int mask )
		{
			int floatCount = 0;
			while ( mask != 0 )
			{
				if ( ( mask & (int)OpMask.X ) != 0 )
				{
					floatCount++;
				}
				mask = mask >> 1;
			}

			return floatCount;
		}

		public static Axiom.Graphics.GpuProgramParameters.GpuConstantType GetGpuConstantType( int mask )
		{
			int floatCount = GetFloatCount( mask );
			Axiom.Graphics.GpuProgramParameters.GpuConstantType type;

			switch ( floatCount )
			{
				case 1:
					type = Graphics.GpuProgramParameters.GpuConstantType.Float1;
					break;
				case 2:
					type = Graphics.GpuProgramParameters.GpuConstantType.Float2;
					break;
				case 3:
					type = Graphics.GpuProgramParameters.GpuConstantType.Float3;
					break;
				case 4:
					type = Graphics.GpuProgramParameters.GpuConstantType.Float4;
					break;
				default:
					type = Graphics.GpuProgramParameters.GpuConstantType.Unknown;
					break;
			}
			return type;
		}
	}
}