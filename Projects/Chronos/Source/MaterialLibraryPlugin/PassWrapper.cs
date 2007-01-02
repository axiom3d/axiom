using System;
using System.Windows.Forms;
using Axiom.Graphics;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Windows.Forms.Design;
using System.Drawing.Drawing2D;
using System.Drawing.Design;

namespace MaterialLibraryPlugin
{
	public enum SceneBlendTypeList {
		Basic, Complex
	}

	[TypeConverter(typeof(ExpandableObjectConverter))]
	public class SceneBlendBase {
		protected Pass p;

		public SceneBlendBase() {}
		public SceneBlendBase(Pass pass) {
			p = pass;
		}
	}


	public class SceneBlendSingle : SceneBlendBase {
		
		public SceneBlendSingle() {}

		public SceneBlendSingle(Pass pass) {
			p = pass;
		}

		public SceneBlendType SceneBlendType {
			get { 
				if (p.SourceBlendFactor == SceneBlendFactor.One &&
					p.DestBlendFactor == SceneBlendFactor.One)
					return SceneBlendType.Add;
				else if (p.SourceBlendFactor == SceneBlendFactor.SourceColor &&
					p.DestBlendFactor == SceneBlendFactor.OneMinusSourceColor)
					return SceneBlendType.TransparentColor;
				else
					return SceneBlendType.TransparentAlpha;
			}
			set {
				p.SetSceneBlending(value);
			}
		}
	}

	public class SceneBlendCombo : SceneBlendBase {

		public SceneBlendCombo() {}

		public SceneBlendCombo(Pass pass) {
			p = pass;
		}

		public SceneBlendFactor Source {
			get { return p.SourceBlendFactor; }
			set { p.SetSceneBlending(value, p.DestBlendFactor); }
		}

		public SceneBlendFactor Destination {
			get { return p.DestBlendFactor; }
			set { p.SetSceneBlending(p.SourceBlendFactor, value); }
		}
	}

	[TypeConverter(typeof(ExpandableObjectConverter))]
	public class IterationWrapper {
		private Pass p;
		private bool restrictLighting;
		public enum IterTypes { Once, OncePerLight }
		public enum LightTypes { All, Point, Spotlight, Directional }
		public IterationWrapper(Pass pass) { p = pass; }

		private LightType getLightType(LightTypes l) {
			if(l == LightTypes.Directional)
				return LightType.Directional;
			else if(l == LightTypes.Point)
				return LightType.Point;
			else
				return LightType.Spotlight;
		}

		[Description("By default, passes are only issued once. However, if you use the programmable pipeline, or you wish to exceed the normal limits on the number of lights which are supported, you might want to use the OncePerLight option. In this case, only light index 0 is ever used, and the pass is issued multiple times, each time with a different light in light index 0. Clearly this will make the pass more expensive, but it may be the only way to achieve certain effects such as per-pixel lighting effects which take into account 1..n lights.\n\nIf you use OncePerLight, you should also add an ambient pass to the technique before this pass, otherwise when no lights are in range of this object it will not get rendered at all; this is important even when you have no ambient light in the scene, because you would still want the objects sihouette to appear.")]
		public IterTypes Type {
			get {
				if(p.RunOncePerLight) return IterTypes.OncePerLight;
				else return IterTypes.Once;
			}
			set {
				if (value == IterTypes.OncePerLight) {
					p.SetRunOncePerLight(true, p.RunOnlyOncePerLightType, p.OnlyLightType);
				} else {
					p.SetRunOncePerLight(false, p.RunOnlyOncePerLightType, p.OnlyLightType);
				}
			}
		}

		[Description("This attribute only applies if you use OncePerLight, and restricts the pass to being run for lights of a single type (either 'point', 'directional' or 'spot'). This can be useful because when you;re writing a vertex / fragment program it is a lot better if you can assume the kind of lights you'll be dealing with.")]
		public LightTypes RunOnlyForLightType {
			get { 
				if(restrictLighting) {
					if(p.OnlyLightType == LightType.Directional)
						return LightTypes.Directional;
					else if(p.OnlyLightType == LightType.Point)
						return LightTypes.Point;
					else if(p.OnlyLightType == LightType.Spotlight)
						return LightTypes.Spotlight;
				}
				return LightTypes.All;
			}
			set {
				if(value == LightTypes.All) {
					restrictLighting = false;
					p.SetRunOncePerLight(p.RunOncePerLight, false);
				} else {
					restrictLighting = true;
					p.SetRunOncePerLight(p.RunOncePerLight, true, getLightType(value));
				} 
			}
		}
	}

	public class TrackBarValueEditor : System.Drawing.Design.UITypeEditor {
		private IWindowsFormsEditorService edSvc = null;
		private TrackBar trackBar;
		public TrackBarValueEditor() {
			trackBar = new TrackBar();
			trackBar.Value = 0;
		}
		public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value) {
			if (context != null
				&& context.Instance != null
				&& provider != null) {

				edSvc = (IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService));

				if (edSvc != null) {
					TrackBarPropsAttribute attribs = context.PropertyDescriptor.Attributes[typeof(TrackBarPropsAttribute)] as TrackBarPropsAttribute;
					if(attribs != null) {
						trackBar.Minimum = attribs.Min;
						trackBar.Maximum = attribs.Max;
						trackBar.LargeChange = attribs.LargeChange;
						trackBar.SmallChange = attribs.SmallChange;
					}

					edSvc.DropDownControl(trackBar);
					value = trackBar.Value;
				}
			}
			return value;
		}

		public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context) {
			if (context != null && context.Instance != null) {
				return UITypeEditorEditStyle.DropDown;
			}
			return base.GetEditStyle(context);
		}

		private void ValueChanged(object sender, EventArgs e) {
			if (edSvc != null) {
				edSvc.CloseDropDown();
			}
		}
	}

	public class ShaderSelector : System.Drawing.Design.UITypeEditor {
		private ShaderEditor editor;
		private IWindowsFormsEditorService edSvc = null;

		public ShaderSelector() {
			editor = new ShaderEditor();
		}

		public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value) {
			if (context != null
				&& context.Instance != null
				&& provider != null) {

				edSvc = (IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService));

				if (edSvc != null) {
					ShaderEditorAttribute attribs = context.PropertyDescriptor.Attributes[typeof(ShaderEditorAttribute)] as ShaderEditorAttribute;
					if(attribs != null) {
						editor.SetProgramType(attribs.Type);
						editor.SetProgram((string)value);
					}
					editor.Pass = (context.Instance as PassWrapper).Pass;
					edSvc.ShowDialog(editor);
					//value = textureList.SelectedTexture;
				}
			}
			return value;
		}

		public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context) {
			if (context != null && context.Instance != null) {
				return UITypeEditorEditStyle.DropDown;
			}
			return base.GetEditStyle(context);
		}

		private void ValueChanged(object sender, EventArgs e) {
			if (edSvc != null) {
				edSvc.CloseDropDown();
			}
		}
	}

	public class TrackBarPropsAttribute : Attribute {
		private int min, max, large, small;
		public TrackBarPropsAttribute(int Min, int Max, int LargeChange, int SmallChange) {
			min = Min;
			max = Max;
			large = LargeChange;
			small = SmallChange;
		}

		public int Min { get { return min; } }
		public int Max { get { return max; } }
		public int LargeChange { get { return large; } }
		public int SmallChange { get { return small; } }
	}

	public class ShaderEditorAttribute : Attribute {
		private GpuProgramType type;
		public ShaderEditorAttribute(GpuProgramType Type) {
			type = Type;
		}

		public GpuProgramType Type { get { return type; } }
	}

	public class PassWrapper {
		private Pass p;
		private SceneBlendTypeList blendType;
		private IterationWrapper iter;
		private TrackBar depthBias;

		public PassWrapper(Pass pass) {
			p = pass;
			depthBias = new TrackBar();
			depthBias.Minimum = 0;
			depthBias.Maximum = 16;
			depthBias.Value = p.DepthBias;
		}

		public Pass Pass {
			get { return p; }
		}

		[Category("Colors")]
		[Description("Sets the ambient colour reflectance properties of this pass. This attribute has no effect if a vertex program is used.\n\nThe base colour of a pass is determined by how much red, green and blue light is reflects at each vertex. This property determines how much ambient light (directionless global light) is reflected. The default is full white, meaning objects are completely globally illuminated. Reduce this if you want to see diffuse or specular light effects, or change the blend of colours to make the object have a base colour other than white. This setting has no effect if dynamic lighting is disabled using the 'lighting off' attribute, or if any texture layer has a 'colour_op replace' attribute.")]
		public Color Ambient {
			get { return p.Ambient.ToColor(); }
			set { p.Ambient = Axiom.Core.ColorEx.FromColor(value); }
		}

		[Category("Colors")]
		[Description("Sets the diffuse colour reflectance properties of this pass. This attribute has no effect if a vertex program is used.\n\nThe base colour of a pass is determined by how much red, green and blue light is reflects at each vertex. This property determines how much diffuse light (light from instances of the Light class in the scene) is reflected. The default is full white, meaning objects reflect the maximum white light they can from Light objects. This setting has no effect if dynamic lighting is disabled using the 'lighting off' attribute, or if any texture layer has a 'colour_op replace' attribute.")]
		public Color Diffuse {
			get { return p.Diffuse.ToColor(); }
			set { p.Diffuse = Axiom.Core.ColorEx.FromColor(value); }
		}

		[Category("Colors")]
		[Description("Sets the specular colour reflectance properties of this pass. This attribute has no effect if a vertex program is used.\n\nThe base colour of a pass is determined by how much red, green and blue light is reflects at each vertex. This property determines how much specular light (highlights from instances of the Light class in the scene) is reflected. The default is to reflect no specular light. The colour of the specular highlights is determined by the colour parameters, and the size of the highlights by the separate shininess parameter. This setting has no effect if dynamic lighting is disabled using the 'lighting off' attribute, or if any texture layer has a 'colour_op replace' attribute.")]
		public Color Specular {
			get { return p.Specular.ToColor(); }
			set { p.Specular = Axiom.Core.ColorEx.FromColor(value); }
		}

		[Category("Colors")]
		[Description("Sets the amount of self-illumination an object has. This attribute has no effect if a vertex program is used.\n\nIf an object is self-illuminating, it does not need external sources to light it, ambient or otherwise. It's like the object has it's own personal ambient light. Unlike the name suggests, this object doesn't act as a light source for other objects in the scene (if you want it to, you have to create a light which is centered on the object). This setting has no effect if dynamic lighting is disabled using the 'lighting off' attribute, or if any texture layer has a 'colour_op replace' attribute.")]
		public Color Emissive {
			get { return p.Emissive.ToColor(); }
			set { p.Emissive = Axiom.Core.ColorEx.FromColor(value); }
		}

		[Category("Colors")]
		[Description("Sets whether or not this pass renders with colour writing on or not.\n\nIf colour writing is off no visible pixels are written to the screen during this pass. You might think this is useless, but if you render with colour writing off, and with very minimal other settings, you can use this pass to initialise the depth buffer before subsequently rendering other passes which fill in the colour data. This can give you significant performance boosts on some newer cards, especially when using complex fragment programs, because if the depth check fails then the fragment program is never run.")]
		public bool ColorWrite {
			get { return p.ColorWrite; }
			set { p.ColorWrite = value; }
		}

		[Category("Scene Blending")]
		[Description("Sets the kind of blending this pass has with the existing contents of the scene. Wheras the texture blending operations seen in the texture_unit entries are concerned with blending between texture layers, this blending is about combining the output of this pass as a whole with the existing contents of the rendering target. This blending therefore allows object transparency and other special effects. There are 2 formats, one using predefined blend types, the other allowing a roll-your-own approach using source and destination factors.")]
		public SceneBlendBase SceneBlending {
			get {
				if (this.SceneBlendingType == SceneBlendTypeList.Basic)
					return new SceneBlendSingle(p);
				else
					return new SceneBlendCombo(p);
			}
		}

		[Category("Scene Blending")]
		[Description("Sets the kind of blending this pass has with the existing contents of the scene. Wheras the texture blending operations seen in the texture_unit entries are concerned with blending between texture layers, this blending is about combining the output of this pass as a whole with the existing contents of the rendering target. This blending therefore allows object transparency and other special effects. There are 2 formats, one using predefined blend types, the other allowing a roll-your-own approach using source and destination factors. Use 'Basic' for predefined types, and 'Complex' to roll your own type.")]
		public SceneBlendTypeList SceneBlendingType {
			get { return blendType; }
			set {
				blendType = value;
			}
		}

		[Category("Depth Properties")]
		[Description("Sets whether or not this pass renders with depth-buffer checking on or not.\n\nIf depth-buffer checking is on, whenever a pixel is about to be written to the frame buffer the depth buffer is checked to see if the pixel is in front of all other pixels written at that point. If not, the pixel is not written. If depth checking is off, pixels are written no matter what has been rendered before. Also see depth_func for more advanced depth check configuration.")]
		public bool DepthCheck {
			get { return p.DepthCheck; }
			set { p.DepthCheck = value; }
		}

		[Category("Depth Properties")]
		[Description("Sets whether or not this pass renders with depth-buffer writing on or not.\n\nIf depth-buffer writing is on, whenever a pixel is written to the frame buffer the depth buffer is updated with the depth value of that new pixel, thus affecting future rendering operations if future pixels are behind this one. If depth writing is off, pixels are written without updating the depth buffer. Depth writing should normally be on but can be turned off when rendering static backgrounds or when rendering a collection of transparent objects at the end of a scene so that they overlap each other correctly.")]
		public bool DepthWrite {
			get { return p.DepthWrite; }
			set { p.DepthWrite = value; }
		}

		[Category("Depth Properties")]
		[Description("Sets the function used to compare depth values when depth checking is on.\n\nIf depth checking is enabled (see depth_check) a comparison occurs between the depth value of the pixel to be written and the current contents of the buffer. This comparison is normally less_equal, i.e. the pixel is written if it is closer (or at the same distance) than the current contents.")]
		public CompareFunction DepthFunction {
			get { return p.DepthFunction; }
			set { p.DepthFunction = value; }
		}

		[Category("Depth Properties")]
		[Description("Sets the bias applied to the depth value of this pass. Can be used to make coplanar polygons appear on top of others e.g. for decals. Value must be between 0.., the default being 0. The higher the value, the greater the offset (for if you want to do multiple overlapping decals).")]
		[Editor(typeof(TrackBarValueEditor), typeof(UITypeEditor))] 
		[TrackBarProps(0, 16, 1, 3)]
		public int DepthBias {
			get { depthBias.Value = p.DepthBias; return depthBias.Value; }
			set { depthBias.Value = value; p.DepthBias = depthBias.Value; }
		}

		[Category("Culling")]
		[Description("Sets the hardware culling mode for this pass.\n\nA typical way for the hardware rendering Root to cull triangles is based on the 'vertex winding' of triangles. Vertex winding refers to the direction in which the vertices are passed or indexed to in the rendering operation as viewed from the camera, and will wither be clockwise or anticlockwise (that's 'counterclockwise' for you Americans out there ;). If the option 'cull_hardware clockwise' is set, all triangles whose vertices are viewed in clockwise order from the camera will be culled by the hardware. 'anticlockwise' is the reverse (obviously), and 'none' turns off hardware culling so all triagles are rendered (useful for creating 2-sided passes).")]
		public CullingMode HardwareCulling {
			get { return p.CullMode; }
			set { p.CullMode = value; }
		}

		[Category("Culling")]
		[Description("Sets the software culling mode for this pass.\n\nIn some situations the Root will also cull geometry in software before sending it to the hardware renderer. This setting only takes effect on SceneManager's that use it (since it is best used on large groups of planar world geometry rather than on movable geometry since this would be expensive), but if used can cull geometry before it is sent to the hardware. In this case the culling is based on whether the 'back' or 'front' of the traingle is facing the camera - this definition is based on the face normal (a vector which sticks out of the front side of the polygon perpendicular to the face). Since Ogre expects face normals to be on anticlockwise side of the face, 'cull_software back' is the software equivalent of 'cull_hardware clockwise' setting, which is why they are both the default. The naming is different to reflect the way the culling is done though, since most of the time face normals are precalculated and they don't have to be the way Ogre expects - you could set 'cull_hardware none' and completely cull in software based on your own face normals, if you have the right SceneManager which uses them.")]
		public ManualCullingMode SoftwareCulling {
			get { return p.ManualCullMode; }
			set { p.ManualCullMode = value; }
		}

		[Category("Lighting")]
		[Description("Sets whether or not dynamic lighting is turned on for this pass or not. If lighting is turned off, all objects rendered using the pass will be fully lit. This attribute has no effect if a vertex program is used.\n\nTurning dynamic lighting off makes any ambient, diffuse, specular, emissive and shading properties for this pass redundant. When lighting is turned on, objects are lit according to their vertex normals for diffuse and specular light, and globally for ambient and emissive.")]
		public bool LightingEnabled {
			get { return p.LightingEnabled; }
			set { p.LightingEnabled = value; }
		}

		[Category("Lighting")]
		[Description("Sets the kind of shading which should be used for representing dynamic lighting for this pass.\n\nWhen dynamic lighting is turned on, the effect is to generate colour values at each vertex. Whether these values are interpolated across the face (and how) depends on this setting.")]
		public Shading Shading {
			get { return p.ShadingMode; }
			set { p.ShadingMode = value; }
		}

		[Category("Lighting")]
		[Description("Sets whether or not this pass is iterated, ie issued more than once.\n\nBy default, passes are only issued once. However, if you use the programmable pipeline, or you wish to exceed the normal limits on the number of lights which are supported, you might want to use the OncePerLight option. In this case, only light index 0 is ever used, and the pass is issued multiple times, each time with a different light in light index 0. Clearly this will make the pass more expensive, but it may be the only way to achieve certain effects such as per-pixel lighting effects which take into account 1..n lights.\n\nIf you use OncePerLight, you should also add an ambient pass to the technique before this pass, otherwise when no lights are in range of this object it will not get rendered at all; this is important even when you have no ambient light in the scene, because you would still want the objects sihouette to appear.\n\nThe second parameter to the attribute only applies if you use OncePerLight, and restricts the pass to being run for lights of a single type (either 'point', 'directional' or 'spot'). In the example, the pass will be run once per point light. This can be useful because when you;re writing a vertex / fragment program it is a lot better if you can assume the kind of lights you'll be dealing with.")]
		public IterationWrapper Iteration {
			get {
				if(iter == null) iter = new IterationWrapper(p);
				return iter;
			}
			set { iter = value; }
		}

		[Category("Lighting")]
		[Description("Sets the maximum number of lights which will be considered for use with this pass.\n\nThe maximum number of lights which can be used when rendering fixed-function materials is set by the rendering system, and is typically set at 8. When you are using the programmable pipeline this limit is dependent on the program you are running, or, if you use 'iteration once_per_light', it effectively only bounded by the number of passes you are willing to use. Whichever method you use, however, the max_lights limit applies.")]
		public int MaxLights {
			get { return p.MaxLights; }
			set { p.MaxLights = value; }
		}

		[Category("Fog Settings")]
		[Description("Tells the pass whether it should override the scene fog settings, and enforce it's own. Very useful for things that you don't want to be affected by fog when the rest of the scene is fogged, or vice versa.\n\n")]
		public bool FogOverride {
			get { return p.FogOverride; }
			set { 
				p.SetFog(value, p.FogMode, p.FogColor, p.FogDensity, p.FogStart, p.FogEnd);
			}
		}
		
		[Category("Fog Settings")]
		[Description("Sets the override fog color.")]
		public Color FogColor {
			get { return p.FogColor.ToColor(); }
			set { 
				p.SetFog(p.FogOverride, p.FogMode, Axiom.Core.ColorEx.FromColor(value), p.FogDensity, p.FogStart, p.FogEnd);
			}
		}

		[Category("Fog Settings")]
		[Description("The density parameter used in the 'exp' or 'exp2' fog types.")]
		public float FogDensity {
			get { return p.FogDensity; }
			set { 
				p.SetFog(p.FogOverride, p.FogMode, p.FogColor, value, p.FogStart, p.FogEnd);
			}
		}

		[Category("Fog Settings")]
		[Description("The start distance from the camera of linear fog.")]
		public float FogStart {
			get { return p.FogStart; }
			set { 
				p.SetFog(p.FogOverride, p.FogMode, p.FogColor, p.FogDensity, value, p.FogEnd);
			}
		}

		[Category("Fog Settings")]
		[Description("The end distance from the camera of linear fog.")]
		public float FogEnd {
			get { return p.FogEnd; }
			set { 
				p.SetFog(p.FogOverride, p.FogMode, p.FogColor, p.FogDensity, p.FogStart, value);
			}
		}

		[Editor(typeof(ShaderSelector), typeof(UITypeEditor))] 
		[ShaderEditor(GpuProgramType.Vertex)]
		public string VertexProgram {
			get { return p.VertexProgramName;}
			set { p.VertexProgramName = value; }
		}

		[Editor(typeof(ShaderSelector), typeof(UITypeEditor))] 
		[ShaderEditor(GpuProgramType.Fragment)]
		public string FragmentProgram {
			get { return p.FragmentProgramName;}
			set { p.FragmentProgramName = value; }
		}
	}
}
