using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
		public string Name;
		public string ParamName;
		public ShaderType Type;
		public float MinVal;
		public float MaxVal;
		public int PhysicalIndex;
		public int ElementIndex;

		/// <summary>
		/// 
		/// </summary>
		public float Range
		{
			get
			{
				return MaxVal - MinVal;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="val"></param>
		/// <returns></returns>
		public float ConvertParamToScrollPosition( float val )
		{
			return val - MinVal;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="val"></param>
		/// <returns></returns>
		public float ConvertScrollPositionToParam( float val )
		{
			return val + MinVal;
		}
	}
}
