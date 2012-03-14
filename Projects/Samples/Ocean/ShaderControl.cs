namespace Axiom.Samples.Ocean
{
	public enum ShaderType
	{
		GpuVertex,
		GpuFragment,
		MatSpecular,
		MatDiffuse,
		MatAmbient,
		MatShininess,
		MatEmissive
	}

	public struct ShaderControl
	{
		public int ElementIndex;
		public float MaxVal;
		public float MinVal;
		public string Name;
		public string ParamName;
		public int PhysicalIndex;
		public ShaderType Type;

		/// <summary>
		/// 
		/// </summary>
		public float Range
		{
			get
			{
				return this.MaxVal - this.MinVal;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="val"></param>
		/// <returns></returns>
		public float ConvertParamToScrollPosition( float val )
		{
			return val - this.MinVal;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="val"></param>
		/// <returns></returns>
		public float ConvertScrollPositionToParam( float val )
		{
			return val + this.MinVal;
		}
	}
}
