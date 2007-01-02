using System;
using System.Collections;
using Tao.Cg;

// Broken - don't currently use.

namespace MaterialLibraryPlugin {
	/// <summary>
	/// Summary description for CgConsts.
	/// </summary>
	public class CgConsts {
		public static int[][] ParamIndexes = {
			new int[] {
						Cg.CG_HALF,
						Cg.CG_HALF1,	Cg.CG_HALF2,	Cg.CG_HALF3,	Cg.CG_HALF4,
						Cg.CG_HALF1x1,	Cg.CG_HALF1x2,	Cg.CG_HALF1x3,	Cg.CG_HALF1x4, 
						Cg.CG_HALF2x1,	Cg.CG_HALF2x2,	Cg.CG_HALF2x3,	Cg.CG_HALF2x4,
						Cg.CG_HALF3x1,	Cg.CG_HALF3x2,	Cg.CG_HALF3x3,	Cg.CG_HALF3x4,
						Cg.CG_HALF4x1,	Cg.CG_HALF4x2,	Cg.CG_HALF4x3,	Cg.CG_HALF4x4,
					},
			new int[] {
						Cg.CG_FLOAT,
  					    Cg.CG_FLOAT1,	Cg.CG_FLOAT2,	Cg.CG_FLOAT3,	Cg.CG_FLOAT4,
					    Cg.CG_FLOAT1x1,	Cg.CG_FLOAT1x2,	Cg.CG_FLOAT1x3,	Cg.CG_FLOAT1x4, 
						Cg.CG_FLOAT2x1,	Cg.CG_FLOAT2x2,	Cg.CG_FLOAT2x3,	Cg.CG_FLOAT2x4,
  					    Cg.CG_FLOAT3x1,	Cg.CG_FLOAT3x2,	Cg.CG_FLOAT3x3,	Cg.CG_FLOAT3x4,
						Cg.CG_FLOAT4x1,	Cg.CG_FLOAT4x2,	Cg.CG_FLOAT4x3,	Cg.CG_FLOAT4x4,
					},
			new int[] {
						Cg.CG_FIXED,
						Cg.CG_FIXED1,	Cg.CG_FIXED2,	Cg.CG_FIXED3,	Cg.CG_FIXED4,
						Cg.CG_FIXED1x1,	Cg.CG_FIXED1x2,	Cg.CG_FIXED1x3,	Cg.CG_FIXED1x4, 
						Cg.CG_FIXED2x1,	Cg.CG_FIXED2x2,	Cg.CG_FIXED2x3,	Cg.CG_FIXED2x4,
						Cg.CG_FIXED3x1,	Cg.CG_FIXED3x2,	Cg.CG_FIXED3x3,	Cg.CG_FIXED3x4,
						Cg.CG_FIXED4x1,	Cg.CG_FIXED4x2,	Cg.CG_FIXED4x3,	Cg.CG_FIXED4x4
				  },
			new int[] {
						Cg.CG_INT,
						Cg.CG_INT1,		Cg.CG_INT2,		Cg.CG_INT3,		Cg.CG_INT4,
						Cg.CG_INT1x1,	Cg.CG_INT1x2,	Cg.CG_INT1x3,	Cg.CG_INT1x4, 
						Cg.CG_INT2x1,	Cg.CG_INT2x2,	Cg.CG_INT2x3,	Cg.CG_INT2x4,
						Cg.CG_INT3x1,	Cg.CG_INT3x2,	Cg.CG_INT3x3,	Cg.CG_INT3x4,
						Cg.CG_INT4x1,	Cg.CG_INT4x2,	Cg.CG_INT4x3,	Cg.CG_INT4x4,
					 },
			new int[] {
						Cg.CG_BOOL,
						Cg.CG_BOOL1,	Cg.CG_BOOL2,	Cg.CG_BOOL3,	Cg.CG_BOOL4,
						Cg.CG_BOOL1x1,	Cg.CG_BOOL1x2,	Cg.CG_BOOL1x3,	Cg.CG_BOOL1x4,
						Cg.CG_BOOL2x1,	Cg.CG_BOOL2x2,	Cg.CG_BOOL2x3,	Cg.CG_BOOL2x4,
						Cg.CG_BOOL3x1,	Cg.CG_BOOL3x2,	Cg.CG_BOOL3x3,	Cg.CG_BOOL3x4,
						Cg.CG_BOOL4x1,	Cg.CG_BOOL4x2,	Cg.CG_BOOL4x3,	Cg.CG_BOOL4x4
					}
		};

		public static int GetSizeForIndex(int index) {
			if(index <= 4) return 1;
			if(index % 4 == 0)
				return (index - 4) / 4;
			else {
				double val = (index / 4) * (index % 4) / 4;
				return (int)System.Math.Round(val);
			}
		}

		public static string GetTypeForIndex(int index, int subindex) {
			string[] types = {"HALF","FLOAT","FIXED","INT","BOOL"};
			if(subindex > 0) {
				if(subindex <= 4)
					return types[index] + subindex.ToString();
				else {
					int pt1, pt2;
					if(subindex % 4 == 0) {
						pt1 = (subindex-4)/4;
						pt2 = 4;
					}
					else {
						pt1 = subindex/4;
						pt2 = subindex % 4;
					}
					return types[index] + pt1.ToString() + "x" + pt2.ToString();
				}
			} else {
				return types[index];
			}
		}

		public static System.Type GetTypeForIndex(int index) {
			switch(index) {
				case 0:
					return typeof(float);
				case 1:
					return typeof(float);
				case 2:
					return typeof(float);
				case 3:
					return typeof(int);
				case 4:
					return typeof(bool);
				default:
					return null;
			}
		}
	}
}