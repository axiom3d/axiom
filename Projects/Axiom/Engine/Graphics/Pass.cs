#region LGPL License
/*
Axiom Graphics Engine Library
Copyright © 2003-2011 Axiom Project Team

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

#region SVN Version Information
// <file>
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections;
using System.Diagnostics;

using Axiom.Configuration;
using Axiom.Core;
using System.Collections.Generic;
using Axiom.Graphics.Collections;

#endregion Namespace Declarations

namespace Axiom.Graphics
{
	/// <summary>
	/// 	Class defining a single pass of a Technique (of a Material), ie
	///    a single rendering call. 
	/// </summary>
	/// <remarks>
	///    Rendering can be repeated with many passes for more complex effects.
	///    Each pass is either a fixed-function pass (meaning it does not use
	///    a vertex or fragment program) or a programmable pass (meaning it does
	///    use either a vertex or a fragment program, or both). 
	///    <p/>
	///    Programmable passes are complex to define, because they require custom
	///    programs and you have to set all constant inputs to the programs (like
	///    the position of lights, any base material colors you wish to use etc), but
	///    they do give you much total flexibility over the algorithms used to render your
	///    pass, and you can create some effects which are impossible with a fixed-function pass.
	///    On the other hand, you can define a fixed-function pass in very little time, and
	///    you can use a range of fixed-function effects like environment mapping very
	///    easily, plus your pass will be more likely to be compatible with older hardware.
	///    There are pros and cons to both, just remember that if you use a programmable
	///    pass to create some great effects, allow more time for definition and testing.
	/// </remarks>
	public class Pass
	{

		#region Static Interface

		#region DirtyList Property

		/// <summary>
		///		List of passes with dirty hashes.
		/// </summary>
		private static PassList _dirtyList = new PassList();
		/// <summary>
		///		Gets a list of dirty passes.
		/// </summary>
		internal static PassList DirtyList
		{
			get
			{
				return _dirtyList;
			}
		}

		#endregion DirtyList Property

		#region GraveyardList Property

		/// <summary>
		///		List of passes queued for deletion.
		/// </summary>
		private static PassList _graveyardList = new PassList();
		/// <summary>
		///		Gets a list of passes queued for deletion.
		/// </summary>
		internal static PassList GraveyardList
		{
			get
			{
				return _graveyardList;
			}
		}

		#endregion GraveyardList Property

		protected static int nextPassId = 0;
		protected static Object passLock = new Object();

		#endregion Static Interface

		#region Fields and Properties

		public int passId;

		// <summary>
		//    Texture anisotropy level.
		// </summary>
		//private int _maxAnisotropy;

		#region Parent Property

		/// <summary>
		///    A reference to the technique that owns this Pass.
		/// </summary>
		private Technique _parent;
		/// <summary>
		///    Gets a reference to the Technique that owns this pass.
		/// </summary>
		public Technique Parent
		{
			get
			{
				return _parent;
			}
			set
			{
				_parent = value;
			}
		}

		#endregion Parent Property

		#region Index Property

		/// <summary>
		///    Index of this rendering pass.
		/// </summary>
		private int _index;
		/// <summary>
		///    Gets the index of this Pass in the parent Technique.
		/// </summary>
		public int Index
		{
			get
			{
				return _index;
			}
			set
			{
				_index = value;
				this.DirtyHash();
			}
		}

		#endregion Index Property

		/// <summary>
		///    Pass hash, used for sorting passes.
		/// </summary>
		private int _hashCode;

		#region Name Property

		/// <summary>
		///     Name of this pass (or the index if it isn't set)
		/// </summary>
		private string _name;
		/// <summary>
		///     Name of this pass (or the index if it isn't set)
		/// </summary>
		public string Name
		{
			get
			{
				return _name;
			}
			set
			{
				_name = value;
			}
		}


		#endregion Name Property

		#region Ambient Property

		/// <summary>
		///    Ambient color in fixed function passes.
		/// </summary>
		private ColorEx _ambient;
		/// <summary>
		///    Sets the ambient color reflectance properties of this pass.
		/// </summary>
		/// <remarks>
		///    The base color of a pass is determined by how much red, green and blue light is reflects
		///    (provided texture layer #0 has a blend mode other than LayerBlendOperation.Replace). 
		///    This property determines how much ambient light (directionless global light) is reflected. 
		///    The default is full white, meaning objects are completely globally illuminated. Reduce this 
		///    if you want to see diffuse or specular light effects, or change the blend of colors to make 
		///    the object have a base color other than white.
		///    <p/>
		///    This setting has no effect if dynamic lighting is disabled (see <see cref="Pass.LightingEnabled"/>),
		///    or if this is a programmable pass.
		/// </remarks>
		public ColorEx Ambient
		{
			get
			{
				return _ambient;
			}
			set
			{
				_ambient = value;
			}
		}

		#endregion Ambient Property

		#region Diffuse Property

		/// <summary>
		///    Diffuse color in fixed function passes.
		/// </summary>
		private ColorEx _diffuse;
		/// <summary>
		///    Sets the diffuse color reflectance properties of this pass.
		/// </summary>
		/// <remarks>
		///    The base color of a pass is determined by how much red, green and blue light is reflects
		///    (provided texture layer #0 has a blend mode other than LayerBlendOperation.Replace). This property determines how
		///    much diffuse light (light from instances of the Light class in the scene) is reflected. The default
		///    is full white, meaning objects reflect the maximum white light they can from Light objects.
		///    <p/>
		///    This setting has no effect if dynamic lighting is disabled (see <see cref="Pass.LightingEnabled"/>),
		///    or if this is a programmable pass.
		/// </remarks>
		public ColorEx Diffuse
		{
			get
			{
				return _diffuse;
			}
			set
			{
				_diffuse = value;
			}
		}

		#endregion Diffuse Property

		#region Specular Property

		/// <summary>
		///    Specular color in fixed function passes.
		/// </summary>
		private ColorEx _specular;
		/// <summary>
		///    Sets the specular color reflectance properties of this pass.
		/// </summary>
		/// <remarks>
		///    The base color of a pass is determined by how much red, green and blue light is reflects
		///    (provided texture layer #0 has a blend mode other than LBO_REPLACE). This property determines how
		///    much specular light (highlights from instances of the Light class in the scene) is reflected.
		///    The default is to reflect no specular light.
		///    <p/>
		///    The size of the specular highlights is determined by the separate Shininess property.
		///    <p/>
		///    This setting has no effect if dynamic lighting is disabled (see <see cref="Pass.LightingEnabled"/>),
		///    or if this is a programmable pass.
		/// </remarks>
		public ColorEx Specular
		{
			get
			{
				return _specular;
			}
			set
			{
				_specular = value;
			}
		}

		#endregion Specular Property

		#region Emmissive Property

		/// <summary>
		/// Emissive color in fixed function passes.
		/// </summary>
		private ColorEx _emissive;
		/// <summary>
		/// Emissive color in fixed function passes.
		/// </summary>
		public ColorEx Emissive
		{
			get
			{
				return _emissive;
			}
			set
			{
				_emissive = value;
			}
		}

		/// <summary>
		/// Emissive color in fixed function passes.
		/// </summary>
		public ColorEx SelfIllumination
		{
			get
			{
				return _emissive;
			}
			set
			{
				_emissive = value;
			}
		}

		#endregion Emmissive Property

		#region Shininess Property

		/// <summary>
		///    Shininess of the object's surface in fixed function passes.
		/// </summary>
		private float _shininess;
		/// <summary>
		///    Sets the shininess of the pass, affecting the size of specular highlights.
		/// </summary>
		/// <remarks>
		///    This setting has no effect if dynamic lighting is disabled (see Pass::setLightingEnabled),
		///    or if this is a programmable pass.
		/// </remarks>
		public float Shininess
		{
			get
			{
				return _shininess;
			}
			set
			{
				_shininess = value;
			}
		}

		#endregion Shininess Property

		#region VertexColorTracking Property

		/// <summary>
		///    Color parameters that should track the vertex color for fixed function passes.
		/// </summary>
		private TrackVertexColor _tracking = TrackVertexColor.None;
		/// <summary>
		///    Color parameters that should track the vertex color for fixed function passes.
		/// </summary>
		public TrackVertexColor VertexColorTracking
		{
			get
			{
				return _tracking;
			}
			set
			{
				_tracking = value;
			}
		}

		#endregion VertexColorTracking Property

		#region SourceBlendFactor Property

		/// <summary>
		///    Source blend factor.
		/// </summary>
		private SceneBlendFactor _sourceBlendFactor;
		/// <summary>
		///    Retrieves the source blending factor for the material (as set using SetSceneBlending).
		/// </summary>
		public SceneBlendFactor SourceBlendFactor
		{
			get
			{
				return _sourceBlendFactor;
			}
			set
			{
				_sourceBlendFactor = value;
			}
		}

		#endregion SourceBlendFactor Property

		#region DestinationBlendFactor Property

		/// <summary>
		///    Destination blend factor.
		/// </summary>
		private SceneBlendFactor _destinationBlendFactor;
		/// <summary>
		///    Retrieves the destination blending factor for the material (as set using SetSceneBlending).
		/// </summary>
		public SceneBlendFactor DestinationBlendFactor
		{
			get
			{
				return _destinationBlendFactor;
			}
		}

		#endregion DestinationBlendFactor Property

		#region DepthCheck Property

		/// <summary>
		///    Depth buffer checking setting for this pass.
		/// </summary>
		private bool _depthCheck;
		/// <summary>
		///    Gets/Sets whether or not this pass renders with depth-buffer checking on or not.
		/// </summary>
		/// <remarks>
		///    If depth-buffer checking is on, whenever a pixel is about to be written to the frame buffer
		///    the depth buffer is checked to see if the pixel is in front of all other pixels written at that
		///    point. If not, the pixel is not written.
		///    <p/>
		///    If depth checking is off, pixels are written no matter what has been rendered before.
		///    Also see <see cref="DepthFunction"/> for more advanced depth check configuration.
		/// </remarks>
		public bool DepthCheck
		{
			get
			{
				return _depthCheck;
			}
			set
			{
				_depthCheck = value;
			}
		}

		#endregion DepthCheck Property

		#region DepthWrite Property

		/// <summary>
		///    Depth write setting for this pass.
		/// </summary>
		private bool _depthWrite;
		/// <summary>
		///    Gets/Sets whether or not this pass renders with depth-buffer writing on or not.
		/// </summary>
		/// <remarks>
		///    If depth-buffer writing is on, whenever a pixel is written to the frame buffer
		///    the depth buffer is updated with the depth value of that new pixel, thus affecting future
		///    rendering operations if future pixels are behind this one.
		///    <p/>
		///    If depth writing is off, pixels are written without updating the depth buffer. Depth writing should
		///    normally be on but can be turned off when rendering static backgrounds or when rendering a collection
		///    of transparent objects at the end of a scene so that they overlap each other correctly.
		/// </remarks>
		public bool DepthWrite
		{
			get
			{
				return _depthWrite;
			}
			set
			{
				_depthWrite = value;
			}
		}

		#endregion DepthWrite Property

		#region DepthFunction Property

		/// <summary>
		///    Depth comparison function for this pass.
		/// </summary>
		private CompareFunction _depthFunction;
		/// <summary>
		///    Gets/Sets the function used to compare depth values when depth checking is on.
		/// </summary>
		/// <remarks>
		///    If depth checking is enabled (see <see cref="DepthCheck"/>) a comparison occurs between the depth
		///    value of the pixel to be written and the current contents of the buffer. This comparison is
		///    normally CompareFunction.LessEqual, i.e. the pixel is written if it is closer (or at the same distance)
		///    than the current contents. If you wish, you can change this comparison using this method.
		/// </remarks>
		public CompareFunction DepthFunction
		{
			get
			{
				return _depthFunction;
			}
			set
			{
				_depthFunction = value;
			}
		}

		#endregion DepthFunction Property

		#region DepthBias Properties

		/// <summary>
		///    Depth bias for this pass.
		/// </summary>
		private float _depthBiasConstant;
		/// <summary>
		///		Depth bias slope for this pass.
		/// </summary>
		private float _depthBiasSlopeScale;

		/// <overloads>
		/// <summary>
		///    Sets the depth bias to be used for this Pass.
		/// </summary>
		/// <remarks>
		///    When polygons are coplanar, you can get problems with 'depth fighting' (or 'z fighting') where
		///    the pixels from the two polys compete for the same screen pixel. This is particularly
		///    a problem for decals (polys attached to another surface to represent details such as
		///    bulletholes etc.).
		///    <p/>
		///    A way to combat this problem is to use a depth bias to adjust the depth buffer value
		///    used for the decal such that it is slightly higher than the true value, ensuring that
		///    the decal appears on top. There are two aspects to the biasing, a constant
		///    bias value and a slope-relative biasing value, which varies according to the
		///    maximum depth slope relative to the camera, ie:
		///    <pre>finalBias = maxSlope * slopeScaleBias + constantBias</pre>
		///    Note that slope scale bias, whilst more accurate, may be ignored by old hardware.
		/// </remarks>
		/// <param name="constantBias">The constant bias value, expressed as a factor of the minimum observable depth</param>
		/// </overloads>
		/// <param name="slopeBias">The slope-relative bias value, expressed as a factor of the depth slope</param>
		public void SetDepthBias( float constantBias, float slopeBias )
		{
			_depthBiasConstant = constantBias;
			_depthBiasSlopeScale = slopeBias;
		}

		public void SetDepthBias( float constantBias )
		{
			SetDepthBias( constantBias, 0.0f );
		}

		/// <summary>
		/// Returns the current DepthBiasConstant value for this pass.
		/// </summary>
		/// <remarks>Use <see cref="SetDepthBias(float, float)"/> to set this property</remarks>
		public float DepthBiasConstant
		{
			get
			{
				return _depthBiasConstant;
			}
		}

		/// <summary>
		/// Returns the current DepthBiasSlopeScale value for this pass.
		/// </summary>
		/// <remarks>Use <see cref="SetDepthBias(float, float)"/> to set this property</remarks>
		public float DepthBiasSlopeScale
		{
			get
			{
				return _depthBiasSlopeScale;
			}
		}

		#endregion DepthBias Property

		#region ColorWrite Property

		/// <summary>
		///    Color write setting for this pass.
		/// </summary>
		private bool _colorWriteEnabled;
		/// <summary>
		///    Sets whether or not color buffer writing is enabled for this Pass.
		/// </summary>
		/// <remarks>
		///    For some effects, you might wish to turn off the color write operation
		///    when rendering geometry; this means that only the depth buffer will be
		///    updated (provided you have depth buffer writing enabled, which you 
		///    probably will do, although you may wish to only update the stencil
		///    buffer for example - stencil buffer state is managed at the RenderSystem
		///    level only, not the Material since you are likely to want to manage it 
		///    at a higher level).
		/// </remarks>
		public bool ColorWriteEnabled
		{
			get
			{
				return _colorWriteEnabled;
			}
			set
			{
				_colorWriteEnabled = value;
			}
		}

		#endregion ColorWrite Property

		#region AlphaReject Properties

		private CompareFunction _alphaRejectFunction = CompareFunction.AlwaysPass;
		private int _alphaRejectValue;
		private bool _alphaToCoverageEnabled;

		/// <summary>
		/// Sets the way the pass will have use alpha to totally reject pixels from the pipeline.
		/// </summary>
		/// <remarks>
		/// The default is <see ref="CompareFunction.AlwaysPass" /> i.e. alpha is not used to reject pixels.
		/// <para>This option applies in both the fixed function and the programmable pipeline.</para></remarks>
		/// <param name="alphaRejectFunction">The comparison which must pass for the pixel to be written.</param>
		/// <param name="value">value against which alpha values will be tested [(0-255]</param>
		public void SetAlphaRejectSettings( CompareFunction alphaRejectFunction, int value )
		{
			_alphaRejectFunction = alphaRejectFunction;
			_alphaRejectValue = value;
		}

		/// <summary>
		/// The comparison which must pass for the pixel to be written.
		/// </summary>
		public CompareFunction AlphaRejectFunction
		{
			get
			{
				return _alphaRejectFunction;
			}
			set
			{
				_alphaRejectFunction = value;
			}
		}

		/// <summary>
		/// value against which alpha values will be tested [(0-255]
		/// </summary>
		public int AlphaRejectValue
		{
			get
			{
				return _alphaRejectValue;
			}
			set
			{
				Debug.Assert( value < 255 && value > 0, "AlphaRejectValue must be between 0 and 255" );
				_alphaRejectValue = value;
			}
		}

		/// <summary>
		/// Whether to use alpha to coverage (A2C) when blending alpha rejected values
		/// </summary>
		public bool IsAlphaToCoverageEnabled
		{
			get
			{
				return _alphaToCoverageEnabled;
			}
			set
			{
				_alphaToCoverageEnabled = value;
			}
		}

		#endregion AlphaReject Properties

		#region CullingMode Property

		/// <summary>
		///    Hardware culling mode for this pass.
		/// </summary>
		private CullingMode _cullingMode;
		/// <summary>
		///    Sets the culling mode for this pass based on the 'vertex winding'.
		/// </summary>
		/// <remarks>
		///    A typical way for the rendering engine to cull triangles is based on the 'vertex winding' of
		///    triangles. Vertex winding refers to the direction in which the vertices are passed or indexed
		///    to in the rendering operation as viewed from the camera, and will wither be clockwise or
		///    counterclockwise. The default is Clockwise i.e. that only triangles whose vertices are passed/indexed in 
		///    counter-clockwise order are rendered - this is a common approach and is used in 3D studio models for example. 
		///    You can alter this culling mode if you wish but it is not advised unless you know what you are doing.
		///    <p/>
		///    You may wish to use the CullingMode.None option for mesh data that you cull yourself where the vertex
		///    winding is uncertain.
		/// </remarks>
		public CullingMode CullingMode
		{
			get
			{
				return _cullingMode;
			}
			set
			{
				_cullingMode = value;
			}
		}

		#endregion CullingMode Property

		#region ManualCullingMode Property

		/// <summary>
		///    Software culling mode for this pass.
		/// </summary>
		private ManualCullingMode _manualCullingMode;
		/// <summary>
		///    Sets the manual culling mode, performed by CPU rather than hardware.
		/// </summary>
		/// <remarks>
		///    In some situations you want to use manual culling of triangles rather than sending the
		///    triangles to the hardware and letting it cull them. This setting only takes effect on SceneManager's
		///    that use it (since it is best used on large groups of planar world geometry rather than on movable
		///    geometry since this would be expensive), but if used can cull geometry before it is sent to the
		///    hardware.
		/// </remarks>
		/// <value>
		///    The default for this setting is ManualCullingMode.Back.
		/// </value>
		public ManualCullingMode ManualCullingMode
		{
			get
			{
				return _manualCullingMode;
			}
			set
			{
				_manualCullingMode = value;
			}
		}

		#endregion ManualCullingMode Property

		#region LightingEnabled Property

		/// <summary>
		///    Is lighting enabled for this pass?
		/// </summary>
		private bool _lightingEnabled;
		/// <summary>
		///    Sets whether or not dynamic lighting is enabled.
		/// </summary>
		/// <remarks>
		///    If true, dynamic lighting is performed on geometry with normals supplied, geometry without
		///    normals will not be displayed.
		///    If false, no lighting is applied and all geometry will be full brightness.
		/// </remarks>
		public bool LightingEnabled
		{
			get
			{
				return _lightingEnabled;
			}
			set
			{
				_lightingEnabled = value;
			}
		}

		#endregion LightingEnabled Property

		#region MaxSimultaneousLights Property

		/// <summary>
		///    Max number of simultaneous lights that can be used for this pass.
		/// </summary>
		private int _maxSimultaneousLights;
		/// <summary>
		///    Sets the maximum number of lights to be used by this pass. 
		/// </summary>
		/// <remarks>
		///    During rendering, if lighting is enabled (or if the pass uses an automatic
		///    program parameter based on a light) the engine will request the nearest lights 
		///    to the object being rendered in order to work out which ones to use. This
		///    parameter sets the limit on the number of lights which should apply to objects 
		///    rendered with this pass. 
		/// </remarks>
		public int MaxSimultaneousLights
		{
			get
			{
				return _maxSimultaneousLights;
			}
			set
			{
				_maxSimultaneousLights = value;
			}
		}

		#endregion MaxLights Property

		#region StartLight Property

		/// <summary>
		///    the light index that this pass will start at in the light list.
		/// </summary>
		private bool _startLight;
		/// <summary>
		///    Sets the light index that this pass will start at in the light list.
		/// </summary>
		/// <remarks>
		/// Normally the lights passed to a pass will start from the beginning
		/// of the light list for this object. This option allows you to make this
		/// pass start from a higher light index, for example if one of your earlier
		/// passes could deal with lights 0-3, and this pass dealt with lights 4+. 
		/// This option also has an interaction with pass iteration, in that
		/// if you choose to iterate this pass per light too, the iteration will
		/// only begin from light 4.
		/// </remarks>
		public bool StartLight
		{
			get
			{
				return _startLight;
			}
			set
			{
				_startLight = value;
			}
		}

		#endregion StartLight Property

		#region IteratePerLight Property

		/// <summary>
		///    Run this pass once per light? 
		/// </summary>
		private bool _iteratePerLight;
		/// <summary>
		///    Does this pass run once for every light in range?
		/// </summary>
		public bool IteratePerLight
		{
			get
			{
				return _iteratePerLight;
			}
			set
			{
				_iteratePerLight = value;
			}
		}

		#endregion IteratePerLight Property

		#region LightsPerIteration Property
		private int _lightsPerIteration = 1;

		/// <summary>
		/// If light iteration is enabled, determine the number of lights per iteration.
		/// </summary>
		/// <remarks>
		/// The default for this setting is 1, so if you enable light iteration
		/// (<see cref="IteratePerLight"/>), the pass is rendered once per light. If
		/// you set this value higher, the passes will occur once per 'n' lights.
		/// The start of the iteration is set by <see cref="StartLight"/> and the end
		/// by <see cref="MaxSimultaneousLights"/>.
		/// </remarks>
		public int LightsPerIteration
		{
			get
			{
				return _lightsPerIteration;
			}
			set
			{
				_lightsPerIteration = value;
			}
		}

		#endregion LightsPerIteration Property

		#region RunOnlyOncePerLightType Property

		/// <summary>
		///     Should it only be run for a certain light type? 
		/// </summary>
		private bool _runOnlyForOneLightType;
		/// <summary>
		///    Does this pass run only for a single light type (if RunOncePerLight is true). 
		/// </summary>
		public bool RunOnlyOncePerLightType
		{
			get
			{
				return _runOnlyForOneLightType;
			}
			set
			{
				_runOnlyForOneLightType = value;
			}
		}

		#endregion RunOnlyOncePerLightType Property

		#region OnlyLightType Property

		/// <summary>
		///    Type of light for a programmable pass that supports only one particular type of light.
		/// </summary>
		private LightType _onlyLightType;
		/// <summary>
		///     Gets the single light type this pass runs for if RunOncePerLight and 
		///     RunOnlyForOneLightType are both true. 
		/// </summary>
		public LightType OnlyLightType
		{
			get
			{
				return _onlyLightType;
			}
			set
			{
				_onlyLightType = value;
			}
		}

		#endregion OnlyLightType Property

		#region ShadingMode Property

		/// <summary>
		///    Shading options for this pass.
		/// </summary>
		private Shading _shadingMode;
		/// <summary>
		///    Sets the type of light shading required.
		/// </summary>
		/// <value>
		///    The default shading method is Gouraud shading.
		/// </value>
		public Shading ShadingMode
		{
			get
			{
				return _shadingMode;
			}
			set
			{
				_shadingMode = value;
			}
		}

		#endregion ShadingMode Property

		#region PolygonMode Property

		/// <summary>
		/// the type of polygon rendering required
		/// </summary>
		private PolygonMode _polygonMode = PolygonMode.Solid;
		/// <summary>
		/// Sets the type of polygon rendering required
		/// </summary>
		/// <remarks>
		/// The default shading method is Solid
		/// </remarks>
		public PolygonMode PolygonMode
		{
			get
			{
				return _polygonMode;
			}
			set
			{
				_polygonMode = value;
			}
		}

		#endregion PolygonMode Property

		#region Fog Properties

		#region FogOverride Property

		/// <summary>
		///    Does this pass override global fog settings?
		/// </summary>
		private bool _fogOverride;
		/// <summary>
		///    Returns true if this pass is to override the scene fog settings.
		/// </summary>
		public bool FogOverride
		{
			get
			{
				return _fogOverride;
			}
			protected set
			{
				_fogOverride = value;
			}
		}

		#endregion FogOverride Property

		#region FogMode Property

		/// <summary>
		///    Fog mode to use for this pass (if overriding).
		/// </summary>
		private FogMode _fogMode;
		/// <summary>
		///    Returns the fog mode for this pass.
		/// </summary>
		/// <remarks>
		///    Only valid if FogOverride is true.
		/// </remarks>
		public FogMode FogMode
		{
			get
			{
				return _fogMode;
			}
			protected set
			{
				_fogMode = value;
			}
		}

		#endregion FogMode Property

		#region FogColor Property

		/// <summary>
		///    Color of the fog used for this pass (if overriding).
		/// </summary>
		private ColorEx _fogColor;
		/// <summary>
		///    Returns the fog color for the scene.
		/// </summary>
		/// <remarks>
		///    Only valid if FogOverride is true.
		/// </remarks>
		public ColorEx FogColor
		{
			get
			{
				return _fogColor;
			}
			protected set
			{
				_fogColor = value;
			}
		}

		#endregion FogColor Property

		#region FogStart Property

		/// <summary>
		///    Starting point of the fog for this pass (if overriding).
		/// </summary>
		private float _fogStart;
		/// <summary>
		///    Returns the fog start distance for this pass.
		/// </summary>
		/// <remarks>
		///    Only valid if FogOverride is true.
		/// </remarks>
		public float FogStart
		{
			get
			{
				return _fogStart;
			}
			protected set
			{
				_fogStart = value;
			}
		}

		#endregion FogStart Property

		#region FogEnd Property

		/// <summary>
		///    Ending point of the fog for this pass (if overriding).
		/// </summary>
		private float _fogEnd;
		/// <summary>
		///    Returns the fog end distance for this pass.
		/// </summary>
		/// <remarks>
		///    Only valid if FogOverride is true.
		/// </remarks>
		public float FogEnd
		{
			get
			{
				return _fogEnd;
			}
			set
			{
				_fogEnd = value;
			}
		}

		#endregion FogEnd Property

		#region FogDensity Property

		/// <summary>
		///    Density of the fog for this pass (if overriding).
		/// </summary>
		private float _fogDensity;
		/// <summary>
		///    Returns the fog density for this pass.
		/// </summary>
		/// <remarks>
		///    Only valid if FogOverride is true.
		/// </remarks>
		public float FogDensity
		{
			get
			{
				return _fogDensity;
			}
			protected set
			{
				_fogDensity = value;
			}
		}

		#endregion FogDensity Property

		#endregion Fog Properties

		#region TextureUnitState Convenience Properties

		/// <summary>
		///    List of fixed function texture unit states for this pass.
		/// </summary>
		protected TextureUnitStateList textureUnitStates = new TextureUnitStateList();

		/// <summary>
		///    Gets the number of fixed function texture unit states for this Pass.
		/// </summary>
		public int TextureUnitStageCount
		{
			get
			{
				return textureUnitStates.Count;
			}
		}

		/// <summary>
		///    Sets the anisotropy level to be used for all textures.
		/// </summary>
		/// <remarks>
		///    This property has been moved to the TextureUnitState class, which is accessible via the 
		///    Technique and Pass. For simplicity, this method allows you to set these properties for 
		///    every current TeextureUnitState, If you need more precision, retrieve the Technique, 
		///    Pass and TextureUnitState instances and set the property there.
		/// </remarks>
		public int TextureAnisotropy
		{
			set
			{
				for ( int i = 0; i < textureUnitStates.Count; i++ )
				{
					( (TextureUnitState)textureUnitStates[ i ] ).TextureAnisotropy = value;
				}
			}
		}

		/// <summary>
		///    Set texture filtering for every texture unit.
		/// </summary>
		/// <remarks>
		///    This property actually exists on the TextureUnitState class
		///    For simplicity, this method allows you to set these properties for 
		///    every current TeextureUnitState, If you need more precision, retrieve the  
		///    TextureUnitState instance and set the property there.
		/// </remarks>
		public TextureFiltering TextureFiltering
		{
			set
			{
				for ( int i = 0; i < textureUnitStates.Count; i++ )
				{
					( (TextureUnitState)textureUnitStates[ i ] ).SetTextureFiltering( value );
				}
			}
		}

		#endregion TextureUnitState Convenience Properties

		#region Programmable Pipeline Propteries

		/// <summary>
		///    Returns true if this pass is programmable ie includes either a vertex or fragment program.
		/// </summary>
		public bool IsProgrammable
		{
			get
			{
				return _vertexProgramUsage != null || _fragmentProgramUsage != null;
			}
		}

		#region VertexProgram Properties

		/// <summary>
		///    Details on the vertex program to be used for this pass.
		/// </summary>
		private GpuProgramUsage _vertexProgramUsage;

		/// <summary>
		///    Returns true if this Pass uses the programmable vertex pipeline.
		/// </summary>
		public bool HasVertexProgram
		{
			get
			{
				return _vertexProgramUsage != null;
			}
		}

		/// <summary>
		///    Gets the vertex program used by this pass.
		/// </summary>
		/// <remarks>
		///    Only available after Load() has been called.
		/// </remarks>
		public GpuProgram VertexProgram
		{
			get
			{
				Debug.Assert( this.HasVertexProgram, "This pass does not contain a vertex program!" );
				return _vertexProgramUsage.Program;
			}
		}

		/// <summary>
		///    Gets/Sets the name of the vertex program to use.
		/// </summary>
		/// <remarks>
		///    Only applicable to programmable passes, and this particular call is
		///    designed for low-level programs; use the named parameter methods
		///    for setting high-level programs.
		///    <p/>
		///    This must have been created using GpuProgramManager by the time that 
		///    this Pass is loaded.
		/// </remarks>
		public string VertexProgramName
		{
			get
			{
				if ( this.HasVertexProgram )
				{
					return _vertexProgramUsage.ProgramName;
				}
				else
				{
					return String.Empty;
				}
			}
			set
			{
				SetVertexProgram( value );
			}
		}

		/// <summary>
		///    Gets/Sets the vertex program parameters used by this pass.
		/// </summary>
		/// <remarks>
		///    Only applicable to programmable passes, and this particular call is
		///    designed for low-level programs; use the named parameter methods
		///    for setting high-level program parameters.
		/// </remarks>
		public GpuProgramParameters VertexProgramParameters
		{
			get
			{
				Debug.Assert( this.HasVertexProgram, "This pass does not contain a vertex program!" );
				return _vertexProgramUsage.Parameters;
			}
			set
			{
				Debug.Assert( this.HasVertexProgram, "This pass does not contain a vertex program!" );
				_vertexProgramUsage.Parameters = value;
			}
		}

		#endregion VertexProgram Properties

		#region FragmentProgram Properties

		/// <summary>
		///    Details on the fragment program to be used for this pass.
		/// </summary>
		private GpuProgramUsage _fragmentProgramUsage;

		/// <summary>
		///    Returns true if this Pass uses the programmable fragment pipeline.
		/// </summary>
		public bool HasFragmentProgram
		{
			get
			{
				return _fragmentProgramUsage != null;
			}
		}

		/// <summary>
		///    Gets the fragment program used by this pass.
		/// </summary>
		/// <remarks>
		///    Only available after Load() has been called.
		/// </remarks>
		public GpuProgram FragmentProgram
		{
			get
			{
				Debug.Assert( this.HasFragmentProgram, "This pass does not contain a fragment program!" );
				return _fragmentProgramUsage.Program;
			}
		}

		/// <summary>
		///    Gets/Sets the name of the fragment program to use.
		/// </summary>
		/// <remarks>
		///    Only applicable to programmable passes, and this particular call is
		///    designed for low-level programs; use the named parameter methods
		///    for setting high-level programs.
		///    <p/>
		///    This must have been created using GpuProgramManager by the time that 
		///    this Pass is loaded.
		/// </remarks>
		public string FragmentProgramName
		{
			get
			{
				// return blank if there is no fragment program in this pass
				if ( this.HasFragmentProgram )
				{
					return _fragmentProgramUsage.ProgramName;
				}
				else
				{
					return String.Empty;
				}
			}
			set
			{
				SetFragmentProgram( value );
			}
		}

		/// <summary>
		///    Gets/Sets the fragment program parameters used by this pass.
		/// </summary>
		/// <remarks>
		///    Only applicable to programmable passes, and this particular call is
		///    designed for low-level programs; use the named parameter methods
		///    for setting high-level program parameters.
		/// </remarks>
		public GpuProgramParameters FragmentProgramParameters
		{
			get
			{
				Debug.Assert( this.HasFragmentProgram, "This pass does not contain a fragment program!" );
				return _fragmentProgramUsage.Parameters;
			}
			set
			{
				Debug.Assert( this.HasFragmentProgram, "This pass does not contain a fragment program!" );
				_fragmentProgramUsage.Parameters = value;
			}
		}

		#endregion FragmentProgram Properties

		#region GeometryProgram Properties

		/// <summary>
		///    Details on the geometry program to be used for this pass.
		/// </summary>
		private GpuProgramUsage _geometryProgramUsage;

		/// <summary>
		///    Returns true if this Pass uses the programmable geometry pipeline.
		/// </summary>
		public bool HasGeometryProgram
		{
			get
			{
				return _geometryProgramUsage != null;
			}
		}

		/// <summary>
		///    Gets the geometry program used by this pass.
		/// </summary>
		/// <remarks>
		///    Only available after Load() has been called.
		/// </remarks>
		public GpuProgram GeometryProgram
		{
			get
			{
				Debug.Assert( this.HasGeometryProgram, "This pass does not contain a geometry program!" );
				return _geometryProgramUsage.Program;
			}
		}

		/// <summary>
		///    Gets/Sets the name of the geometry program to use.
		/// </summary>
		/// <remarks>
		///    Only applicable to programmable passes, and this particular call is
		///    designed for low-level programs; use the named parameter methods
		///    for setting high-level programs.
		///    <p/>
		///    This must have been created using GpuProgramManager by the time that 
		///    this Pass is loaded.
		/// </remarks>
		public string GeometryProgramName
		{
			get
			{
				// return blank if there is no geometry program in this pass
				if ( this.HasGeometryProgram )
				{
					return _geometryProgramUsage.ProgramName;
				}
				else
				{
					return String.Empty;
				}
			}
			set
			{
				SetGeometryProgram( value );
			}
		}

		/// <summary>
		///    Gets/Sets the geometry program parameters used by this pass.
		/// </summary>
		/// <remarks>
		///    Only applicable to programmable passes, and this particular call is
		///    designed for low-level programs; use the named parameter methods
		///    for setting high-level program parameters.
		/// </remarks>
		public GpuProgramParameters GeometryProgramParameters
		{
			get
			{
				Debug.Assert( this.HasGeometryProgram, "This pass does not contain a geomtery program!" );
				return _geometryProgramUsage.Parameters;
			}
			set
			{
				Debug.Assert( this.HasGeometryProgram, "This pass does not contain a geometry program!" );
				_geometryProgramUsage.Parameters = value;
			}
		}

		#endregion GeometryProgram Properties

		#region ShadowCasterVertexProgram Properties

		/// <summary>
		///    Details on the shadow caster vertex program to be used for this pass.
		/// </summary>
		protected GpuProgramUsage shadowCasterVertexProgramUsage;

		/// <summary>
		///    Returns true if this Pass uses the programmable shadow caster vertex pipeline.
		/// </summary>
		public bool HasShadowCasterVertexProgram
		{
			get
			{
				return shadowCasterVertexProgramUsage != null;
			}
		}
		public string ShadowCasterVertexProgramName
		{
			get
			{
				if ( this.HasShadowCasterVertexProgram )
				{
					return shadowCasterVertexProgramUsage.ProgramName;
				}
				else
				{
					return String.Empty;
				}
			}
		}
		public GpuProgramParameters ShadowCasterVertexProgramParameters
		{
			get
			{
				Debug.Assert( this.HasShadowCasterVertexProgram, "This pass does not contain a shadow caster vertex program!" );
				return shadowCasterVertexProgramUsage.Parameters;
			}
			set
			{
				Debug.Assert( this.HasShadowCasterVertexProgram, "This pass does not contain a shadow caster vertex program!" );
				shadowCasterVertexProgramUsage.Parameters = value;
			}
		}

		#endregion ShadowCasterVertexProgram Properties

		#region ShadowCasterFragmentProgram Properties

		/// <summary>
		///    Details on the shadow caster fragment program to be used for this pass.
		/// </summary>
		private GpuProgramUsage _shadowCasterFragmentProgramUsage;

		/// <summary>
		///    Returns true if this Pass uses the programmable shadow caster fragment pipeline.
		/// </summary>
		public bool HasShadowCasterFragmentProgram
		{
			get
			{
				return _shadowCasterFragmentProgramUsage != null;
			}
		}
		public string ShadowCasterFragmentProgramName
		{
			get
			{
				if ( this.HasShadowCasterFragmentProgram )
				{
					return _shadowCasterFragmentProgramUsage.ProgramName;
				}
				else
				{
					return String.Empty;
				}
			}
		}
		public GpuProgramParameters ShadowCasterFragmentProgramParameters
		{
			get
			{
				Debug.Assert( this.HasShadowCasterFragmentProgram, "This pass does not contain a shadow caster fragment program!" );
				return _shadowCasterFragmentProgramUsage.Parameters;
			}
			set
			{
				Debug.Assert( this.HasShadowCasterFragmentProgram, "This pass does not contain a shadow caster fragment program!" );
				_shadowCasterFragmentProgramUsage.Parameters = value;
			}
		}

		#endregion ShadowCasterFragmentProgram Properties

		#region ShadowRecieverVertexProgram Properties

		/// <summary>
		///    Details on the shadow receiver vertex program to be used for this pass.
		/// </summary>
		private GpuProgramUsage _shadowReceiverVertexProgramUsage;
		/// <summary>
		///    Returns true if this Pass uses the programmable shadow receiver vertex pipeline.
		/// </summary>
		public bool HasShadowReceiverVertexProgram
		{
			get
			{
				return _shadowReceiverVertexProgramUsage != null;
			}
		}
		public string ShadowReceiverVertexProgramName
		{
			get
			{
				if ( this.HasShadowReceiverVertexProgram )
				{
					return _shadowReceiverVertexProgramUsage.ProgramName;
				}
				else
				{
					return String.Empty;
				}
			}
		}
		public GpuProgramParameters ShadowReceiverVertexProgramParameters
		{
			get
			{
				Debug.Assert( this.HasShadowReceiverVertexProgram, "This pass does not contain a shadow receiver vertex program!" );
				return _shadowReceiverVertexProgramUsage.Parameters;
			}
			set
			{
				Debug.Assert( this.HasShadowReceiverVertexProgram, "This pass does not contain a shadow receiver vertex program!" );
				_shadowReceiverVertexProgramUsage.Parameters = value;
			}
		}

		#endregion ShadowRecieverVertexProgram Properties

		#region ShadowRecieverFragmentProgram Properties

		/// <summary>
		///    Details on the shadow receiver fragment program to be used for this pass.
		/// </summary>
		private GpuProgramUsage _shadowReceiverFragmentProgramUsage;
		/// <summary>
		///    Returns true if this Pass uses the programmable shadow receiver fragment pipeline.
		/// </summary>
		public bool HasShadowReceiverFragmentProgram
		{
			get
			{
				return _shadowReceiverFragmentProgramUsage != null;
			}
		}
		public string ShadowReceiverFragmentProgramName
		{
			get
			{
				if ( this.HasShadowReceiverFragmentProgram )
				{
					return _shadowReceiverFragmentProgramUsage.ProgramName;
				}
				else
				{
					return String.Empty;
				}
			}
		}
		public GpuProgramParameters ShadowReceiverFragmentProgramParameters
		{
			get
			{
				Debug.Assert( this.HasShadowReceiverFragmentProgram, "This pass does not contain a shadow receiver fragment program!" );
				return _shadowReceiverFragmentProgramUsage.Parameters;
			}
			set
			{
				Debug.Assert( this.HasShadowReceiverFragmentProgram, "This pass does not contain a shadow receiver fragment program!" );
				_shadowReceiverFragmentProgramUsage.Parameters = value;
			}
		}

		#endregion ShadowRecieverFragmentProgram Properties

		#endregion Programmable Pipeline Propteries

		#region PointSize Property

		private float _pointSize;
		public float PointSize
		{
			get
			{
				return _pointSize;
			}
			set
			{
				_pointSize = value;
			}
		}

		#endregion PointSize Property

		#region PointMinSize Property

		private float _pointMinSize;
		public float PointMinSize
		{
			get
			{
				return _pointMinSize;
			}
			set
			{
				_pointMinSize = value;
			}
		}

		#endregion PointMinSize Property

		#region PointMaxSize Property

		private float _pointMaxSize;
		public float PointMaxSize
		{
			get
			{
				return _pointMaxSize;
			}
			set
			{
				_pointMaxSize = value;
			}
		}

		#endregion PointMaxSize Property

		#region PointSpritesEnabled Property

		private bool _pointSpritesEnabled;

		public bool PointSpritesEnabled
		{
			get
			{
				return _pointSpritesEnabled;
			}
			set
			{
				_pointSpritesEnabled = value;
			}
		}

		#endregion PointSpritesEnabled Property

		/// <summary>
		///		Is this pass queued for deletion?
		/// </summary>
		protected bool queuedForDeletion;

		#region IterationCount Property
        /// <summary>
        /// the number of iterations that this pass should perform when doing fast multi pass operation.
        /// </summary>
        /// <remarks>
        /// Only applicable for programmable passes.
        /// A value greater than 1 will cause the pass to be executed count number of
        /// times without changing the render state.  This is very usefull for passes
        /// that use programmable shaders that have to iterate more than once but don't
        /// need a render state change.  Using multi pass can dramatically speed up rendering
        /// for materials that do things like fur, blur.
        /// A value of 1 turns off multi pass operation and the pass does
        /// the normal pass operation.
        /// </remarks>
        public int IterationCount { get; set; }

        #endregion IterationCount Property

		/// <summary>
		///		Gets a flag indicating whether this pass is ambient only.
		/// </summary>
		public bool IsAmbientOnly
		{
			get
			{
				// treat as ambient if lighting is off, or color write is off, 
				// or all non-ambient (& emissive) colors are black
				// NB a vertex program could override this, but passes using vertex
				// programs are expected to indicate they are ambient only by 
				// setting the state so it matches one of the conditions above, even 
				// though this state is not used in rendering.
				return ( !_lightingEnabled ||
						 !_colorWriteEnabled ||
						 ( _diffuse == ColorEx.Black && _specular == ColorEx.Black ) );
			}
		}

		/// <summary>
		///    Returns true if this pass is loaded.
		/// </summary>
		public bool IsLoaded
		{
			get
			{
				return _parent.IsLoaded;
			}
		}


		/// <summary>
		///    Returns true if this pass has some element of transparency.
		/// </summary>
		public bool IsTransparent
		{
			get
			{
				// Transparent if any of the destination color is taken into account
				return ( _destinationBlendFactor != SceneBlendFactor.Zero );
			}
		}

		#endregion Fields and Properties

		#region Construction and Destruction

		/// <summary>
		///    Default constructor.
		/// </summary>
		/// <param name="parent">Technique that owns this Pass.</param>
		/// <param name="index">Index of this pass.</param>
		public Pass( Technique parent, int index )
		{
			this._parent = parent;
			this._index = index;

			lock ( passLock )
			{
				this.passId = nextPassId++;
			}

			// color defaults
			_ambient = ColorEx.White;
			_diffuse = ColorEx.White;
			_specular = ColorEx.Black;
			_emissive = ColorEx.Black;

			// by default, don't override the scene's fog settings
			_fogOverride = false;
			_fogMode = FogMode.None;
			_fogColor = ColorEx.White;
			_fogStart = 0;
			_fogEnd = 1;
			_fogDensity = 0.001f;

			// default blending (overwrite)
			_sourceBlendFactor = SceneBlendFactor.One;
			_destinationBlendFactor = SceneBlendFactor.Zero;



			// depth buffer settings
			_depthCheck = true;
			_depthWrite = true;
			_colorWriteEnabled = true;
			_depthFunction = CompareFunction.LessEqual;

			// cull settings
			_cullingMode = CullingMode.Clockwise;
			_manualCullingMode = ManualCullingMode.Back;

			// light settings
			_lightingEnabled = true;
			_runOnlyForOneLightType = true;
			_onlyLightType = LightType.Point;
			_shadingMode = Shading.Gouraud;

			// Default max lights to the global max
			_maxSimultaneousLights = Config.MaxSimultaneousLights;

			_name = index.ToString();

            IterationCount = 1;

			DirtyHash();
		}

		#endregion Construction and Destruction

		#region Methods

		/// <summary>
		///    Adds the passed in TextureUnitState, to the existing Pass.
		/// </summary>
		/// <param name="state">TextureUnitState to add to this pass.</param>
		public void AddTextureUnitState( TextureUnitState state )
		{
			textureUnitStates.Add( state );
			if ( state.Name == null )
			{
				// it's the last entry in the container so it's index is count - 1
				state.Name = ( textureUnitStates.Count - 1 ).ToString();
				// Since the anme was never set and a default one has been made, 
				// clear the alias name so that when the texture unit name is set 
				// by the user, the alias name will be set to that name.
				state.TextureNameAlias = null;
			}
			// needs recompilation
			_parent.NotifyNeedsRecompile();
			DirtyHash();
		}

	    /// <summary>
	    ///    Method for cloning a Pass object.
	    /// </summary>
	    /// <param name="parent">Parent technique that will own this cloned Pass.</param>
	    /// <param name="index"></param>
	    /// <returns></returns>
	    public Pass Clone( Technique parent, int index )
		{
			Pass newPass = new Pass( parent, index );

			CopyTo( newPass );

			// dirty the hash on the new pass
			newPass.DirtyHash();

			return newPass;
		}

		/// <summary>
		///		Copy the details of this pass to the target pass.
		/// </summary>
		/// <param name="target">Destination pass to copy this pass's attributes to.</param>
		public void CopyTo( Pass target )
		{
			target._name = _name;
			target._hashCode = _hashCode;

			// surface
			target._ambient = _ambient.Clone();
			target._diffuse = _diffuse.Clone();
			target._specular = _specular.Clone();
			target._emissive = _emissive.Clone();
			target._shininess = _shininess;
			target._tracking = _tracking;

			// fog
			target._fogOverride = _fogOverride;
			target._fogMode = _fogMode;
			target._fogColor = _fogColor.Clone();
			target._fogStart = _fogStart;
			target._fogEnd = _fogEnd;
			target._fogDensity = _fogDensity;

			// default blending
			target._sourceBlendFactor = _sourceBlendFactor;
			target._destinationBlendFactor = _destinationBlendFactor;

			target._depthCheck = _depthCheck;
			target._depthWrite = _depthWrite;
			target._alphaRejectFunction = _alphaRejectFunction;
			target._alphaRejectValue = _alphaRejectValue;
			target._colorWriteEnabled = _colorWriteEnabled;
			target._depthFunction = _depthFunction;
			target._depthBiasConstant = _depthBiasConstant;
			target._depthBiasSlopeScale = _depthBiasSlopeScale;
			target._cullingMode = _cullingMode;
			target._manualCullingMode = _manualCullingMode;
			target._lightingEnabled = _lightingEnabled;
			target._maxSimultaneousLights = _maxSimultaneousLights;
			target._iteratePerLight = _iteratePerLight;
			target._runOnlyForOneLightType = _runOnlyForOneLightType;
			target._onlyLightType = _onlyLightType;
			target._shadingMode = _shadingMode;
			target._polygonMode = _polygonMode;
			target.IterationCount = IterationCount;

			// vertex program
			if ( _vertexProgramUsage != null )
			{
			    target._vertexProgramUsage = new GpuProgramUsage( _vertexProgramUsage, this );
			}
			else
			{
				target._vertexProgramUsage = null;
			}

			// shadow caster vertex program
			if ( shadowCasterVertexProgramUsage != null )
			{
				target.shadowCasterVertexProgramUsage = new GpuProgramUsage(shadowCasterVertexProgramUsage, this);
			}
			else
			{
				target.shadowCasterVertexProgramUsage = null;
			}

			// shadow receiver vertex program
			if ( _shadowReceiverVertexProgramUsage != null )
			{
				target._shadowReceiverVertexProgramUsage = new GpuProgramUsage(_shadowReceiverVertexProgramUsage, this);
			}
			else
			{
				target._shadowReceiverVertexProgramUsage = null;
			}

			// fragment program
			if ( _fragmentProgramUsage != null )
			{
				target._fragmentProgramUsage = new GpuProgramUsage(_fragmentProgramUsage, this);
			}
			else
			{
				target._fragmentProgramUsage = null;
			}

			// shadow caster fragment program
			if ( _shadowCasterFragmentProgramUsage != null )
			{
				target._shadowCasterFragmentProgramUsage = new GpuProgramUsage(_shadowCasterFragmentProgramUsage, this);
			}
			else
			{
				target._shadowCasterFragmentProgramUsage = null;
			}

			// shadow receiver fragment program
			if ( _shadowReceiverFragmentProgramUsage != null )
			{
				target._shadowReceiverFragmentProgramUsage = new GpuProgramUsage(_shadowReceiverFragmentProgramUsage, this);
			}
			else
			{
				target._shadowReceiverFragmentProgramUsage = null;
			}


			// Clear texture units but doesn't notify need recompilation in the case
			// we are cloning, The parent material will take care of this.
			target.textureUnitStates.Clear();

			// Copy texture units

			for ( int i = 0; i < textureUnitStates.Count; i++ )
			{
				TextureUnitState newState = new TextureUnitState( target );
				TextureUnitState src = (TextureUnitState)textureUnitStates[ i ];
				src.CopyTo( newState );

				target.textureUnitStates.Add( newState );
			}

			target.DirtyHash();
		}

		/// <summary>
		///    Overloaded method.
		/// </summary>
		/// <returns></returns>
		public TextureUnitState CreateTextureUnitState()
		{
			TextureUnitState state = new TextureUnitState( this );
			textureUnitStates.Add( state );
			// needs recompilation
			_parent.NotifyNeedsRecompile();
			DirtyHash();
			return state;
		}

		/// <summary>
		///    Overloaded method.
		/// </summary>
		/// <param name="textureName">The basic name of the texture (i.e. brickwall.jpg)</param>
		/// <returns></returns>
		public TextureUnitState CreateTextureUnitState( string textureName )
		{
			return CreateTextureUnitState( textureName, 0 );
		}

		/// <summary>
		///    Inserts a new TextureUnitState object into the Pass.
		/// </summary>
		/// <remarks>
		///    This unit is is added on top of all previous texture units.
		///    <p/>
		///    Applies to both fixed-function and programmable passes.
		/// </remarks>
		/// <param name="textureName">The basic name of the texture (i.e. brickwall.jpg)</param>
		/// <param name="texCoordSet">The index of the texture coordinate set to use.</param>
		/// <returns></returns>
		public TextureUnitState CreateTextureUnitState( string textureName, int texCoordSet )
		{
			TextureUnitState state = new TextureUnitState( this );
			state.SetTextureName( textureName );
			state.TextureCoordSet = texCoordSet;
			textureUnitStates.Add( state );
			// needs recompilation
			_parent.NotifyNeedsRecompile();
			DirtyHash();
			return state;
		}

		/// <summary>
		///    Gets a reference to the TextureUnitState for this pass at the specified indx.
		/// </summary>
		/// <param name="index">Index of the state to retreive.</param>
		/// <returns>TextureUnitState at the specified index.</returns>
		public TextureUnitState GetTextureUnitState( int index )
		{
			Debug.Assert( index >= 0 && index < textureUnitStates.Count, "index out of range" );

			return (TextureUnitState)textureUnitStates[ index ];
		}

		/// <summary>
		///    Internal method for loading this pass.
		/// </summary>
		internal void Load()
		{
			// it is assumed this is only being called when the Material is being loaded

			// load each texture unit state
			for ( int i = 0; i < textureUnitStates.Count; i++ )
			{
				( (TextureUnitState)textureUnitStates[ i ] ).Load();
			}

			// load programs
			if ( this.HasVertexProgram )
			{
				// load vertex program
				_vertexProgramUsage.Load();
			}

			if ( this.HasFragmentProgram )
			{
				// load vertex program
				_fragmentProgramUsage.Load();
			}

			if ( this.HasShadowCasterVertexProgram )
			{
				// load shadow caster vertex program
				shadowCasterVertexProgramUsage.Load();
			}

			if ( this.HasShadowCasterFragmentProgram )
			{
				// load shadow caster fragment program
				_shadowCasterFragmentProgramUsage.Load();
			}

			if ( this.HasShadowReceiverVertexProgram )
			{
				// load shadow receiver vertex program
				_shadowReceiverVertexProgramUsage.Load();
			}

			if ( this.HasShadowReceiverFragmentProgram )
			{
				// load shadow receiver fragment program
				_shadowReceiverFragmentProgramUsage.Load();
			}

			// recalculate hash code
			DirtyHash();
		}

		/// <summary>
		///    Tells the pass that it needs recompilation.
		/// </summary>
		internal void NotifyNeedsRecompile()
		{
			_parent.NotifyNeedsRecompile();
		}

		/// <summary>
		///    Internal method for recalculating the hash code used for sorting passes.
		/// </summary>
		internal void RecalculateHash()
		{
			/* Hash format is 32-bit, divided as follows (high to low bits)
			   bits   purpose
				4     Pass index (i.e. max 16 passes!)
			   14     Hashed texture name from unit 0
			   14     Hashed texture name from unit 1

			   Note that at the moment we don't sort on the 3rd texture unit plus
			   on the assumption that these are less frequently used; sorting on 
			   the first 2 gives us the most benefit for now.
		   */
			_hashCode = ( _index << 28 );
			int count = TextureUnitStageCount;

			// Fix from Multiverse
			//    It fixes a problem that was causing rendering passes for a single material to be executed in the wrong order.
			if ( count > 0 && !( (TextureUnitState)textureUnitStates[ 0 ] ).IsBlank )
			{
				_hashCode += ( ( (TextureUnitState)textureUnitStates[ 0 ] ).TextureName.GetHashCode() & ( ( 1 << 14 ) - 1 ) ) << 14;
			}
			if ( count > 1 && !( (TextureUnitState)textureUnitStates[ 1 ] ).IsBlank )
			{
				_hashCode += ( ( (TextureUnitState)textureUnitStates[ 1 ] ).TextureName.GetHashCode() & ( ( 1 << 14 ) - 1 ) );
			}
		}

		/// <summary>
		///    Removes all texture unit settings from this pass.
		/// </summary>
		public void RemoveAllTextureUnitStates()
		{
			textureUnitStates.Clear();

			if ( !queuedForDeletion )
			{
				// needs recompilation
				_parent.NotifyNeedsRecompile();
			}

			DirtyHash();
		}

		/// <summary>
		///    Removes the specified TextureUnitState from this pass.
		/// </summary>
		/// <param name="state">A reference to the TextureUnitState to remove from this pass.</param>
		public void RemoveTextureUnitState( TextureUnitState state )
		{
			textureUnitStates.Remove( state );

			if ( !queuedForDeletion )
			{
				// needs recompilation
				_parent.NotifyNeedsRecompile();
			}

			DirtyHash();
		}

		/// <summary>
		///    Removes the specified TextureUnitState from this pass.
		/// </summary>
        /// <param name="index">Index of the TextureUnitState to remove from this pass.</param>
		public void RemoveTextureUnitState( int index )
		{
			TextureUnitState state = (TextureUnitState)textureUnitStates[ index ];

			if ( state != null )
				RemoveTextureUnitState( state );
		}

		/// <summary>
		///    Sets the fogging mode applied to this pass.
		/// </summary>
		/// <remarks>
		///    Fogging is an effect that is applied as polys are rendered. Sometimes, you want
		///    fog to be applied to an entire scene. Other times, you want it to be applied to a few
		///    polygons only. This pass-level specification of fog parameters lets you easily manage
		///    both.
		///    <p/>
		///    The SceneManager class also has a SetFog method which applies scene-level fog. This method
		///    lets you change the fog behavior for this pass compared to the standard scene-level fog.
		/// </remarks>
		/// <param name="overrideScene">
		///    If true, you authorise this pass to override the scene's fog params with it's own settings.
		///    If you specify false, so other parameters are necessary, and this is the default behaviour for passs.
		/// </param>
		/// <param name="mode">
		///    Only applicable if <paramref name="overrideScene"/> is true. You can disable fog which is turned on for the
		///    rest of the scene by specifying FogMode.None. Otherwise, set a pass-specific fog mode as
		///    defined in the enum FogMode.
		/// </param>
		/// <param name="color">
		///    The color of the fog. Either set this to the same as your viewport background color,
		///    or to blend in with a skydome or skybox.
		/// </param>
		/// <param name="density">
		///    The density of the fog in FogMode.Exp or FogMode.Exp2 mode, as a value between 0 and 1. 
		///    The default is 0.001.
		/// </param>
		/// <param name="start">
		///    Distance in world units at which linear fog starts to encroach. 
		///    Only applicable if mode is FogMode.Linear.
		/// </param>
		/// <param name="end">
		///    Distance in world units at which linear fog becomes completely opaque.
		///    Only applicable if mode is FogMode.Linear.
		/// </param>
		public void SetFog( bool overrideScene, FogMode mode, ColorEx color, float density, float start, float end )
		{
			_fogOverride = overrideScene;

			// set individual params if overriding scene level fog
			if ( overrideScene )
			{
				_fogMode = mode;
				_fogColor = color;
				_fogDensity = density;
				_fogStart = start;
				_fogEnd = end;
			}
		}

		/// <summary>
		///    Overloaded method.
		/// </summary>
		/// <param name="overrideScene">
		///    If true, you authorise this pass to override the scene's fog params with it's own settings.
		///    If you specify false, so other parameters are necessary, and this is the default behaviour for passs.
		/// </param>
		public void SetFog( bool overrideScene )
		{
			SetFog( overrideScene, FogMode.None, ColorEx.White, 0.001f, 0.0f, 1.0f );
		}

		/// <summary>
		///    Overloaded method.
		/// </summary>
		/// <param name="overrideScene">
		///    If true, you authorise this pass to override the scene's fog params with it's own settings.
		///    If you specify false, so other parameters are necessary, and this is the default behaviour for passs.
		/// </param>
		/// <param name="mode">
		///    Only applicable if <paramref name="overrideScene"/> is true. You can disable fog which is turned on for the
		///    rest of the scene by specifying FogMode.None. Otherwise, set a pass-specific fog mode as
		///    defined in the enum FogMode.
		/// </param>
		public void SetFog( bool overrideScene, FogMode mode )
		{
			SetFog( overrideScene, mode, ColorEx.White, 0.001f, 0.0f, 1.0f );
		}

		/// <summary>
		///    Overloaded method.
		/// </summary>
		/// <param name="overrideScene">
		///    If true, you authorise this pass to override the scene's fog params with it's own settings.
		///    If you specify false, so other parameters are necessary, and this is the default behaviour for passs.
		/// </param>
		/// <param name="mode">
		///    Only applicable if <paramref name="overrideScene"/> is true. You can disable fog which is turned on for the
		///    rest of the scene by specifying FogMode.None. Otherwise, set a pass-specific fog mode as
		///    defined in the enum FogMode.
		/// </param>
		/// <param name="color">
		///    The color of the fog. Either set this to the same as your viewport background color,
		///    or to blend in with a skydome or skybox.
		/// </param>
		public void SetFog( bool overrideScene, FogMode mode, ColorEx color )
		{
			SetFog( overrideScene, mode, color, 0.001f, 0.0f, 1.0f );
		}

		/// <summary>
		///    Overloaded method.
		/// </summary>
		/// <param name="overrideScene">
		///    If true, you authorise this pass to override the scene's fog params with it's own settings.
		///    If you specify false, so other parameters are necessary, and this is the default behaviour for passs.
		/// </param>
		/// <param name="mode">
		///    Only applicable if <paramref name="overrideScene"/> is true. You can disable fog which is turned on for the
		///    rest of the scene by specifying FogMode.None. Otherwise, set a pass-specific fog mode as
		///    defined in the enum FogMode.
		/// </param>
		/// <param name="color">
		///    The color of the fog. Either set this to the same as your viewport background color,
		///    or to blend in with a skydome or skybox.
		/// </param>
		/// <param name="density">
		///    The density of the fog in FogMode.Exp or FogMode.Exp2 mode, as a value between 0 and 1. 
		///    The default is 0.001.
		/// </param>
		public void SetFog( bool overrideScene, FogMode mode, ColorEx color, float density )
		{
			SetFog( overrideScene, mode, color, density, 0.0f, 1.0f );
		}

		/// <summary>
		///    Sets whether or not this pass should be run once per light which 
		///    can affect the object being rendered.
		/// </summary>
		/// <remarks>
		///    The default behavior for a pass (when this option is 'false'), is 
		///    for a pass to be rendered only once, with all the lights which could 
		///    affect this object set at the same time (up to the maximum lights 
		///    allowed in the render system, which is typically 8). 
		///    <p/>
		///    Setting this option to 'true' changes this behavior, such that 
		///    instead of trying to issue render this pass once per object, it 
		///    is run once <b>per light</b> which can affect this object. In 
		///    this case, only light index 0 is ever used, and is a different light 
		///    every time the pass is issued, up to the total number of lights 
		///    which is affecting this object. This has 2 advantages: 
		///    <ul><li>There is no limit on the number of lights which can be 
		///    supported</li> 
		///    <li>It's easier to write vertex / fragment programs for this because 
		///    a single program can be used for any number of lights</li> 
		///    </ul> 
		///    However, this technique is a lot more expensive, and typically you 
		///    will want an additional ambient pass, because if no lights are 
		///    affecting the object it will not be rendered at all, which will look 
		///    odd even if ambient light is zero (imagine if there are lit objects 
		///    behind it - the objects silhouette would not show up). Therefore, 
		///    use this option with care, and you would be well advised to provide 
		///    a less expensive fallback technique for use in the distance. 
		///    <p/>
		///    Note: The number of times this pass runs is still limited by the maximum 
		///    number of lights allowed as set in MaxLights, so 
		///    you will never get more passes than this. 
		/// </remarks>
		/// <param name="enabled">Whether this feature is enabled.</param>
		/// <param name="onlyForOneLightType">
		///    If true, the pass will only be run for a single type of light, other light types will be ignored. 
		/// </param>
		/// <param name="lightType">The single light type which will be considered for this pass.</param>
		public void SetRunOncePerLight( bool enabled, bool onlyForOneLightType, LightType lightType )
		{
			_iteratePerLight = enabled;
			_runOnlyForOneLightType = onlyForOneLightType;
			_onlyLightType = lightType;
		}

		public void SetRunOncePerLight( bool enabled, bool onlyForOneLightType )
		{
			SetRunOncePerLight( enabled, onlyForOneLightType, LightType.Point );
		}

		public void SetRunOncePerLight( bool enabled )
		{
			SetRunOncePerLight( enabled, true );
		}

		/// <summary>
		///    Sets the kind of blending this pass has with the existing contents of the scene.
		/// </summary>
		/// <remarks>
		///    Whereas the texture blending operations seen in the TextureUnitState class are concerned with
		///    blending between texture layers, this blending is about combining the output of the Pass
		///    as a whole with the existing contents of the rendering target. This blending therefore allows
		///    object transparency and other special effects. If all passes in a technique have a scene
		///    blend, then the whole technique is considered to be transparent.
		///    <p/>
		///    This method allows you to select one of a number of predefined blending types. If you require more
		///    control than this, use the alternative version of this method which allows you to specify source and
		///    destination blend factors.
		///    <p/>
		///    This method is applicable for both the fixed-function and programmable pipelines.
		/// </remarks>
		/// <param name="type">One of the predefined SceneBlendType blending types.</param>
		public void SetSceneBlending( SceneBlendType type )
		{
			// convert canned blending types into blending factors
			switch ( type )
			{
				case SceneBlendType.Add:
					SetSceneBlending( SceneBlendFactor.One, SceneBlendFactor.One );
					break;
				case SceneBlendType.TransparentAlpha:
					SetSceneBlending( SceneBlendFactor.SourceAlpha, SceneBlendFactor.OneMinusSourceAlpha );
					break;
				case SceneBlendType.TransparentColor:
					SetSceneBlending( SceneBlendFactor.SourceColor, SceneBlendFactor.OneMinusSourceColor );
					break;
				case SceneBlendType.Modulate:
					SetSceneBlending( SceneBlendFactor.DestColor, SceneBlendFactor.Zero );
					break;
			}
		}

		/// <summary>
		///    Allows very fine control of blending this Pass with the existing contents of the scene.
		/// </summary>
		/// <remarks>
		///    Wheras the texture blending operations seen in the TextureUnitState class are concerned with
		///    blending between texture layers, this blending is about combining the output of the material
		///    as a whole with the existing contents of the rendering target. This blending therefore allows
		///    object transparency and other special effects.
		///    <p/>
		///    This version of the method allows complete control over the blending operation, by specifying the
		///    source and destination blending factors. The result of the blending operation is:
		///    <span align="center">
		///    final = (texture * sourceFactor) + (pixel * destFactor)
		///    </span>
		///    <p/>
		///    Each of the factors is specified as one of a number of options, as specified in the SceneBlendFactor
		///    enumerated type.
		///    <p/>
		///    This method is applicable for both the fixed-function and programmable pipelines.
		/// </remarks>
		/// <param name="src">The source factor in the above calculation, i.e. multiplied by the texture color components.</param>
		/// <param name="dest">The destination factor in the above calculation, i.e. multiplied by the pixel color components.</param>
		public void SetSceneBlending( SceneBlendFactor src, SceneBlendFactor dest )
		{
			// copy settings
			_sourceBlendFactor = src;
			_destinationBlendFactor = dest;
		}

		/// <summary>
		///		
		/// </summary>
		/// <param name="name"></param>
		public void SetFragmentProgram( string name )
		{
			SetFragmentProgram( name, true );
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		/// <param name="resetParams"></param>
		public void SetFragmentProgram( string name, bool resetParams )
		{
			// turn off fragment programs when the name is set to null
			if ( name.Length == 0 )
			{
				_fragmentProgramUsage = null;
			}
			else
			{
				// create a new usage object
				if ( !this.HasFragmentProgram )
				{
					_fragmentProgramUsage = new GpuProgramUsage( GpuProgramType.Fragment, this );
				}

			    _fragmentProgramUsage.SetProgramName( name, resetParams );
			}

			// needs recompilation
			_parent.NotifyNeedsRecompile();
		}

		/// <summary>
		///		
		/// </summary>
		/// <param name="name"></param>
		public void SetGeometryProgram( string name )
		{
			SetGeometryProgram( name, true );
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		/// <param name="resetParams"></param>
		public void SetGeometryProgram( string name, bool resetParams )
		{
			// turn off fragment programs when the name is set to null
			if ( name.Length == 0 )
			{
				_geometryProgramUsage = null;
			}
			else
			{
				// create a new usage object
				if ( !this.HasGeometryProgram )
				{
					_geometryProgramUsage = new GpuProgramUsage( GpuProgramType.Geometry, this );
				}

			    _geometryProgramUsage.SetProgramName( name, resetParams );
			}

			// needs recompilation
			_parent.NotifyNeedsRecompile();
		}

		/// <summary>
		/// </summary>
		public void SetShadowCasterFragmentProgram( string name )
		{
			// turn off fragment programs when the name is set to null
			if ( name.Length == 0 )
			{
				_shadowCasterFragmentProgramUsage = null;
			}
			else
			{
				// create a new usage object
				if ( !this.HasShadowCasterFragmentProgram )
				{
					_shadowCasterFragmentProgramUsage = new GpuProgramUsage( GpuProgramType.Fragment, this );
				}

			    _shadowCasterFragmentProgramUsage.SetProgramName( name );
			}

			// needs recompilation
			_parent.NotifyNeedsRecompile();
		}

		/// <summary>
		/// </summary>
		public void SetShadowReceiverFragmentProgram( string name )
		{
			// turn off fragment programs when the name is set to null
			if ( name.Length == 0 )
			{
				_shadowReceiverFragmentProgramUsage = null;
			}
			else
			{
				// create a new usage object
				if ( !this.HasShadowReceiverFragmentProgram )
				{
					_shadowReceiverFragmentProgramUsage = new GpuProgramUsage( GpuProgramType.Fragment, this );
				}

			    _shadowReceiverFragmentProgramUsage.SetProgramName( name );
			}

			// needs recompilation
			_parent.NotifyNeedsRecompile();
		}

		/// <summary>
		///		
		/// </summary>
		/// <param name="name"></param>
		public void SetVertexProgram( string name )
		{
			SetVertexProgram( name, true );
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		/// <param name="resetParams"></param>
		public void SetVertexProgram( string name, bool resetParams )
		{
			// turn off vertex programs when the name is set to null
			if ( name.Length == 0 )
			{
				_vertexProgramUsage = null;
			}
			else
			{
				// create a new usage object
				if ( !this.HasVertexProgram )
				{
					_vertexProgramUsage = new GpuProgramUsage( GpuProgramType.Vertex, this );
				}

			    _vertexProgramUsage.SetProgramName( name, resetParams );
			}

			// needs recompilation
			_parent.NotifyNeedsRecompile();
		}

		/// <summary>
		/// </summary>
		public void SetShadowCasterVertexProgram( string name )
		{
			// turn off vertex programs when the name is set to null
			if ( name.Length == 0 )
			{
				shadowCasterVertexProgramUsage = null;
			}
			else
			{
				// create a new usage object
				if ( !this.HasShadowCasterVertexProgram )
				{
					shadowCasterVertexProgramUsage = new GpuProgramUsage( GpuProgramType.Vertex, this );
				}

			    shadowCasterVertexProgramUsage.SetProgramName( name );
			}

			// needs recompilation
			_parent.NotifyNeedsRecompile();
		}

		/// <summary>
		/// </summary>
		public void SetShadowReceiverVertexProgram( string name )
		{
			// turn off vertex programs when the name is set to null
			if ( name.Length == 0 )
			{
				_shadowReceiverVertexProgramUsage = null;
			}
			else
			{
				// create a new usage object
				if ( !this.HasShadowReceiverVertexProgram )
				{
					_shadowReceiverVertexProgramUsage = new GpuProgramUsage( GpuProgramType.Vertex, this );
				}

			    _shadowReceiverVertexProgramUsage.SetProgramName( name );
			}

			// needs recompilation
			_parent.NotifyNeedsRecompile();
		}

		/// <summary>
		///    Splits this Pass to one which can be handled in the number of
		///    texture units specified.
		/// </summary>
		/// <param name="numUnits">
		///    The target number of texture units.
		/// </param>
		/// <returns>
		///    A new Pass which contains the remaining units, and a scene_blend
		///    setting appropriate to approximate the multitexture. This Pass will be 
		///    attached to the parent Technique of this Pass.
		/// </returns>
		public Pass Split( int numUnits )
		{
			// can't split programmable passes
			if ( _fragmentProgramUsage != null )
			{
				throw new Exception( "Passes with fragment programs cannot be automatically split.  Define a fallback technique instead" );
			}

			if ( textureUnitStates.Count > numUnits )
			{
				int start = textureUnitStates.Count - numUnits;

				Pass newPass = _parent.CreatePass();

				// get a reference ot the texture unit state at the split position
				TextureUnitState state = (TextureUnitState)textureUnitStates[ start ];

				// set the new pass to fallback using scene blending
				newPass.SetSceneBlending( state.ColorBlendFallbackSource, state.ColorBlendFallbackDest );

				// add the rest of the texture units to the new pass
				for ( int i = start; i < textureUnitStates.Count; i++ )
				{
					state = (TextureUnitState)textureUnitStates[ i ];
					newPass.AddTextureUnitState( state );
				}

				// remove the extra texture units from this pass
				textureUnitStates.RemoveRange( start, textureUnitStates.Count - start );

				return newPass;
			}

			return null;
		}

		/// <summary>
		///    Internal method for unloaded this pass.
		/// </summary>
		internal void Unload()
		{
			// load each texture unit state
			for ( int i = 0; i < textureUnitStates.Count; i++ )
			{
				( (TextureUnitState)textureUnitStates[ i ] ).Unload();
			}

			if ( this.HasFragmentProgram )
				this._fragmentProgramUsage.Program.Unload();

			if ( this.HasVertexProgram )
				this._vertexProgramUsage.Program.Unload();
		}

		/// <summary>
		///		Mark the hash for this pass as dirty.	
		/// </summary>
		public void DirtyHash()
		{
			_dirtyList.Add( this );
		}

		/// <summary>
		///		Queue this pass for deletion when appropriate.
		/// </summary>
		public void QueueForDeletion()
		{
			queuedForDeletion = true;

			RemoveAllTextureUnitStates();

			// remove from the dirty list
			_dirtyList.Remove( this );

			_graveyardList.Add( this );
		}

		/// <summary>
		///		Process all dirty and pending deletion passes.
		/// </summary>
		public static void ProcessPendingUpdates()
		{
			// clear the graveyard
			_graveyardList.Clear();

			// recalc the hashcode for each pass
			for ( int i = 0; i < _dirtyList.Count; i++ )
			{
				Pass pass = (Pass)_dirtyList[ i ];
				pass.RecalculateHash();
			}

			// clear out the dirty list
			_dirtyList.Clear();
		}

		public bool ApplyTextureAliases( Dictionary<string, string> aliasList, bool apply )
		{
			// iterate through all TextureUnitStates and apply texture aliases
			bool testResult = false;

			foreach ( TextureUnitState tus in textureUnitStates )
			{
				if ( tus.ApplyTextureAliases( aliasList, apply ) )
					testResult = true;
			}

			return testResult;

		}

		#endregion

		#region Object overrides

		/// <summary>
		///    Gets the 'hash' of this pass, ie a precomputed number to use for sorting.
		/// </summary>
		/// <remarks>
		///    This hash is used to sort passes, and for this reason the pass is hashed
		///    using firstly its index (so that all passes are rendered in order), then
		///    by the textures which it's TextureUnitState instances are using.
		/// </remarks>
		/// <returns></returns>
		public override int GetHashCode()
		{
			return _hashCode;
		}

		#endregion Object overrides

        [OgreVersion(1, 7, 2790)]
	    public void UpdateAutoParams( AutoParamDataSource source, GpuProgramParameters.GpuParamVariability mask )
	    {
            if (HasVertexProgram)
            {
                // Update vertex program auto params
                _vertexProgramUsage.Parameters.UpdateAutoParams( source, mask );
            }

            if (HasGeometryProgram)
            {
                // Update geometry program auto params
                _geometryProgramUsage.Parameters.UpdateAutoParams(source, mask);
            }

            if (HasFragmentProgram)
            {
                // Update fragment program auto params
                _fragmentProgramUsage.Parameters.UpdateAutoParams(source, mask);
            }
	    }
	}

	/// <summary>
	///		Struct recording a pass which can be used for a specific illumination stage.
	/// </summary>
	/// <remarks>
	///		This structure is used to record categorized passes which fit into a 
	///		number of distinct illumination phases - ambient, diffuse / specular 
	///		(per-light) and decal (post-lighting texturing).
	///		An original pass may fit into one of these categories already, or it
	///		may require splitting into its component parts in order to be categorized 
	///		properly.
	/// </remarks>
	public struct IlluminationPass
	{
		/// <summary>
		///		The stage at which this pass is relevant.
		/// </summary>
		public IlluminationStage Stage;
		/// <summary>
		///		The pass to use in this stage.
		/// </summary>
		public Pass Pass;
		/// <summary>
		///		Whether this pass is one which should be deleted itself.
		/// </summary>
		public bool DestroyOnShutdown;
		/// <summary>
		///		The original pass which spawned this one.
		/// </summary>
		public Pass OriginalPass;
	}
}
