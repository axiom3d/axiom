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
using System.Collections;
using System.Diagnostics;
using Axiom.Core;

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
    ///    use a vertex and fragment program). Note that because of the nature of
    ///    the custom inputs and outputs of the programmable pipeline, you must use
    ///    both a vertex and fragment program, not just one or the other, if you
    ///    decide to use a programmable pass.
    ///    <p/>
    ///    Programmable passes are complex to define, because they require custom
    ///    programs and you have to set all constant inputs to the programs (like
    ///    the position of lights, any base material colours you wish to use etc), but
    ///    they do give you much total flexibility over the algorithms used to render your
    ///    pass, and you can create some effects which are impossible with a fixed-function pass.
    ///    On the other hand, you can define a fixed-function pass in very little time, and
    ///    you can use a range of fixed-function effects like environment mapping very
    ///    easily, plus your pass will be more likely to be compatible with older hardware.
    ///    There are pros and cons to both, just remember that if you use a programmable
    ///    pass to create some great effects, allow more time for definition and testing.
	/// </remarks>
	public class Pass {
		#region Fields
	
        /// <summary>
        ///    A reference to the technique that owns this Pass.
        /// </summary>
        protected Technique parent;
        /// <summary>
        ///    Index of this rendering pass.
        /// </summary>
        protected int index;
        /// <summary>
        ///    Pass hash, used for sorting passes.
        /// </summary>
        protected int hashCode;
        /// <summary>
        ///    Is this a programmable rendering pass?
        /// </summary>
        protected bool isProgrammable;
        /// <summary>
        ///    Ambient color in fixed function passes.
        /// </summary>
        protected ColorEx ambient;
        /// <summary>
        ///    Diffuse color in fixed function passes.
        /// </summary>
        protected ColorEx diffuse;
        /// <summary>
        ///    Specular color in fixed function passes.
        /// </summary>
        protected ColorEx specular;
        /// <summary>
        ///    Emissive color in fixed function passes.
        /// </summary>
        protected ColorEx emissive;
        /// <summary>
        ///    Shininess of the object's surface in fixed function passes.
        /// </summary>
        protected float shininess;
        /// <summary>
        ///    Source blend factor.
        /// </summary>
        protected SceneBlendFactor sourceBlendFactor;
        /// <summary>
        ///    Destination blend factor.
        /// </summary>
        protected SceneBlendFactor destBlendFactor;
        /// <summary>
        ///    Depth buffer checking setting for this pass.
        /// </summary>
        protected bool depthCheck;
        /// <summary>
        ///    Depth write setting for this pass.
        /// </summary>
        protected bool depthWrite;
        /// <summary>
        ///    Depth comparison function for this pass.
        /// </summary>
        protected CompareFunction depthFunc;
        /// <summary>
        ///    Depth bias for this pass.
        /// </summary>
        protected int depthBias;
        /// <summary>
        ///    Color write setting for this pass.
        /// </summary>
        protected bool colorWrite;
        /// <summary>
        ///    Hardware culling mode for this pass.
        /// </summary>
        protected CullingMode cullMode;
        /// <summary>
        ///    Software culling mode for this pass.
        /// </summary>
        protected ManualCullingMode manualCullMode;
        /// <summary>
        ///    Is lighting enabled for this pass?
        /// </summary>
        protected bool lightingEnabled;
        /// <summary>
        ///    Shading options for this pass.
        /// </summary>
        protected Shading shadeOptions;
        /// <summary>
        ///    Texture filtering for this pass.
        /// </summary>
        protected TextureFiltering textureFiltering;
        /// <summary>
        ///    Texture anisotropy level.
        /// </summary>
        protected int maxAniso;
        /// <summary>
        ///    Is the filtering level the default?
        /// </summary>
        protected bool isDefaultFiltering;
        /// <summary>
        ///    Is anisotropy the default?
        /// </summary>
        protected bool isDefaultAniso;
        /// <summary>
        ///    Does this pass override global fog settings?
        /// </summary>
        protected bool fogOverride;
        /// <summary>
        ///    Fog mode to use for this pass (if overriding).
        /// </summary>
        protected FogMode fogMode;
        /// <summary>
        ///    Color of the fog used for this pass (if overriding).
        /// </summary>
        protected ColorEx fogColor;
        /// <summary>
        ///    Starting point of the fog for this pass (if overriding).
        /// </summary>
        protected float fogStart;
        /// <summary>
        ///    Ending point of the fog for this pass (if overriding).
        /// </summary>
        protected float fogEnd;
        /// <summary>
        ///    Density of the fog for this pass (if overriding).
        /// </summary>
        protected float fogDensity;
        /// <summary>
        ///    List of fixed function texture unit states for this pass.
        /// </summary>
        protected ArrayList textureUnitStates = new ArrayList();
        /// <summary>
        ///    Details on the vertex program to be used for this pass.
        /// </summary>
        protected GpuProgramUsage vertexProgramUsage;
        /// <summary>
        ///    Details on the fragment program to be used for this pass.
        /// </summary>
        protected GpuProgramUsage fragmentProgramUsage;

		#endregion
		
		#region Constructors
		
        /// <summary>
        ///    Default constructor.
        /// </summary>
        /// <param name="parent">Technique that owns this Pass.</param>
        /// <param name="index">Index of this pass.</param>
        public Pass(Technique parent, int index) {
            Construct(parent, index, false);
        }

        /// <summary>
        ///    Default constructor.
        /// </summary>
        /// <param name="parent">Technique that owns this Pass.</param>
        /// <param name="index">Index of this pass.</param>
        /// <param name="isProgrammable">Programmable or fixed-function?</param>
		public Pass(Technique parent, int index, bool isProgrammable) {
            Construct(parent, index, isProgrammable);
		}

        /// <summary>
        ///    Helper method to allow for unique object construction.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="index"></param>
        /// <param name="isProgrammable"></param>
        private void Construct(Technique parent, int index, bool isProgrammable) {
            this.parent = parent;
            this.index = index;
            this.isProgrammable = isProgrammable;

            // color defaults
            ambient = ColorEx.FromColor(System.Drawing.Color.White);
            diffuse = ColorEx.FromColor(System.Drawing.Color.White);
            specular = ColorEx.FromColor(System.Drawing.Color.Black);
            emissive = ColorEx.FromColor(System.Drawing.Color.Black);
            
            // default blending (overwrite)
            sourceBlendFactor = SceneBlendFactor.One;
            destBlendFactor = SceneBlendFactor.Zero;

            // depth buffer settings
            depthCheck = true;
            depthWrite = true;
            colorWrite = true;
            depthFunc = CompareFunction.LessEqual;

            // cull settings
            cullMode = CullingMode.Clockwise;
            manualCullMode = ManualCullingMode.Back;

            // light settings
            lightingEnabled = true;
            shadeOptions = Shading.Gouraud;

            // texture filtering options
            textureFiltering = MaterialManager.Instance.DefaultTextureFiltering;
            maxAniso = MaterialManager.Instance.DefaultAnisotropy;
            isDefaultFiltering = true;
            isDefaultAniso = true;

            // initialize vertex and fragment program usage if necessary
            if(isProgrammable) {
                vertexProgramUsage = new GpuProgramUsage(GpuProgramType.Vertex, "");
                fragmentProgramUsage = new GpuProgramUsage(GpuProgramType.Fragment, "");
            }
        }
		
		#endregion
		
		#region Methods

        /// <summary>
        ///    Adds the passed in TextureUnitState, to the existing Pass.
        /// </summary>
        /// <param name="state">TextureUnitState to add to this pass.</param>
        public void AddTextureUnitState(TextureUnitState state) {
            textureUnitStates.Add(state);
        }

        /// <summary>
        ///    Overloaded method.
        /// </summary>
        /// <param name="textureName">The basic name of the texture (i.e. brickwall.jpg)</param>
        /// <returns></returns>
        public TextureUnitState CreateTextureUnitState(string textureName) {
            return CreateTextureUnitState(textureName, 0);
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
        public TextureUnitState CreateTextureUnitState(string textureName, int texCoordSet) {
            TextureUnitState state = new TextureUnitState(this);
            state.TextureName = textureName;
            state.TextureCoordSet = texCoordSet;
            textureUnitStates.Add(state);
            return state;
        }

        /// <summary>
        ///    Gets a reference to the TextureUnitState for this pass at the specified indx.
        /// </summary>
        /// <param name="index">Index of the state to retreive.</param>
        /// <returns>TextureUnitState at the specified index.</returns>
        public TextureUnitState GetTextureUnitState(int index) {
            Debug.Assert(index < textureUnitStates.Count, "index < textureUnitStates.Count");

            return (TextureUnitState)textureUnitStates[index];
        }

        /// <summary>
        ///    Internal method for loading this pass.
        /// </summary>
        internal void Load() {
            // it is assumed this is only being called when the Material is being loaded

            // load each texture unit state
            for(int i = 0; i < textureUnitStates.Count; i++) {
                ((TextureUnitState)textureUnitStates[i]).Load();
            }

            // load programs
            if(isProgrammable) {
                // load vertex program
                vertexProgramUsage.Load();

                // load vertex program
                fragmentProgramUsage.Load();
            }

            // recalculate hash code
            RecalculateHash();
        }

        /// <summary>
        ///    Tells the pass that it needs recompilation.
        /// </summary>
        internal void NotifyNeedsRecompile() {
            parent.NotifyNeedsRecompile();
        }

        /// <summary>
        ///    Internal method for recalculating the hash code used for sorting passes.
        /// </summary>
        internal void RecalculateHash() {
            /* Hash format is 32-bit, divided as follows (high to low bits)
               bits   purpose
                4     Pass index (i.e. max 16 passes!)
               14     Hashed texture name from unit 0
               14     Hashed texture name from unit 1

               Note that at the moment we don't sort on the 3rd texture unit plus
               on the assumption that these are less frequently used; sorting on 
               the first 2 gives us the most benefit for now.
           */
            hashCode = (index << 28);
            int count = NumTextureUnitStages;

            if(c > 0) {
                hashCode += (((TextureUnitState)textureUnitStates[0]).TextureName.GetHashCode() % (2 ^ 14)) << 14;
            }
            if(c > 1) {
                hashCode += (((TextureUnitState)textureUnitStates[0]).TextureName.GetHashCode() % (2 ^ 14));
            }
        }

        /// <summary>
        ///    Removes all texture unit settings from this pass.
        /// </summary>
        public void RemoveAllTextureUnitStates() {
            textureUnitStates.Clear();
        }

        /// <summary>
        ///    Removes the specified TextureUnitState from this pass.
        /// </summary>
        /// <param name="state">A reference to the TextureUnitState to remove from this pass.</param>
        public void RemoveTextureUnitState(TextureUnitState state) {
            textureUnitStates.Remove(state);
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
        ///    Only applicable if <paramref cref="overrideScene"/> is true. You can disable fog which is turned on for the
        ///    rest of the scene by specifying FogMode.None. Otherwise, set a pass-specific fog mode as
        ///    defined in the enum FogMode.
        /// </param>
        /// <param name="color">
        ///    The colour of the fog. Either set this to the same as your viewport background colour,
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
        public void SetFog(bool overrideScene, FogMode mode, ColorEx color, float density, float start, float end) {
            fogOverride = overrideScene;

            // set individual params if overriding scene level fog
            if(overrideScene) {
                fogMode = mode;
                fogColor = color;
                fogDensity = density;
                fogStart = start;
                fogEnd = end;
            }
        }

        /// <summary>
        ///    Overloaded method.
        /// </summary>
        /// <param name="overrideScene">
        ///    If true, you authorise this pass to override the scene's fog params with it's own settings.
        ///    If you specify false, so other parameters are necessary, and this is the default behaviour for passs.
        /// </param>
        public void SetFog(bool overrideScene) {
            SetFog(overrideScene, FogMode.None, ColorEx.FromColor(System.Drawing.Color.White), 0.001f, 0.0f, 1.0f);
        }

        /// <summary>
        ///    Overloaded method.
        /// </summary>
        /// <param name="overrideScene">
        ///    If true, you authorise this pass to override the scene's fog params with it's own settings.
        ///    If you specify false, so other parameters are necessary, and this is the default behaviour for passs.
        /// </param>
        /// <param name="mode">
        ///    Only applicable if <paramref cref="overrideScene"/> is true. You can disable fog which is turned on for the
        ///    rest of the scene by specifying FogMode.None. Otherwise, set a pass-specific fog mode as
        ///    defined in the enum FogMode.
        /// </param>
        public void SetFog(bool overrideScene, FogMode mode) {
            SetFog(overrideScene, mode, ColorEx.FromColor(System.Drawing.Color.White), 0.001f, 0.0f, 1.0f);
        }

        /// <summary>
        ///    Overloaded method.
        /// </summary>
        /// <param name="overrideScene">
        ///    If true, you authorise this pass to override the scene's fog params with it's own settings.
        ///    If you specify false, so other parameters are necessary, and this is the default behaviour for passs.
        /// </param>
        /// <param name="mode">
        ///    Only applicable if <paramref cref="overrideScene"/> is true. You can disable fog which is turned on for the
        ///    rest of the scene by specifying FogMode.None. Otherwise, set a pass-specific fog mode as
        ///    defined in the enum FogMode.
        /// </param>
        /// <param name="color">
        ///    The colour of the fog. Either set this to the same as your viewport background colour,
        ///    or to blend in with a skydome or skybox.
        /// </param>
        public void SetFog(bool overrideScene, FogMode mode, ColorEx color) {
            SetFog(overrideScene, mode, color, 0.001f, 0.0f, 1.0f);
        }

        /// <summary>
        ///    Overloaded method.
        /// </summary>
        /// <param name="overrideScene">
        ///    If true, you authorise this pass to override the scene's fog params with it's own settings.
        ///    If you specify false, so other parameters are necessary, and this is the default behaviour for passs.
        /// </param>
        /// <param name="mode">
        ///    Only applicable if <paramref cref="overrideScene"/> is true. You can disable fog which is turned on for the
        ///    rest of the scene by specifying FogMode.None. Otherwise, set a pass-specific fog mode as
        ///    defined in the enum FogMode.
        /// </param>
        /// <param name="color">
        ///    The colour of the fog. Either set this to the same as your viewport background colour,
        ///    or to blend in with a skydome or skybox.
        /// </param>
        /// <param name="density">
        ///    The density of the fog in FogMode.Exp or FogMode.Exp2 mode, as a value between 0 and 1. 
        ///    The default is 0.001.
        /// </param>
        public void SetFog(bool overrideScene, FogMode mode, ColorEx color, float density) {
            SetFog(overrideScene, mode, color, density, 0.0f, 1.0f);
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
        public void SetSceneBlending(SceneBlendType type) {
            // convert canned blending types into blending factors
            switch(type) {
                case SceneBlendType.Add:
                    SetSceneBlending(SceneBlendFactor.One, SceneBlendFactor.One);
                    break;
                case SceneBlendType.TransparentAlpha:
                    SetSceneBlending(SceneBlendFactor.SourceAlpha, SceneBlendFactor.OneMinusSourceAlpha);
                    break;
                case SceneBlendType.TransparentColor:
                    SetSceneBlending(SceneBlendFactor.SourceColor, SceneBlendFactor.OneMinusSourceColor);
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
        /// <param name="src">The source factor in the above calculation, i.e. multiplied by the texture colour components.</param>
        /// <param name="dest">The destination factor in the above calculation, i.e. multiplied by the pixel colour components.</param>
        public void SetSceneBlending(SceneBlendFactor src, SceneBlendFactor dest) {
            // copy settings
            sourceBlendFactor = src;
            destBlendFactor = dest;
        }

        /// <summary>
        ///    Splits this Pass to one which can be handled in the number of
        ///    texture units specified.
        /// </summary>
        /// <param name="numUnits">The target number of texture units.</param>
        /// <returns>
        ///    A new Pass which contains the remaining units, and a scene_blend
        ///    setting appropriate to approximate the multitexture. This Pass will be 
        ///    attached to the parent Technique of this Pass.
        /// </returns>
        /// TODO: Verify indexing
        public Pass Split(int numUnits) {
            // can't split programmable passes
            if(isProgrammable) {
                throw new Exception("Programmable passes cannot be automatically split.  Define a fallback technique instead");
            }

            if(textureUnitStates.Count > numUnits) {
                int start = textureUnitStates.Count - numUnits;

                Pass newPass = parent.CreatePass(false);

                // get a reference ot the texture unit state at the split position
                TextureUnitState state = (TextureUnitState)textureUnitStates[start];

                // set the new pass to fallback using scene blending
                newPass.SetSceneBlending(state.ColorBlendFallbackSource, state.ColorBlendFallbackDest);

                // add the rest of the texture units to the new pass
                for(int i = start; i < textureUnitStates.Count; i++) {
                    state = (TextureUnitState)textureUnitStates[i];
                    newPass.AddTextureUnitState(state);
                }

                // remove the extra texture units from this pass
                textureUnitStates.RemoveRange(start, textureUnitStates.Count - start);

                return newPass;
            }

            return null;
        }

        /// <summary>
        ///    Internal method for unloaded this pass.
        /// </summary>
        internal void Unload() {
            // load each texture unit state
            for(int i = 0; i < textureUnitStates.Count; i++) {
                ((TextureUnitState)textureUnitStates[i]).Unload();
            }

            // load programs
            if(isProgrammable) {
                // TODO: Do what?
            }
        }

        /// <summary>
        ///    Update any automatic parameters on this pass.
        /// </summary>
        /// <param name="renderable">Current object being rendered.</param>
        /// <param name="camera">Current being being used for rendering.</param>
        internal void UpdateAutoParams(IRenderable renderable, Camera camera) {
            // TODO: Implement me...please!!
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
        public override int GetHashCode() {
            return hashCode;
        }

        #endregion Object overrides
		
		#region Properties
        
        /// <summary>
		///    Sets the ambient color reflectance properties of this pass.
		/// </summary>
		/// <remarks>
		///    The base colour of a pass is determined by how much red, green and blue light is reflects
		///    (provided texture layer #0 has a blend mode other than LayerBlendOperation.Replace). 
		///    This property determines how much ambient light (directionless global light) is reflected. 
		///    The default is full white, meaning objects are completely globally illuminated. Reduce this 
		///    if you want to see diffuse or specular light effects, or change the blend of colours to make 
		///    the object have a base colour other than white.
		///    <p/>
        ///    This setting has no effect if dynamic lighting is disabled (see <see cref="Pass.LightingEnabled"/>),
        ///    or if this is a programmable pass.
		/// </remarks>
        public ColorEx Ambient {
            get {
                return ambient;
            }
            set {
                ambient = value;
            }
        }

        /// <summary>
        ///    Sets whether or not colour buffer writing is enabled for this Pass.
        /// </summary>
        /// <remarks>
        ///    For some effects, you might wish to turn off the colour write operation
        ///    when rendering geometry; this means that only the depth buffer will be
        ///    updated (provided you have depth buffer writing enabled, which you 
        ///    probably will do, although you may wish to only update the stencil
        ///    buffer for example - stencil buffer state is managed at the RenderSystem
        ///    level only, not the Material since you are likely to want to manage it 
        ///    at a higher level).
        /// </remarks>
        public bool ColorWrite {
            get {
            }
            set {
            }
        }

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
        public CullingMode CullMode {
            get {
                return cullMode;
            }
            set {
                cullMode = value;
            }
        }
        
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
        ///    the decal appears on top.
        /// </remarks>
        /// <value>
        ///    The bias value, should be between 0 and 16.
        /// </value>
        public int DepthBias {
            get {
                return depthBias;
            }
            set {
                Debug.Assert(bias <= 16, "Depth bias must be between 0 and 16.");
                depthBias = value;
            }
        }

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
        public bool DepthCheck {
            get {
                return depthCheck;
            }
            set {
                depthCheck = value;
            }
        }

        /// <summary>
        ///    Gets/Sets the function used to compare depth values when depth checking is on.
        /// </summary>
        /// <remarks>
        ///    If depth checking is enabled (see <see cref="DepthCheck"/>) a comparison occurs between the depth
        ///    value of the pixel to be written and the current contents of the buffer. This comparison is
        ///    normally CompareFunction.LessEqual, i.e. the pixel is written if it is closer (or at the same distance)
        ///    than the current contents. If you wish, you can change this comparison using this method.
        /// </remarks>
        public CompareFunction DepthFunction {
            get {
                return depthFunc;
            }
            set {
                depthFunc = value;
            }
        }
        
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
        public bool DepthWrite {
            get {
                return depthWrite;
            }
            set {
                depthWrite = value;
            }
        }

        /// <summary>
        ///    Retrieves the destination blending factor for the material (as set using SetSceneBlending).
        /// </summary>
        public SceneBlendFactor DestBlendFactor {
            get {
                return sourceBlendFactor;
            }
        }

        /// <summary>
		///    Sets the diffuse colour reflectance properties of this pass.
		/// </summary>
		/// <remarks>
		///    The base colour of a pass is determined by how much red, green and blue light is reflects
		///    (provided texture layer #0 has a blend mode other than LayerBlendOperation.Replace). This property determines how
		///    much diffuse light (light from instances of the Light class in the scene) is reflected. The default
		///    is full white, meaning objects reflect the maximum white light they can from Light objects.
		///    <p/>
		///    This setting has no effect if dynamic lighting is disabled (see <see cref="Pass.LightingEnabled"/>),
		///    or if this is a programmable pass.
		/// </remarks>
        public ColorEx Diffuse {
            get {
                return diffuse;
            }
            set {
                diffuse = value;
            }
        }
        
        /// <summary>
		/// 
		/// </summary>
        public ColorEx Emissive {
            get {
                return emissive;
            }
            set {
                emissive = value;
            }
        }
        
        /// <summary>
        ///    Returns the fog colour for the scene.
        /// </summary>
        /// <remarks>
        ///    Only valid if FogOverride is true.
        /// </remarks>
        public ColorEx FogColor {
            get {
                return fogColor;
            }
        }
        
        /// <summary>
        ///    Returns the fog density for this pass.
        /// </summary>
        /// <remarks>
        ///    Only valid if FogOverride is true.
        /// </remarks>
        public float FogDensity {
            get {
                return fogDensity;
            }
        }

        /// <summary>
        ///    Returns the fog end distance for this pass.
        /// </summary>
        /// <remarks>
        ///    Only valid if FogOverride is true.
        /// </remarks>
        public float FogEnd {
            get {
                return fogEnd;
            }
        }
        
        /// <summary>
        ///    Returns the fog mode for this pass.
        /// </summary>
        /// <remarks>
        ///    Only valid if FogOverride is true.
        /// </remarks>
        public FogMode FogMode {
            get {
                return fogMode;
            }
        }

        /// <summary>
        ///    Returns true if this pass is to override the scene fog settings.
        /// </summary>
        public bool FogOverride {
            get {
                return fogOverride;
            }
        }

        /// <summary>
        ///    Returns the fog start distance for this pass.
        /// </summary>
        /// <remarks>
        ///    Only valid if FogOverride is true.
        /// </remarks>
        public float FogStart {
            get {
                return fogStart;
            }
        }

        /// <summary>
        ///    Gets the vertex program used by this pass.
        /// </summary>
        /// <remarks>
        ///    Only available after Load() has been called.
        /// </remarks>
        public GpuProgram FragmentProgram {
            get {
                Debug.Assert(isProgrammable, "This pass is not programmable!");
                return fragmentProgramUsage.Program;
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
        public string FragmentProgramName {
            get {
                Debug.Assert(isProgrammable, "This pass is not programmable!");
                return fragmentProgramUsage.ProgramName;
            }
            set {
                Debug.Assert(isProgrammable, "This pass is not programmable!");
                fragmentProgramUsage.ProgramName = value;
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
        public GpuProgramParameters FragmentProgramParameters {
            get {
                Debug.Assert(isProgrammable, "This pass is not programmable!");
                return fragmentProgramUsage.Params;
            }
            set {
                Debug.Assert(isProgrammable, "This pass is not programmable!");
                fragmentProgramUsage.Params = value;
            }
        }

        /// <summary>
        ///    Gets the index of this Pass in the parent Technique.
        /// </summary>
        public int Index {
            get {
                return index;
            }
        }

        /// <summary>
        ///    Returns true if this pass is loaded.
        /// </summary>
        public bool IsLoaded {
            get {
                return parent.IsLoaded;
            }
        }

        /// <summary>
        ///    Returns true if this pass is programmable ie supports vertex and fragment programs.
        /// </summary>
        public bool IsProgrammable {
            get {
                return isProgrammable;
            }
            set {
                isProgrammable = value;
            }
        }

        /// <summary>
        ///    Returns true if this pass has some element of transparency.
        /// </summary>
        public bool IsTransparent {
            get {
                // Transparent if any of the destination color is taken into account
                return (destBlendFactor != SceneBlendFactor.Zero);
            }
        }

        /// <summary>
		///    Sets whether or not dynamic lighting is enabled.
		/// </summary>
		/// <remarks>
        ///    If true, dynamic lighting is performed on geometry with normals supplied, geometry without
        ///    normals will not be displayed.
        ///    If false, no lighting is applied and all geometry will be full brightness.
		/// </remarks>
        public bool LightingEnabled {
            get {
                return lightingEnabled;
            }
            set {
                lightingEnabled = value;
            }
        }

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
        public ManualCullingMode ManualCullMode {
            get {
                return manualCullMode;
            }
            set {
                manualCullMode = value;
            }
        }

        /// <summary>
        ///    Gets the number of fixed function texture unit states for this Pass.
        /// </summary>
        public int NumTextureUnitStages {
            get {
                return textureUnitStates.Count;
            }
        }

        /// <summary>
        ///    Gets a reference to the Technique that owns this pass.
        /// </summary>
        public Technique Parent {
            get {
                return parent;
            }
        }
        
        /// <summary>
		///    Sets the type of light shading required.
		/// </summary>
		/// <value>
		///    The default shading method is Gouraud shading.
		/// </value>
        public Shading ShadingMode {
            get {
                return shadeOptions;
            }
            set {
                shadeOptions = value;
            }
        }
        
        /// <summary>
        ///    Sets the shininess of the pass, affecting the size of specular highlights.
        /// </summary>
        /// <remarks>
        ///    This setting has no effect if dynamic lighting is disabled (see Pass::setLightingEnabled),
        ///    or if this is a programmable pass.
        /// </remarks>
        public float Shininess {
            get {
                return shininess;
            }
            set {
                shininess = value;
            }
        }

        /// <summary>
        ///    Retrieves the source blending factor for the material (as set using SetSceneBlending).
        /// </summary>
        public SceneBlendFactor SourceBlendFactor {
            get {
                return sourceBlendFactor;
            }
        }

        /// <summary>
        ///    Sets the specular colour reflectance properties of this pass.
        /// </summary>
        /// <remarks>
        ///    The base colour of a pass is determined by how much red, green and blue light is reflects
        ///    (provided texture layer #0 has a blend mode other than LBO_REPLACE). This property determines how
        ///    much specular light (highlights from instances of the Light class in the scene) is reflected.
        ///    The default is to reflect no specular light.
        ///    <p/>
        ///    The size of the specular highlights is determined by the separate Shininess property.
        ///    <p/>
        ///    This setting has no effect if dynamic lighting is disabled (see <see cref="Pass.LightingEnabled"/>),
        ///    or if this is a programmable pass.
        /// </remarks>
        public ColorEx Specular {
            get {
                return specular;
            }
            set {
                specular = value;
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
        public int TextureAnisotropy {
            set {
                for(int i = 0; i < textureUnitStates.Count; i++) {
                    ((TextureUnitState)textureUnitStates[i]).TextureAnisotropy = value;
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
        public TextureFiltering TextureFiltering {
            set {
                for(int i = 0; i < textureUnitStates.Count; i++) {
                    ((TextureUnitState)textureUnitStates[i]).TextureFiltering = value;
                }
            }
        }

        /// <summary>
        ///    Gets the vertex program used by this pass.
        /// </summary>
        /// <remarks>
        ///    Only available after Load() has been called.
        /// </remarks>
        public GpuProgram VertexProgram {
            get {
                Debug.Assert(isProgrammable, "This pass is not programmable!");
                return vertexProgramUsage.Program;
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
        public string VertexProgramName {
            get {
                Debug.Assert(isProgrammable, "This pass is not programmable!");
                return vertexProgramUsage.ProgramName;
            }
            set {
                Debug.Assert(isProgrammable, "This pass is not programmable!");
                vertexProgramUsage.ProgramName = value;
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
        public GpuProgramParameters VertexProgramParameters {
            get {
                Debug.Assert(isProgrammable, "This pass is not programmable!");
                return vertexProgramUsage.Params;
            }
            set {
                Debug.Assert(isProgrammable, "This pass is not programmable!");
                vertexProgramUsage.Params = value;
            }
        }
        
        #endregion
	}
}
