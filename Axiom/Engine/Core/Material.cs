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
using System.Diagnostics;
using System.Drawing;
using System.IO;
using Axiom.Core;
using Axiom.Configuration;
using Axiom.SubSystems.Rendering;

namespace Axiom.Core
{
	/// <summary>
	/// An object that contains texture data and information on how it is rendered.
	/// </summary>
	// INC: In progress
	public class Material : Resource, IComparable
	{
		#region Member variables

		/// <summary></summary>
		protected bool isTransparent;
		/// <summary></summary>
		protected int numTextureLayers;
		/// <summary></summary>
		protected TextureLayer[] textureLayers = new TextureLayer[Config.MaxTextureLayers];

		/// <summary></summary>
		protected ColorEx ambient;
		/// <summary></summary>
		protected ColorEx diffuse;
		/// <summary></summary>
		protected ColorEx specular;
		/// <summary></summary>
		protected ColorEx emissive;
		/// <summary></summary>
		protected float shininess;

		/// <summary></summary>
		protected SceneBlendFactor sourceBlendFactor;
		/// <summary></summary>
		protected SceneBlendFactor destBlendFactor;
		/// <summary></summary>
		protected bool depthCheck;
		/// <summary></summary>
		protected bool depthWrite;
		/// <summary></summary>
		protected CompareFunction depthFunc;
		/// <summary></summary>
		protected ushort depthBias;

		/// <summary></summary>
		protected CullingMode cullMode;
		/// <summary></summary>
		protected ManualCullingMode manualCullMode;

		/// <summary></summary>
		protected bool lightingEnabled;

		/// <summary>Shading option.</summary>
		protected Shading shading;

		/// <summary></summary>
		protected TextureFiltering textureFiltering;

		/// <summary></summary>
		protected bool fogOverride;
		/// <summary></summary>
		protected FogMode fogMode;
		/// <summary></summary>
		protected ColorEx fogColor;
		/// <summary></summary>
		protected float fogStart;
		/// <summary></summary>
		protected float fogEnd;
		/// <summary></summary>
		protected float fogDensity;
		/// <summary></summary>
		protected bool deferLoad;

		/// <summary></summary>
		static protected Material defaultSettings;

		#endregion

		#region Constructors
		
		public Material(string name)
		{
			this.name = name;

			for(int i = 0; i < textureLayers.Length; i++)
				textureLayers[i] = new TextureLayer();

			// set defaults for the surface properties
			ambient = ColorEx.FromColor(Color.White);
			diffuse = ColorEx.FromColor(Color.White);
			specular = ColorEx.FromColor(Color.Black);
			emissive = ColorEx.FromColor(Color.Black);
			shininess = 0;

			// default values
			textureFiltering = TextureFiltering.Bilinear;
			lightingEnabled = true;
			sourceBlendFactor = SceneBlendFactor.One;
			destBlendFactor = SceneBlendFactor.Zero;
			depthCheck = true;
			depthWrite = true;
			depthFunc = CompareFunction.LessEqual;
			depthBias = 0;

			// fog defaults
			fogMode = FogMode.None;
			fogColor = ColorEx.FromColor(Color.White);

		}

		public Material(string name, bool deferLoad)
		{
			this.name = name;
			this.deferLoad = deferLoad;

			for(int i = 0; i < textureLayers.Length; i++)
				textureLayers[i] = new TextureLayer();

			// set defaults for the surface properties
			ambient = ColorEx.FromColor(Color.White);
			diffuse = ColorEx.FromColor(Color.White);
			specular = ColorEx.FromColor(Color.Black);
			emissive = ColorEx.FromColor(Color.Black);
			shininess = 0;

			// default values
			textureFiltering = TextureFiltering.Bilinear;
			lightingEnabled = true;
			sourceBlendFactor = SceneBlendFactor.One;
			destBlendFactor = SceneBlendFactor.Zero;
			depthCheck = true;
			depthWrite = true;
			depthFunc = CompareFunction.LessEqual;
			depthBias = 0;

			// fog defaults
			fogMode = FogMode.None;
			fogColor = ColorEx.FromColor(Color.White);
		}

		#endregion

		#region Properties

		/// <summary>
		///		
		/// </summary>
		public bool IsTransparent
		{
			get 
			{ 
				if (sourceBlendFactor != SceneBlendFactor.One || destBlendFactor != SceneBlendFactor.Zero)
					return true;
				else
					return false;
			}
		}

		/// <summary>
		///		
		/// </summary>
		public int NumTextureLayers
		{
			get { return numTextureLayers; }
		}

		/// <summary>
		///		Ambient color of this material.
		/// </summary>
		public ColorEx Ambient
		{
			get { return ambient; }
			set { ambient = value; }
		}

		/// <summary>
		///		Diffuse color of this material.
		/// </summary>
		public ColorEx Diffuse
		{
			get { return diffuse; }
			set { diffuse = value; }
		}

		/// <summary>
		///		Specular color of this material.
		/// </summary>
		public ColorEx Specular
		{
			get { return specular; }
			set { specular = value; }
		}

		/// <summary>
		///		Emissive color of this material.
		/// </summary>
		public ColorEx Emissive
		{
			get { return emissive; }
			set { emissive = value; }
		}

		/// <summary>
		///		Shininess value of this material.
		/// </summary>
		public float Shininess
		{
			get { return shininess; }
			set { shininess = value; }
		}

		/// <summary>
		///		
		/// </summary>
		public TextureLayer[] TextureLayers
		{
			get { return textureLayers; }
		}

		/// <summary>
		///		
		/// </summary>
		public TextureFiltering TextureFiltering
		{
			get { return textureFiltering; }
			set { textureFiltering = value; }
		}

		/// <summary>
		/// 
		/// </summary>
		public bool Lighting
		{
			get { return lightingEnabled; }
			set { lightingEnabled = value; }
		}

		/// <summary>
		/// 
		/// </summary>
		public SceneBlendFactor SourceBlendFactor
		{
			get { return sourceBlendFactor; }
		}

		/// <summary>
		/// 
		/// </summary>
		public SceneBlendFactor DestBlendFactor
		{
			get { return destBlendFactor; }
		}

		/// <summary>
		/// 
		/// </summary>
		public bool DepthWrite
		{
			get { return depthWrite; }
			set { depthWrite = value; }
		}

		/// <summary>
		/// 
		/// </summary>
		public bool DepthCheck
		{
			get { return depthCheck; }
			set { depthCheck = value; }
		}

		/// <summary>
		/// 
		/// </summary>
		public bool FogOverride
		{
			get { return fogOverride; }
			set { fogOverride = value; }
		}

		/// <summary>
		///		Gets the fog mode that was set during the last call to SetFog.
		/// </summary>
		public FogMode FogMode
		{
			get { return fogMode; }
			set { fogMode = value; }
		}

		/// <summary>
		///		Gets the fog starting point that was set during the last call to SetFog.
		/// </summary>
		public float FogStart
		{
			get { return fogStart; }
			set { fogStart = value; }
		}

		/// <summary>
		///		Gets the fog ending point that was set during the last call to SetFog.
		/// </summary>
		public float FogEnd
		{
			get { return fogEnd; }
			set { fogEnd = value; }
		}

		/// <summary>
		///		Gets the fog density that was set during the last call to SetFog.
		/// </summary>
		public float FogDensity
		{
			get { return fogDensity; }
			set { fogDensity = value; }
		}

		/// <summary>
		///		Gets the fog color that was set during the last call to SetFog.
		/// </summary>
		public ColorEx FogColor
		{
			get { return fogColor; }
			set { fogColor = value; }
		}

		#endregion

		#region Methods

		/// <summary>
		/// 
		/// </summary>
		/// <param name="type"></param>
		public void SetSceneBlending(SceneBlendType type)
		{
			// convert canned blending types into blending factors
			switch(type)
			{
				case SceneBlendType.Add:
					SetSceneBlending(SceneBlendFactor.One, SceneBlendFactor.One);
					break;
				case SceneBlendType.TransparentAlpha:
					SetSceneBlending(SceneBlendFactor.SourceAlpha, SceneBlendFactor.SourceAlpha);
					break;
				case SceneBlendType.TransparentColor:
					SetSceneBlending(SceneBlendFactor.SourceColor, SceneBlendFactor.SourceColor);
					break;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="src"></param>
		/// <param name="dest"></param>
		public void SetSceneBlending(SceneBlendFactor src, SceneBlendFactor dest)
		{
			// copy settings
			sourceBlendFactor = src;
			destBlendFactor = dest;

			if(src == SceneBlendFactor.One && dest == SceneBlendFactor.Zero ||
				src == SceneBlendFactor.Zero && dest == SceneBlendFactor.One)
			{
				// solid, do nothing yet
			}
			else
			{
				// transparent, so disable the depth buffer for this material
				// without this, things like particles will overlay each other and look odd at some
				// camera angles
				depthWrite = false;
			}
		}

		/// <summary>
		///		Internal helper method for comparing the surface parameters of 2 materials.
		/// </summary>
		/// <param name="cmp">Materials to compare to this one.</param>
		/// <returns></returns>
		internal bool CompareSurfaceParams(Material cmp)
		{
			if (ambient != cmp.ambient || diffuse != cmp.diffuse ||
				specular != cmp.specular || emissive != cmp.emissive ||
				shininess != cmp.shininess)
			{
				return false;
			}
			else
			{
				return true;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="textureName"></param>
		/// <returns></returns>
		public TextureLayer AddTextureLayer(String textureName)
		{
			// default to tex coord set of 0
			return AddTextureLayer(textureName, 0);
		}

		/// <summary>
		///		
		/// </summary>
		/// <param name="textureName"></param>
		/// <param name="stage"></param>
		public TextureLayer AddTextureLayer(String textureName, int texCoordSet)
		{
			textureLayers[numTextureLayers].DeferredLoad = deferLoad;
			textureLayers[numTextureLayers].TextureName = textureName;
			textureLayers[numTextureLayers].TexCoordSet = texCoordSet;
			// TODO: Add per layer texture filtering

			return textureLayers[numTextureLayers++];
		}

		#endregion

		#region Implementation of IComparable

		/// <summary>
		///		Used for comparing 2 Material objects.
		/// </summary>
		/// <remarks>
		///		This comparison will be used in RenderQueue group sorting of Materials materials.
		///		If this object is transparent and the object being compared is not, this is greater that obj.
		///		If this object is not transparent and the object being compared is, obj is greater than this.
		/// </remarks>
		/// <param name="obj"></param>
		/// <returns></returns>
		public int CompareTo(object obj)
		{
			Debug.Assert(obj is Material, "Materials cannot be compared to objects of type '" + obj.GetType().Name);

			Material material = obj as Material;

			// compare this Material with the incoming object to compare to.
			if(this.isTransparent && !material.isTransparent)
				return -1;
			else if(!this.isTransparent && material.isTransparent)
				return 1;
			else
				return 0;
		}

		#endregion

		#region Implementation of Resource

		/// <summary>
		///		
		/// </summary>
		public override void Load()
		{
			if(!isLoaded)
			{
				if(deferLoad)
				{
					// load all textures and controllers
					for(int i = 0; i < numTextureLayers; i++)
						textureLayers[i].Load();

					deferLoad = false;
				}
				isLoaded = true;
			}
		}

		/// <summary>
		///		
		/// </summary>
		public override void Unload()
		{
		
		}

		/// <summary>
		///		
		/// </summary>
		public override void Dispose()
		{
		}

		#endregion

		#region ICloneable Members

		public Material Clone(string name)
		{
			Material material = (Material)this.MemberwiseClone();
			material.name = name;

			// create a new array, otherwise the cloned material will use the same physical array
			// we only want a carbon copy, so we create a new array and clone the texture layers
			material.textureLayers = new TextureLayer[Config.MaxTextureLayers];

			// clone a copy of all the texture layers
			for(int layer = 0; layer < this.TextureLayers.Length; layer++)
				material.TextureLayers[layer] = (TextureLayer)this.TextureLayers[layer].Clone();

			// load the material so it gets stored in the resource list
			MaterialManager.Instance.Load(material, 1);

			return material;
		}

		public override int GetHashCode()
		{
			return name.GetHashCode();
		}

		public override string ToString()
		{
			return name;
		}



		#endregion
	}
}
