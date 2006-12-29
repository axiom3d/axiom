using System;
using Axiom.Core;
using Axiom.Graphics;
using System.Drawing;
using System.ComponentModel;
using System.Globalization;
using System.Drawing.Design;
using Chronos.Core;

namespace LightPlugin
{
	/// <summary>
	/// Summary description for LightWrappers.
	/// </summary>
	/// 

	public class LightWrapper : IPropertiesWrapper 
	{
		[TypeConverter(typeof(ExpandableObjectConverter))]
		public struct attList 
		{
			private float ar, ac, al, aq;
			public float AttenuationRange { get { return ar;} set { ar = value;} }
			public float AttenuationConstant { get { return ac;} set { ac = value;} }
			public float AttenuationLinear { get { return al;} set { al = value;} }
			public float AttenuationQuadratic { get { return aq;} set { aq = value;} }
		}

		[TypeConverter(typeof(ExpandableObjectConverter))]
		public struct spotList
		{
			private float ia, oa, fo;
			public float InnerAngle { get { return ia;} set { ia = value;} }
			public float OuterAngle { get { return oa;} set { oa = value;} }
			public float Falloff { get { return fo;} set { fo = value;} }
		}

		private Light mLight;
		private attList attenuation = new attList();
		private spotList spotlight = new spotList();
		
		public LightWrapper()
		{
			mLight = new Light();
			construct();
		}

		public LightWrapper(Light l) 
		{
			mLight = l;
			construct();
		}

		private void construct() 
		{
			attenuation.AttenuationRange = mLight.AttenuationRange;
			attenuation.AttenuationConstant = mLight.AttenuationConstant;
			attenuation.AttenuationLinear = mLight.AttenuationLinear;
			attenuation.AttenuationQuadratic = mLight.AttenuationQuadratic;
			spotlight.InnerAngle = mLight.SpotlightInnerAngle;
			spotlight.OuterAngle = mLight.SpotlightOuterAngle;
			spotlight.Falloff = mLight.SpotlightFalloff;
		}

		public attList Attenuation
		{
			get { return attenuation; }
			set 
			{
				mLight.SetAttenuation(attenuation.AttenuationRange,
					attenuation.AttenuationConstant,
					attenuation.AttenuationLinear,
					attenuation.AttenuationQuadratic);
			}
		}

		public spotList Spotlight
		{
			get { return spotlight; }
			set 
			{
				mLight.SetSpotlightRange(spotlight.InnerAngle, spotlight.OuterAngle,
					  spotlight.Falloff);
			}
		}

		public Color Specular 
		{
			get {
				return mLight.Specular.ToColor();
			}
			set {
				mLight.Specular = ColorEx.FromColor(value);
			}
		}

		public Color Diffuse 
		{
			get 
			{
				return mLight.Diffuse.ToColor();
			}
			set 
			{
				mLight.Diffuse = ColorEx.FromColor(value);
			}
		}

		public bool CastShadows 
		{
			get { return mLight.CastShadows; }
			set { mLight.CastShadows = value; }
		}

		public bool IsVisible
		{
			get { return mLight.IsVisible; }
			set { mLight.IsVisible = value; }
		}

		public Light getLight() 
		{
			return this.mLight;
		}

		public LightType Type 
		{
			get { return mLight.Type; }
			set { mLight.Type = value; }
		}

		public void Dispose() {
		}

		public TD.SandBar.ToolBar GetContextualToolBar() {
			return null;
		}
	}
}
