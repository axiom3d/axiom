#region LGPL License
/*
Axiom Game Engine Library
Copyright (C) 2003  Axiom Project Team

The overall design, and a majority of the core engine and rendering code 
contained within this library is a derivative of the open source Object Oriented 
Graphics Engine OGRE, which can be found at http://ogre.sourceforge.net.  
Many thanks to the OGRE team for maintaining such a high quality project.

This library is free software; you can redistribute it and/or
modify it under the terms of the GNU Lesser General Public
License as published by the Free Software Foundation; either
version 2.1 of the License, or (at your option) any later version.

This library is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public
License along with this library; if not, write to the Free Software
Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
*/
#endregion

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
