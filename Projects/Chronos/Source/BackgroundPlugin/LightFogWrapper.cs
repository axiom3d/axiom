using System;
using System.Collections;
using Axiom.Core;
using Axiom.Graphics;
using System.Drawing;
using System.ComponentModel;
using System.Globalization;
using System.Drawing.Design;

namespace BackgroundPlugin
{
	/// <summary>
	/// Summary description for LightFogWrapper.
	/// </summary>
	public class LightFogWrapper
	{
		private SceneManager sm;
			
		[Category("Fog")]
		[Description("Set fog color")]
		public Color FogColor 
		{
			get
			{
				return sm.FogColor.ToColor();
			}
			set
			{
				sm.SetFog(sm.FogMode, ColorEx.FromColor(value), sm.FogDensity,
					sm.FogStart, sm.FogEnd);
			}
		}

		[Category("Fog")]
		[Description("The rate at which fog density increases under Exp and Exp2 modes. Recommended values are less than 0.01")]
		public float FogDensity 
		{
			get { return sm.FogDensity; }
			set 
			{
				sm.SetFog(sm.FogMode, sm.FogColor, value,
					sm.FogStart, sm.FogEnd);
			}
		}

		[Category("Fog")]
		[Description("Only applies to linear fog mode. Set how far from the camera (in units) the fog begins.")]
		public float FogStart 
		{
			get { return sm.FogStart; }
			set 
			{
				sm.SetFog(sm.FogMode, sm.FogColor, sm.FogDensity,
					value, sm.FogEnd);
			}
		}

		[Category("Fog")]
		[Description("Only applies to linear fog mode. Set how far from the camera (in units) the fog reaches full density.")]
		public float FogEnd 
		{
			get { return sm.FogEnd; }
			set 
			{
				sm.SetFog(sm.FogMode, sm.FogColor, sm.FogDensity,
					sm.FogStart, value);
			}
		}

		[Category("Fog")]
		[Description("Set the equation that determines how fog thickens.")]
		public Axiom.Graphics.FogMode FogMode 
		{
			get { return sm.FogMode ; }
			set 
			{
				sm.SetFog(value, sm.FogColor, sm.FogDensity,
					sm.FogStart, sm.FogEnd);
			}
		}

		public LightFogWrapper(SceneManager m) 
		{
			if(m == null)
				throw new System.Exception("LightFogWrapper cannot take a null scene manager");
			if(m.FogColor == null)
				m.SetFog(m.FogMode, ColorEx.FromColor(Color.White), m.FogDensity);
			sm = m;
		}

		[Category("Light")]
		[Description("Sets a global light color for the scene")]
		public Color LightColor 
		{
			get { return sm.AmbientLight.ToColor(); }
			set { sm.AmbientLight = ColorEx.FromColor(value); }
		}

		[Category("Shadows")]
		[Description("Set the technique that the scene uses to render shadows")]
		public ShadowTechnique ShadowTechnique 
		{
			get { return sm.ShadowTechnique; }
			set { sm.ShadowTechnique = value; }
		}

		[Category("Shadows")]
		[Description("Set shadow color")]
		public Color ShadowColor 
		{
			get
			{
				return sm.ShadowColor.ToColor();
			}
			set
			{
				sm.ShadowColor = ColorEx.FromColor(value);
			}
		}

		[Category("Shadows")]
		[Description("Toggle shadow volume debugging")]
		public bool ShowDebugShadows 
		{
			get
			{
				return sm.ShowDebugShadows;
			}
			set
			{
				sm.ShowDebugShadows = value;
			}
		}
	}
}
