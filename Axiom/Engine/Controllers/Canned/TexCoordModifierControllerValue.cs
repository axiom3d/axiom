using System;
using Axiom.Core;
using Axiom.MathLib;

namespace Axiom.Controllers.Canned
{
	/// <summary>
	/// Summary description for TexCoordModifierControllerValue.
	/// </summary>
	public class TexCoordModifierControllerValue : IControllerValue
	{
		#region Member variables
		
		protected bool transU;
		protected bool transV;
		protected bool scaleU;
		protected bool scaleV;
		protected bool rotate;
		protected TextureLayer texLayer;
		
		#endregion

		public TexCoordModifierControllerValue(TextureLayer layer)
		{
			this.texLayer = layer;
		}

		public TexCoordModifierControllerValue(TextureLayer layer, bool scrollU, bool scrollV)
		{
			this.texLayer = layer;
			this.transU = scrollU;
			this.transV = scrollV;
		}

		public TexCoordModifierControllerValue(TextureLayer layer, bool scrollU, bool scrollV, bool scaleU, bool scaleV, bool rotate)
		{
			this.texLayer = layer;
			this.transU = scrollU;
			this.transV = scrollV;
			this.scaleU = scaleU;
			this.scaleV = scaleV;
			this.rotate = rotate;
		}

		#region IControllerValue Members

		public float Value
		{
			get
			{
				Matrix4 trans = texLayer.TextureMatrix;

				if(transU)
					return trans.m03;
				else if(transV)
					return trans.m13;
				else if(scaleU)
					return trans.m00;
				else if(scaleV)
					return trans.m11;

				// should never get here
				return 0.0f;
			}
			set
			{
				if(transU)
					texLayer.SetTextureScrollU(value);

				if(transV)
					texLayer.SetTextureScrollV(value);

				if(scaleU)
				{
					if(value >= 0)
					{
						texLayer.SetTextureScaleU(1 + value);
					}
					else
					{
						texLayer.SetTextureScaleU(1 / -value);
					}
				}

				if(scaleV)
				{
					if(value >= 0)
					{
						texLayer.SetTextureScaleV(1 + value);
					}
					else
					{
						texLayer.SetTextureScaleV(1 / -value);
					}
				}

				if(rotate)
					texLayer.SetTextureRotate(value * 360);
			}
		}

		#endregion
	}
}
