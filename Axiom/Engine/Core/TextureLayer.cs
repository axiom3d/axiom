#region LGPL License
/*
Axiom Game Engine Library
Copyright (C) 2003  Axiom Project Team

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
using System.Collections;
using System.Diagnostics;
using Axiom.Controllers;
using Axiom.Enumerations;
using Axiom.MathLib;
using Axiom.SubSystems.Rendering;

namespace Axiom.Core
{
	/// <summary>
	/// Summary description for TextureLayer.
	/// </summary>
	public class TextureLayer : ICloneable
	{
		#region Member variables

		public const int MAX_ANIMATION_FRAMES = 32;

		/// <summary>Name of the texture for this layer.</summary>
		protected string textureName;
		/// <summary></summary>
		protected bool deferredLoad;
		/// <summary></summary>
		protected int texCoordSet;
		protected TextureAddressing texAddressingMode;
		protected LayerBlendModeEx colorBlendMode = new LayerBlendModeEx();
		protected LayerBlendModeEx alphaBlendMode = new LayerBlendModeEx();
		protected SceneBlendFactor colorBlendFallbackSrc;
		protected SceneBlendFactor colorBlendFallbackDest;
		protected LayerBlendOperation colorOp;
		protected EnvironmentMap envMap;

		/// <summary>Is this a blank layer?</summary>
		protected bool isBlank;
		/// <summary>Number of frames for this layer.</summary>
		protected int numFrames;
		protected int currentFrame;
		/// <summary>store names of textures for animation frames.</summary>
		protected String[] frames = new String[MAX_ANIMATION_FRAMES];
		protected bool isCubic;

		// texture animation parameters
		protected bool recalcTexMatrix;
		protected float transU;
		protected float transV;
		protected float scaleU;
		protected float scaleV;
		protected float rotate;
		protected Matrix4 texMatrix = Matrix4.Identity;
		protected float scrollAnimU;
		protected float scrollAnimV;
		protected float rotateAnim;

		// TODO: make this a hashtable to something else with type as a key
		// .Net doesnt have a nonunique key hashtable like structure
		protected ArrayList effectList = new ArrayList();

		#endregion

		#region Constructors

		/// <summary>
		///		Default constructor.
		/// </summary>
		public TextureLayer()
		{
			this.deferredLoad = false;

			isBlank = true;

			colorBlendMode.blendType = LayerBlendType.Color;
			SetColorOperation(LayerBlendOperation.Modulate);
			TextureAddressing = TextureAddressing.Wrap;

			alphaBlendMode.operation = LayerBlendOperationEx.Modulate;
			alphaBlendMode.blendType = LayerBlendType.Alpha;
			alphaBlendMode.source1 = LayerBlendSource.Texture;
			alphaBlendMode.source2 = LayerBlendSource.Current;

		}

		/// <summary>
		///		Basic constructor.
		/// </summary>
		/// <param name="deferred"></param>
		public TextureLayer(bool deferred)
		{
			this.deferredLoad = deferred;

			// default to wrapping
			texAddressingMode = TextureAddressing.Wrap;

			isBlank = true;

			colorBlendMode.blendType = LayerBlendType.Color;
			SetColorOperation(LayerBlendOperation.Modulate);
			TextureAddressing = TextureAddressing.Wrap;

			alphaBlendMode.operation = LayerBlendOperationEx.Modulate;
			alphaBlendMode.blendType = LayerBlendType.Alpha;
			alphaBlendMode.source1 = LayerBlendSource.Texture;
			alphaBlendMode.source2 = LayerBlendSource.Current;
		}

		#endregion

		#region Properties

		/// <summary>
		///		Gets/Sets the name of the texture for this texture layer.
		/// </summary>
		public String TextureName
		{
			get { return frames[currentFrame]; }
			set 
			{ 
				frames[0] = value; 
				numFrames = 1;
				currentFrame = 0;
				isCubic = false;
				
				if(value.Length == 0)
					isBlank = true;
				else if(!deferredLoad)
				{
					if(TextureManager.Instance[value] != null)
					{
						isBlank = false;
						return;
					}

					// load the texture
					TextureManager.Instance.Load(value);

					isBlank = false;
				}
			}
		}

		/// <summary>
		///		Gets/Sets whether or not the texture loading should be deferred.
		/// </summary>
		public bool DeferredLoad
		{
			get { return deferredLoad; }
			set { deferredLoad = value; }
		}

		/// <summary>
		///		Gets/Sets the texture coordinate set to be used by this texture layer.
		/// </summary>
		public int TexCoordSet
		{
			get { return texCoordSet; }
			set { texCoordSet = value; }
		}

		/// <summary>
		///		Get/Set the texture addressing mode for this layer.
		/// </summary>
		public TextureAddressing TextureAddressing
		{
			get { return texAddressingMode; }
			set { texAddressingMode = value; }
		}

		/// <summary>
		///		Gets a structure that describes the layer blending mode parameters.
		/// </summary>
		public LayerBlendModeEx ColorBlendMode
		{
			get { return colorBlendMode; }
		}

		/// <summary>
		///		Gets a structure that describes the layer blending mode parameters.
		/// </summary>
		public LayerBlendModeEx AlphaBlendMode
		{
			get { return alphaBlendMode; }
		}

		public SceneBlendFactor ColorBlendFallbackSource
		{
			get { return colorBlendFallbackSrc; }
		}

		public SceneBlendFactor ColorBlendFallbackDest
		{
			get { return colorBlendFallbackDest; }
		}

		public LayerBlendOperation ColorOperation
		{
			get { return colorOp; }
		}

		/// <summary>
		///		Gets/Sets whether this layer is blank or not.
		/// </summary>
		public bool Blank
		{
			get 
			{ 
				//if(isBlank)
					//Console.WriteLine("Get Value: " + this.TextureName); 
				return isBlank;  
			}
			set { isBlank = value; }
		}

		/// <summary>
		///		Gets/Sets the current frame this texture layer should be using.
		/// </summary>
		public int CurrentFrame
		{
			get { return currentFrame; }
			set 
			{
				Debug.Assert(value < numFrames, "Cannot set the current frame of a texture layer to be greater than the number of frames in the layer.");
				currentFrame = value;
			}
		}

		/// <summary>
		///		
		/// </summary>
		public int NumFrames
		{
			get { return numFrames; }
		}

		/// <summary>
		///		Gets/Sets the Matrix4 that represents transformation to the texture in this
		///		layer.
		/// </summary>
		public Matrix4 TextureMatrix
		{
			get
			{
				// update the matrix before returning it if necessary
				if(recalcTexMatrix)
					recalcTextureMatrix();
				return texMatrix;
			}
			set
			{
				texMatrix = value;
				recalcTexMatrix = false;
			}
		}

		/// <summary>
		///		Allow access to the list of this layers effects.
		/// </summary>
		public IList Effects
		{
			get { return effectList; }
		}

		#endregion

		#region Methods

		/// <summary>
		/// 
		/// </summary>
		/// <param name="textureName"></param>
		/// <param name="forUVW"></param>
		public void SetCubicTexture(string textureName, bool forUVW)
		{
			string[] postfixes = {"_fr", "_bk", "_lf", "_rt", "_up", "_dn"};
			string[] fullNames = new string[6];
			string baseName;
			string ext;

			int pos = textureName.LastIndexOf(".");

			baseName = textureName.Substring(0, pos);
			ext = textureName.Substring(pos);

			for(int i = 0; i < 6; i++)
			{
				fullNames[i] = baseName + postfixes[i] + ext;
			}

			SetCubicTexture(fullNames, forUVW);
		}

		public void SetColorOpMultipassFallback(SceneBlendFactor src, SceneBlendFactor dest)
		{
			this.colorBlendFallbackSrc = src;
			this.colorBlendFallbackDest = dest;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="textureNames"></param>
		/// <param name="forUVW"></param>
		public void SetCubicTexture(string[] textureNames, bool forUVW)
		{
			if(forUVW)
			{
				// TODO: single subic textures, rather than 6 seperate ones
			}
			else
			{
				numFrames = 6;
				currentFrame = 0;
				isCubic = true;

				for(int i = 0; i < 6; i++)
				{
					frames[i] = textureNames[i];

					if(!deferredLoad)
					{
						try
						{
							// ensure texture is loaded
							TextureManager.Instance.Load(frames[i]);
							isBlank = false;
						}
						catch(Exception ex)
						{
							System.Diagnostics.Trace.WriteLine(String.Format("Error loading texture {0}.  Texture layer will be left blank.", frames[i]));
							isBlank = true;
						}
					}
				}
			}
		}

		/// <summary>
		///		
		/// </summary>
		/// <param name="operation"></param>
		public void SetColorOperation(LayerBlendOperation operation)
		{
			colorOp = operation;

			// configure the multitexturing operations
			switch(operation)
			{
				case LayerBlendOperation.Replace:
					SetColorOperationEx(LayerBlendOperationEx.Source1, LayerBlendSource.Texture, LayerBlendSource.Current);
					SetColorOpMultipassFallback(SceneBlendFactor.One, SceneBlendFactor.Zero);
					break;

				case LayerBlendOperation.Add:
					SetColorOperationEx(LayerBlendOperationEx.Add, LayerBlendSource.Texture, LayerBlendSource.Current);
					SetColorOpMultipassFallback(SceneBlendFactor.One, SceneBlendFactor.One);
					break;

				case LayerBlendOperation.Modulate:
					SetColorOperationEx(LayerBlendOperationEx.Modulate, LayerBlendSource.Texture, LayerBlendSource.Current);
					SetColorOpMultipassFallback(SceneBlendFactor.DestColor, SceneBlendFactor.Zero);
					break;

				case LayerBlendOperation.AlphaBlend:
					SetColorOperationEx(LayerBlendOperationEx.BlendTextureAlpha, LayerBlendSource.Texture, LayerBlendSource.Current);
					SetColorOpMultipassFallback(SceneBlendFactor.SourceAlpha, SceneBlendFactor.OneMinusSourceAlpha);
					break;
			}
		}

		/// <summary>
		///		
		/// </summary>
		/// <param name="operation"></param>
		/// <param name="source1"></param>
		/// <param name="source2"></param>
		public void SetColorOperationEx(LayerBlendOperationEx operation, LayerBlendSource source1, LayerBlendSource source2)
		{
			SetColorOperationEx(operation, source1, source2, ColorEx.FromColor(System.Drawing.Color.White), ColorEx.FromColor(System.Drawing.Color.White), 0.0f);
		}

		/// <summary>
		///		
		/// </summary>
		/// <param name="operation"></param>
		/// <param name="source1"></param>
		/// <param name="source2"></param>
		public void SetColorOperationEx(LayerBlendOperationEx operation, LayerBlendSource source1, LayerBlendSource source2, ColorEx arg1, ColorEx arg2, float blendFactor)
		{
			colorBlendMode.operation = operation;
			colorBlendMode.source1 = source1;
			colorBlendMode.source2 = source2;
			colorBlendMode.colorArg1 = arg1;
			colorBlendMode.colorArg2 = arg2;
			colorBlendMode.blendFactor = blendFactor;
		}

		/// <summary>
		///		
		/// </summary>
		/// <param name="operation"></param>
		/// <param name="source1"></param>
		/// <param name="source2"></param>
		public void SetAlphaOperation(LayerBlendOperationEx operation)
		{
			SetAlphaOperation(operation, LayerBlendSource.Texture, LayerBlendSource.Current, 1.0f, 1.0f, 0.0f);
		}

		/// <summary>
		///		
		/// </summary>
		/// <param name="operation"></param>
		/// <param name="source1"></param>
		/// <param name="source2"></param>
		public void SetAlphaOperation(LayerBlendOperationEx operation, LayerBlendSource source1, LayerBlendSource source2, float arg1, float arg2, float blendFactor)
		{
			colorBlendMode.operation = operation;
			colorBlendMode.source1 = source1;
			colorBlendMode.source2 = source2;
			colorBlendMode.alphaArg1 = arg1;
			colorBlendMode.alphaArg2 = arg2;
			colorBlendMode.blendFactor = blendFactor;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="enable"></param>
		/// <param name="envMap"></param>
		public void SetEnvironmentMap(bool enable)
		{
			// call with Curved as the default value
			SetEnvironmentMap(enable, EnvironmentMap.Curved);
		}	

		/// <summary>
		/// 
		/// </summary>
		/// <param name="enable"></param>
		/// <param name="envMap"></param>
		public void SetEnvironmentMap(bool enable, EnvironmentMap envMap)
		{
			if(enable)
			{
				TextureEffect effect = new TextureEffect();
				effect.type = TextureEffectType.EnvironmentMap;
				effect.subtype = envMap;
				AddEffect(ref effect);
			}
			else
			{
				// remove it from the list
				RemoveEffect(TextureEffectType.EnvironmentMap);
			}
		}	

		/// <summary>
		/// 
		/// </summary>
		/// <param name="frame"></param>
		/// <returns></returns>
		public string GetFrameTextureName(int frame)
		{
			Debug.Assert(frame < numFrames, "Attempted to access a frame which is out of range.");
			return frames[frame];
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="u"></param>
		/// <param name="v"></param>
		public void SetTextureScroll(float u, float v)
		{
			transU = u;
			transV = v;
			recalcTexMatrix = true;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="u"></param>
		public void SetTextureScrollU(float u)
		{
			transU = u;
			recalcTexMatrix = true;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="v"></param>
		public void SetTextureScrollV(float v)
		{
			transV = v;
			recalcTexMatrix = true;
		}

		/// <summary>
		///		Creates a scrolling animation with the specified speed for u and v.
		/// </summary>
		/// <param name="u"></param>
		/// <param name="v"></param>
		public void SetScrollAnimation(float uSpeed, float vSpeed)
		{
			TextureEffect effect = new TextureEffect();
			effect.type = TextureEffectType.Scroll;
			effect.arg1 = uSpeed;
			effect.arg2 = vSpeed;

			AddEffect(ref effect);
		}

		/// <summary>
		///		Creates a rotating animation with the specified speed.
		/// </summary>
		/// <param name="u"></param>
		/// <param name="v"></param>
		public void SetRotateAnimation(float speed)
		{
			TextureEffect effect = new TextureEffect();
			effect.type = TextureEffectType.Rotate;
			effect.arg1 = speed;

			AddEffect(ref effect);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="type"></param>
		/// <param name="waveType"></param>
		/// <param name="baseVal"></param>
		/// <param name="frequenct"></param>
		/// <param name="phase"></param>
		/// <param name="amplitude"></param>
		public void SetTransformAnimation(TextureTransform transType, WaveformType waveType, 
			float baseVal, float frequency, float phase, float amplitude)
		{
			TextureEffect effect = new TextureEffect();
			effect.type = TextureEffectType.Transform;
			effect.subtype = transType;
			effect.waveType = waveType;
			effect.baseVal = baseVal;
			effect.frequency = frequency;
			effect.phase = phase;
			effect.amplitude = amplitude;

			AddEffect(ref effect);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="u"></param>
		/// <param name="v"></param>
		public void SetTextureScale(float u, float v)
		{
			scaleU = u;
			scaleV = v;
			recalcTexMatrix = true;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="u"></param>
		public void SetTextureScaleU(float u)
		{
			scaleU = u;
			recalcTexMatrix = true;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="v"></param>
		public void SetTextureScaleV(float v)
		{
			scaleV = v;
			recalcTexMatrix = true;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="degrees"></param>
		public void SetTextureRotate(float degrees)
		{
			rotate = degrees;
			recalcTexMatrix = true;
		}

		/// <summary>
		///		Used to update the texture matrix if need be.
		/// </summary>
		private void recalcTextureMatrix()
		{
			Matrix3 xform = Matrix3.Identity;

			// texture scaling
			if(scaleU > 0 || scaleV > 0)
			{
				// offset to the center of the texture
				xform.m00 = 1 / scaleU;
				xform.m11 = 1 / scaleV;

				// skip matrix mult since first matrix update
				xform.m02 = (-0.5f * xform.m00) + 0.5f;
				xform.m12 = (-0.5f * xform.m11) + 0.5f;
			}

			// texture translation
			if(transU > 0 || transV > 0)
			{
				Matrix3 xlate = Matrix3.Identity;

				xlate.m02 = transU;
				xlate.m12 = transV;

				// multiplt the transform by the translation
				xform = xlate * xform;
			}

			if(rotate != 0.0f)
			{
				Matrix3 rotation = Matrix3.Identity;

				float theta = MathUtil.DegreesToRadians(rotate);
				float cosTheta = MathUtil.Cos(theta);
				float sinTheta = MathUtil.Sin(theta);

				// set the rotation portion of the matrix
				rotation.m00 = cosTheta;
				rotation.m01 = -sinTheta;
				rotation.m10 = sinTheta;
				rotation.m11 = cosTheta;
 
				// offset the center of rotation to the center of the texture
				float cosThetaOff = cosTheta * -0.5f;
				float sinThetaOff = sinTheta * -0.5f;
				rotation.m02 = cosThetaOff - sinThetaOff;
				rotation.m12 = sinThetaOff + cosThetaOff;

				// multiply the rotation and transformation matrices
				xform = xform * rotation;
			}

			// store the transformation into the local texture matrix
			texMatrix = xform;

		}

		/// <summary>
		///		Used internally to add a new effect to this texture layer.
		/// </summary>
		/// <param name="effect"></param>
		private void AddEffect(ref TextureEffect effect)
		{
			effect.controller = null;

			// these effects must be unique, so remove any existing
			if(effect.type == TextureEffectType.EnvironmentMap ||
				effect.type == TextureEffectType.Scroll ||
				effect.type == TextureEffectType.Rotate)
			{
				for(int i = 0; i < effectList.Count; i++)
				{
					if(((TextureEffect)effectList[i]).type == effect.type)
					{
						effectList.RemoveAt(i);
						break;
					}
				}// for
			}

			// create controller
			if(!deferredLoad)
				CreateEffectController(ref effect);

			// add to internal list
			effectList.Add(effect);
		}

		/// <summary>
		///		Removes effects of the specified type from this layers effect list.
		/// </summary>
		/// <param name="type"></param>
		private void RemoveEffect(TextureEffectType type)
		{
			// TODO: Verify this works correctly since we are removing items during a loop
			for(int i = 0; i < effectList.Count; i++)
			{
				if(((TextureEffect)effectList[i]).type == type)
					effectList.RemoveAt(i);
			}
		}

		/// <summary>
		///		Used internally to create a new controller for this layer given the requested effect.
		/// </summary>
		/// <param name="effect"></param>
		private void CreateEffectController(ref TextureEffect effect)
		{
			// get a reference to the singleton controller manager
			ControllerManager cMgr = ControllerManager.Instance;

			// create an appropriate controller based on the specified animation
			switch(effect.type)
			{
				case TextureEffectType.Scroll:
					effect.controller = cMgr.CreateTextureScroller(this, effect.arg1, effect.arg2);
					break;

				case TextureEffectType.Rotate:
					effect.controller = cMgr.CreateTextureRotator(this, effect.arg1);
					break;

				case TextureEffectType.Transform:
					effect.controller = cMgr.CreateTextureWaveTransformer(
						this, 
						(TextureTransform)effect.subtype,
						effect.waveType,
						effect.baseVal,
						effect.frequency,
						effect.phase,
						effect.amplitude);

					break;

				case TextureEffectType.EnvironmentMap:
				case TextureEffectType.BumpMap:
					break;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		internal void Load()
		{
			for(int i = 0; i < numFrames; i++)
			{
				if(frames[i] != String.Empty)
				{
					// force a load of the texture
					TextureManager.Instance.Load(frames[i]);
					isBlank = false;
				}
			}

			// TODO: Init texture effects here
		}

		#endregion

		#region ICloneable Members

		/// <summary>
		///		Used to clone a texture layer.  Mainly used during a call to Clone on a Material.
		/// </summary>
		/// <returns></returns>
		public object Clone()
		{
			TextureLayer newLayer = (TextureLayer)this.MemberwiseClone();
			newLayer.colorBlendMode = colorBlendMode;
			newLayer.alphaBlendMode = alphaBlendMode;

			return newLayer;
		}
		#endregion
	}

	/// <summary>
	///		Utility class for handling texture layer blending parameters.
	/// </summary>
	public class LayerBlendModeEx
	{
		public LayerBlendType blendType = LayerBlendType.Color;
		public LayerBlendOperationEx operation;
		public LayerBlendSource source1;
		public LayerBlendSource source2;
		public ColorEx colorArg1 = ColorEx.FromColor(System.Drawing.Color.White);
		public ColorEx colorArg2 = ColorEx.FromColor(System.Drawing.Color.White);
		public float alphaArg1 = 1.0f;
		public float alphaArg2 = 1.0f;
		public float blendFactor;

		/// <summary>
		///		Compares to blending modes for equality.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		static public bool operator == (LayerBlendModeEx left, LayerBlendModeEx right)
		{
			if(left.colorArg1 != right.colorArg1 ||
				left.colorArg2 != right.colorArg2 ||
				left.blendFactor != right.blendFactor ||
				left.source1 != right.source1 ||
				left.source2 != right.source2 ||
				left.operation != right.operation)
			{
				return false;
			}
			else
			{
				return true;
			}
		}

		/// <summary>
		///		Compares to blending modes for inequality.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		static public bool operator != (LayerBlendModeEx left, LayerBlendModeEx right)
		{
			if(left.blendType != right.blendType)
				return false;

			if(left.blendType == LayerBlendType.Color)
			{
				if(left.colorArg1 != right.colorArg1 ||
					left.colorArg2 != right.colorArg2 ||
					left.blendFactor != right.blendFactor ||
					left.source1 != right.source1 ||
					left.source2 != right.source2 ||
					left.operation != right.operation)
				{
					return true;
				}
			}
			else
			{
				if(left.alphaArg1 != right.alphaArg1 ||
					left.alphaArg2 != right.alphaArg2 ||
					left.blendFactor != right.blendFactor ||
					left.source1 != right.source1 ||
					left.source2 != right.source2 ||
					left.operation != right.operation)
				{
					return true;
				}
			}

			return false;
		}

	}

	/// <summary>
	///		Internal structure used for defining a texture effect.
	/// </summary>
	struct TextureEffect 
	{
		public TextureEffectType type;
		public System.Enum subtype;
		public float arg1, arg2;
		public WaveformType waveType;
		public float baseVal;
		public float frequency;
		public float phase;
		public float amplitude;
		public Controller controller;
	};
}
