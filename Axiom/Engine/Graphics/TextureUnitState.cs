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
using System.Reflection;
using Axiom.Controllers;
using Axiom.Core;
using Axiom.MathLib;

namespace Axiom.Graphics
{
	/// <summary>
	/// 	Class representing the state of a single texture unit during a Pass of a
	/// 	Technique, of a Material.
	/// </summary>
	/// <remarks> 	
	/// 	Texture units are pipelines for retrieving texture data for rendering onto
	/// 	your objects in the world. Using them is common to both the fixed-function and 
	/// 	the programmable (vertex and fragment program) pipeline, but some of the 
	/// 	settings will only have an effect in the fixed-function pipeline (for example, 
	/// 	setting a texture rotation will have no effect if you use the programmable
	/// 	pipeline, because this is overridden by the fragment program). The effect
	/// 	of each setting as regards the 2 pipelines is commented in each setting.
	/// 	<p/>
	/// 	When I use the term 'fixed-function pipeline' I mean traditional rendering
	/// 	where you do not use vertex or fragment programs (shaders). Programmable 
	/// 	pipeline means that for this pass you are using vertex or fragment programs.
	/// </remarks>
	/// TODO: Destroy controllers
	public class TextureUnitState
	{
        #region Member variables

        /// <summary>
        ///    Maximum amount of animation frames allowed.
        /// </summary>
        public const int MAX_ANIMATION_FRAMES = 32;
        /// <summary>
        ///    The parent Pass that owns this TextureUnitState.
        /// </summary>
        protected Pass parent;
        /// <summary>
        ///    Index of the texture coordinate set to use for texture mapping.
        /// </summary>
        private int texCoordSet;
        /// <summary>
        ///    Addressing mode to use for texture coordinates.
        /// </summary>
        private TextureAddressing texAddressingMode;
        /// <summary>
        ///    Reference to a class containing the color blending operation params for this stage.
        /// </summary>
        private LayerBlendModeEx colorBlendMode = new LayerBlendModeEx();
        /// <summary>
        ///    Reference to a class containing the alpha blending operation params for this stage.
        /// </summary>
        private LayerBlendModeEx alphaBlendMode = new LayerBlendModeEx();
        /// <summary>
        ///    Fallback source blending mode, for use if the desired mode is not available.
        /// </summary>
        private SceneBlendFactor colorBlendFallbackSrc;
        /// <summary>
        ///    Fallback destination blending mode, for use if the desired mode is not available.
        /// </summary>
        private SceneBlendFactor colorBlendFallbackDest;
        /// <summary>
        ///    Operation to use (add, modulate, etc.) for color blending between stages.
        /// </summary>
        private LayerBlendOperation colorOp;
        /// <summary>
        ///    Is this a blank layer (i.e. no textures, or texture failed to load)?
        /// </summary>
        private bool isBlank;
        /// <summary>
        ///    Flags whether this layer is cubic texture.
        /// </summary>
        private bool isCubic;
        /// <summary>
        ///    Flags whether this is a 3 dimensional texture.
        /// </summary>
        private bool is3D;
        /// <summary>
        ///    Number of frames for this layer.
        /// </summary>
        private int numFrames;
        /// <summary>
        ///    Duration (in seconds) of the animated texture (if any).
        /// </summary>
        private float animDuration;
        /// <summary>
        ///    Index of the current frame of animation (always 0 for single texture stages).
        /// </summary>
        private int currentFrame;
        /// <summary>
        ///    Store names of textures for animation frames.
        /// </summary>
        private string[] frames = new string[MAX_ANIMATION_FRAMES];
        /// <summary>
        ///    Flag the determines if a recalc of the texture matrix is required, usually set after a rotate or
        ///    other transformations.
        /// </summary>
        private bool recalcTexMatrix;
        /// <summary>
        ///    U coord of the texture transformation.
        /// </summary>
        private float transU;
        /// <summary>
        ///    V coord of the texture transformation.
        /// </summary>
        private float transV;
        /// <summary>
        ///    U scale value of the texture transformation.
        /// </summary>
        private float scaleU;
        /// <summary>
        ///    V scale value of the texture transformation.
        /// </summary>
        private float scaleV;
        /// <summary>
        ///    Rotation value of the texture transformation.
        /// </summary>
        private float rotate;
        /// <summary>
        ///    4x4 texture matrix which gets updated based on various transformations made to this stage.
        /// </summary>
        private Matrix4 texMatrix;
        /// <summary>
        ///    List of effects to apply during this texture stage.
        /// </summary>
        private ArrayList effectList = new ArrayList();
        /// <summary>
        ///    Texture filtering mode to use for this stage.
        /// </summary>
        private TextureFiltering texFiltering;
        /// <summary>
        ///    Anisotropy setting for this stage.
        /// </summary>
        private int maxAnisotropy;
        /// <summary>
        ///    Function used to reject pixels based on alpha value.
        /// </summary>
        private CompareFunction alphaRejectFunction;
        /// <summary>
        ///    Value against which alpha values will be tested.
        /// </summary>
        private byte alphaRejectValue;

        #endregion

        #region Constructors

        /// <summary>
        ///		Default constructor.
        /// </summary>
        /// <param name="parent">Parent Pass of this TextureUnitState.</param>
        public TextureUnitState(Pass parent) {
            Construct(parent, "", 0);
        }

        /// <summary>
        ///		Name based constructor.
        /// </summary>
        /// <param name="parent">Parent Pass of this texture stage.</param>
        /// <param name="textureName">Name of the texture for this texture stage.</param>
        public TextureUnitState(Pass parent, string textureName) {
            Construct(parent, textureName, 0);
        }

        /// <summary>
        ///		Basic constructor.
        /// </summary>
        public TextureUnitState(Pass parent, string textureName, int texCoordSet) {
            Construct(parent, textureName, texCoordSet);
        }

        /// <summary>
        ///    Common method for constructing the object.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="textureName"></param>
        /// <param name="texCoordSet"></param>
        protected void Construct(Pass parent, string textureName, int texCoordSet) {
            this.parent = parent;
            // texture params
            TextureName = textureName;
            TextureCoordSet = texCoordSet;

            isBlank = true;

            colorBlendMode.blendType = LayerBlendType.Color;
            SetColorOperation(LayerBlendOperation.Modulate);
            TextureAddressing = TextureAddressing.Wrap;

            // set alpha blending options
            alphaBlendMode.operation = LayerBlendOperationEx.Modulate;
            alphaBlendMode.blendType = LayerBlendType.Alpha;
            alphaBlendMode.source1 = LayerBlendSource.Texture;
            alphaBlendMode.source2 = LayerBlendSource.Current;

            // default filtering and anisotropy
            texFiltering = MaterialManager.Instance.DefaultTextureFiltering;
            maxAnisotropy = MaterialManager.Instance.DefaultAnisotropy;

            // texture modification params
            transU = transV = 0;
            scaleU = scaleV = 1;
            rotate = 0;
            animDuration = 0;
            texMatrix = Matrix4.Identity;
            alphaRejectFunction = CompareFunction.AlwaysPass;
            alphaRejectValue = 0;
        }

        #endregion

        #region Properties

        /// <summary>
        ///		Gets a structure that describes the layer blending mode parameters.
        /// </summary>
        public LayerBlendModeEx AlphaBlendMode {
            get { 
                return alphaBlendMode; 
            }
        }

        /// <summary>
        ///    Gets the alpha reject function. See <see cref="SetAlphaRejectSettings"/> for more information.
        /// </summary>
        public CompareFunction AlphaRejectFunction {
            get {
                return alphaRejectFunction;
            }
        }

        /// <summary>
        ///    Gets the alpha reject value. See <see cref="SetAlphaRejectSettings"/> for more information.
        /// </summary>
        public byte AlphaRejectValue {
            get {
                return alphaRejectValue;
            }
        }

        /// <summary>
        ///    Gets/Sets the anisotropy level to be used for this texture stage.
        /// </summary>
        /// <remarks>
        ///    This option applies in both the fixed function and the programmable pipeline.
        /// </remarks>
        /// <value>
        ///    The maximal anisotropy level, should be between 2 and the maximum supported by hardware (1 is the default, ie. no anisotropy)
        /// </value>
        public int TextureAnisotropy {
            get {
                return maxAnisotropy;
            }
            set {
                maxAnisotropy = value;
            }
        }

        /// <summary>
        ///		Gets a structure that describes the layer blending mode parameters.
        /// </summary>
        public LayerBlendModeEx ColorBlendMode {
            get { 
                return colorBlendMode; 
            }
        }

        /// <summary>
        ///    Returns true if this texture unit requires an updated view matrix
        ///    to allow for proper texture matrix generation.
        /// </summary>
        public bool HasViewRelativeTexCoordGen {
            get {
                // TODO: Optimize this to hopefully eliminate the search every time
                for(int i = 0; i < effectList.Count; i++) {
                    TextureEffect effect = (TextureEffect)effectList[i];

                    if(effect.subtype == (System.Enum)EnvironmentMap.Reflection) {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        ///		Gets/Sets the name of the texture for this texture pass.
        /// </summary>
        /// <remarks>
        ///    This will either always be a single name for this layer,
        ///    or will be the name of the current frame for an animated
        ///    or otherwise multi-frame texture.
        ///    <p/>
        ///    Applies to both fixed-function and programmable pipeline.
        /// </remarks>
        public string TextureName {
            get { 
                return frames[currentFrame]; 
            }
            set { 
                frames[0] = value; 
                numFrames = 1;
                currentFrame = 0;
                isCubic = false;
				
                if(value.Length == 0) {
                    isBlank = true;
                    return;
                }

                if(this.IsLoaded) {
                    // reload
                    Load();

                    // tell parent to recalc hash (for sorting)
                    parent.RecalculateHash();
                }
            }
        }

        /// <summary>
        ///		Gets/Sets the texture coordinate set to be used by this texture layer.
        /// </summary>
        /// <remarks>
        ///		Default is 0 for all layers. Only change this if you have provided multiple texture coords per
        ///		vertex.
        ///		<p/>
        ///		Applies to both fixed-function and programmable pipeline.
        /// </remarks>
        public int TextureCoordSet {
            get { 
                return texCoordSet; 
            }
            set { 
                texCoordSet = value; 
            }
        }

        /// <summary>
        ///		Gets/Sets the texture addressing mode, i.e. what happens at uv values above 1.0.
        /// </summary>
        /// <remarks>
        ///    The default is <code>TextureAddressing.Wrap</code> i.e. the texture repeats over values of 1.0.
        ///    This applies for both the fixed-function and programmable pipelines.
        /// </remarks>
        public TextureAddressing TextureAddressing {
            get { 
                return texAddressingMode; 
            }
            set { 
                texAddressingMode = value; 
            }
        }

        /// <summary>
        ///		Get/Set the texture filtering mode for this layer.
        /// </summary>
        /// <remarks>
        ///    This option applies in both the fixed function and the programmable pipeline.
        /// </remarks>
        public TextureFiltering TextureFiltering {
            get { 
                return texFiltering; 
            }
            set { 
                texFiltering = value; 
            }
        }

        /// <summary>
        ///    Gets/Sets the multipass fallback for color blending operation source factor.
        /// </summary>
        public SceneBlendFactor ColorBlendFallbackSource {
            get { 
                return colorBlendFallbackSrc; 
            }
        }

        /// <summary>
        ///    Gets/Sets the multipass fallback for color blending operation destination factor.
        /// </summary>
        public SceneBlendFactor ColorBlendFallbackDest {
            get { 
                return colorBlendFallbackDest; 
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public LayerBlendOperation ColorOperation {
            get { return colorOp; }
        }

        /// <summary>
        ///		Gets/Sets whether this layer is blank or not.
        /// </summary>
        public bool Blank {
            get { 
                return isBlank;  
            }
            set { isBlank = value; }
        }

        /// <summary>
        ///		Gets/Sets the active frame in an animated or multi-image texture.
        /// </summary>
        /// <remarks>
        ///		An animated texture (or a cubic texture where the images are not combined for 3D use) is made up of
        ///		a number of frames. This method sets the active frame.
        ///		<p/>
        ///		Applies to both fixed-function and programmable pipeline.
        /// </remarks>
        public int CurrentFrame {
            get { 
                return currentFrame; 
            }
            set {
                Debug.Assert(value < numFrames, "Cannot set the current frame of a texture layer to be greater than the number of frames in the layer.");
                currentFrame = value;

                // this will affect the passes hashcode because of the texture name change
                parent.RecalculateHash();
            }
        }

        /// <summary>
        ///    Gets/Sets wether this texture layer is currently blank.
        /// </summary>
        public bool IsBlank {
            get {
                return isBlank;
            }
            set {
                isBlank = value;
            }
        }

        /// <summary>
        ///    Returns true if this layer is a cubic texture.
        /// </summary>
        /// <remarks>
        ///    Applies to both fixed-function and programmable pipeline.
        /// </remarks>
        public bool IsCubic {
            get {
                return isCubic;
            }
        }

        /// <summary>
        ///    Returns true if this texture layer uses a composite 3D cubic texture.
        /// </summary>
        /// <remarks>
        ///    Applies to both fixed-function and programmable pipeline.
        /// </remarks>
        public bool Is3D {
            get {
                return is3D;
            }
        }

        /// <summary>
        ///    Returns true if the resource for this texture layer have been loaded.
        /// </summary>
        public bool IsLoaded {
            get {
                return parent.IsLoaded;
            }
        }

        /// <summary>
        ///    Gets the number of effects currently tied to this texture stage.
        /// </summary>
        public int NumEffects {
            get {
                return effectList.Count;
            }
        }

        /// <summary>
        ///		Gets the number of frames for a texture.
        /// </summary>
        /// <remarks>
        ///    Applies to both fixed-function and programmable pipeline.
        /// </remarks>
        public int NumFrames {
            get { 
                return numFrames; 
            }
        }

        /// <summary>
        ///    Gets a reference to the Pass that owns this TextureUnitState.
        /// </summary>
        public Pass Parent {
            get {
                return parent;
            }
        }

        /// <summary>
        ///		Gets/Sets the Matrix4 that represents transformation to the texture in this layer.
        /// </summary>
        /// <remarks>
        ///    Texture coordinates can be modified on a texture layer to create effects like scrolling
        ///    textures. A texture transform can either be applied to a layer which takes the source coordinates
        ///    from a fixed set in the geometry, or to one which generates them dynamically (e.g. environment mapping).
        ///    <p/>
        ///    It's obviously a bit impractical to create scrolling effects by calling this method manually since you
        ///    would have to call it every frame with a slight alteration each time, which is tedious. Instead
        ///    you can use the ControllerManager class to create a Controller object which will manage the
        ///    effect over time for you. See <see cref="ControllerManager.CreateTextureScroller"/>and it's sibling methods for details.<BR>
        ///    In addition, if you want to set the individual texture transformations rather than concatenating them
        ///    yourself, use <see cref="SetTextureScroll"/>, <see cref="SetTextureScroll"/> and <see cref="SetTextureRotate"/>. 
        ///    <p/>
        ///    This has no effect in the programmable pipeline.
        /// </remarks>
        /// <seealso cref="Controller"/><seealso cref="ControllerManager"/>
        public Matrix4 TextureMatrix {
            get {
                // update the matrix before returning it if necessary
                if(recalcTexMatrix)
                    RecalcTextureMatrix();
                return texMatrix;
            }
            set {
                texMatrix = value;
                recalcTexMatrix = false;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        ///    Gets the texture effect at the specified index.
        /// </summary>
        /// <param name="index">Index of the texture effect to retrieve.</param>
        /// <returns>The TextureEffect at the specified index.</returns>
        public TextureEffect GetEffect(int index) {
            Debug.Assert(index < effectList.Count, "index < effectList.Count");

            return (TextureEffect)effectList[index];
        }

        /// <summary>
        ///    Removes all effects from this texture stage.
        /// </summary>
        public void RemoveAllEffects() {
            effectList.Clear();
        }

        /// <summary>
        ///    Removes the specified effect from the list of effects being applied during this
        ///    texture stage.
        /// </summary>
        /// <param name="effect">Effect to remove.</param>
        public void RemoveEffect(TextureEffect effect) {
            effectList.Remove(effect);
        }

        /// <summary>
        ///    Sets the multipass fallback operation for this layer, if you used TextureUnitState.SetColourOperationEx
        ///    and not enough multitexturing hardware is available.
        /// </summary>
        /// <remarks>
        ///    Because some effects exposed using TextureUnitState.SetColourOperationEx are only supported under
        ///    multitexturing hardware, if the hardware is lacking the system must fallback on multipass rendering,
        ///    which unfortunately doesn't support as many effects. This method is for you to specify the fallback
        ///    operation which most suits you.
        ///    <p/>
        ///    You'll notice that the interface is the same as the Material.SetSceneBlending method; this is
        ///    because multipass rendering IS effectively scene blending, since each layer is rendered on top
        ///    of the last using the same mechanism as making an object transparent, it's just being rendered
        ///    in the same place repeatedly to get the multitexture effect.
        ///    <p/>
        ///    If you use the simpler (and hence less flexible) TextureUnitState.SetColourOperation method you
        ///    don't need to call this as the system sets up the fallback for you.
        ///    <p/>
        ///    This option has no effect in the programmable pipeline, because there is no multipass fallback
        ///    and multitexture blending is handled by the fragment shader.
        /// </remarks>
        /// <param name="src">How to apply the source color during blending.</param>
        /// <param name="dest">How to affect the destination color during blending.</param>
        public void SetColorOpMultipassFallback(SceneBlendFactor src, SceneBlendFactor dest) {
            colorBlendFallbackSrc = src;
            colorBlendFallbackDest = dest;
        }

        /// <summary>
        ///    Sets this texture layer to use a combination of 6 texture maps, each one relating to a face of a cube.
        /// </summary>
        /// <remarks>
        ///    Cubic textures are made up of 6 separate texture images. Each one of these is an orthoganal view of the
        ///    world with a FOV of 90 degrees and an aspect ratio of 1:1. You can generate these from 3D Studio by
        ///    rendering a scene to a reflection map of a transparent cube and saving the output files.
        ///    <p/>
        ///    Cubic maps can be used either for skyboxes (complete wrap-around skies, like space) or as environment
        ///    maps to simulate reflections. The system deals with these 2 scenarios in different ways:
        ///    <ol>
        ///    <li>
        ///    <p>
        ///    For cubic environment maps, the 6 textures are combined into a single 'cubic' texture map which
        ///    is then addressed using 3D texture coordinates. This is required because you don't know what
        ///    face of the box you're going to need to address when you render an object, and typically you
        ///    need to reflect more than one face on the one object, so all 6 textures are needed to be
        ///    'active' at once. Cubic environment maps are enabled by calling this method with the forUVW
        ///    parameter set to true, and then calling <code>SetEnvironmentMap(true)</code>.
        ///    </p>
        ///    <p>
        ///    Note that not all cards support cubic environment mapping.
        ///    </p>
        ///    </li>
        ///    <li>
        ///    <p>
        ///    For skyboxes, the 6 textures are kept separate and used independently for each face of the skybox.
        ///    This is done because not all cards support 3D cubic maps and skyboxes do not need to use 3D
        ///    texture coordinates so it is simpler to render each face of the box with 2D coordinates, changing
        ///    texture between faces.
        ///    </p>
        ///    <p>
        ///    Skyboxes are created by calling SceneManager.SetSkyBox.
        ///    </p>
        ///    </li>
        ///    </ul>
        ///    <p/>
        ///    Applies to both fixed-function and programmable pipeline.
        /// </remarks>
        /// <param name="textureName">
        ///    The basic name of the texture e.g. brickwall.jpg, stonefloor.png. There must be 6 versions
        ///    of this texture with the suffixes _fr, _bk, _up, _dn, _lf, and _rt (before the extension) which
        ///    make up the 6 sides of the box. The textures must all be the same size and be powers of 2 in width & height.
        ///    If you can't make your texture names conform to this, use the alternative method of the same name which takes
        ///    an array of texture names instead.
        /// </param>
        /// <param name="forUVW">
        ///    Set to true if you want a single 3D texture addressable with 3D texture coordinates rather than
        ///    6 separate textures. Useful for cubic environment mapping.
        /// </param>
        public void SetCubicTexture(string textureName, bool forUVW) {
            if(forUVW) {
                // pass in the single texture name
                SetCubicTexture(new string[] { textureName}, forUVW);
            }
            else {
                string[] postfixes = {"_fr", "_bk", "_lf", "_rt", "_up", "_dn"};
                string[] fullNames = new string[6];
                string baseName;
                string ext;

                int pos = textureName.LastIndexOf(".");

                baseName = textureName.Substring(0, pos);
                ext = textureName.Substring(pos);

                for(int i = 0; i < 6; i++) {
                    fullNames[i] = baseName + postfixes[i] + ext;
                }

                SetCubicTexture(fullNames, forUVW);
            }
        }

        /// <summary>
        ///    Sets this texture layer to use a combination of 6 texture maps, each one relating to a face of a cube.
        /// </summary>
        /// <remarks>
        ///    Cubic textures are made up of 6 separate texture images. Each one of these is an orthoganal view of the
        ///    world with a FOV of 90 degrees and an aspect ratio of 1:1. You can generate these from 3D Studio by
        ///    rendering a scene to a reflection map of a transparent cube and saving the output files.
        ///    <p/>
        ///    Cubic maps can be used either for skyboxes (complete wrap-around skies, like space) or as environment
        ///    maps to simulate reflections. The system deals with these 2 scenarios in different ways:
        ///    <ol>
        ///    <li>
        ///    <p>
        ///    For cubic environment maps, the 6 textures are combined into a single 'cubic' texture map which
        ///    is then addressed using 3D texture coordinates. This is required because you don't know what
        ///    face of the box you're going to need to address when you render an object, and typically you
        ///    need to reflect more than one face on the one object, so all 6 textures are needed to be
        ///    'active' at once. Cubic environment maps are enabled by calling this method with the forUVW
        ///    parameter set to true, and then calling <code>SetEnvironmentMap(true)</code>.
        ///    </p>
        ///    <p>
        ///    Note that not all cards support cubic environment mapping.
        ///    </p>
        ///    </li>
        ///    <li>
        ///    <p>
        ///    For skyboxes, the 6 textures are kept separate and used independently for each face of the skybox.
        ///    This is done because not all cards support 3D cubic maps and skyboxes do not need to use 3D
        ///    texture coordinates so it is simpler to render each face of the box with 2D coordinates, changing
        ///    texture between faces.
        ///    </p>
        ///    <p>
        ///    Skyboxes are created by calling SceneManager.SetSkyBox.
        ///    </p>
        ///    </li>
        ///    </ul>
        ///    <p/>
        ///    Applies to both fixed-function and programmable pipeline.
        /// </remarks>
        /// <param name="textureNames">
        ///    6 versions of this texture with the suffixes _fr, _bk, _up, _dn, _lf, and _rt (before the extension) which
        ///    make up the 6 sides of the box. The textures must all be the same size and be powers of 2 in width & height.
        ///    If you can't make your texture names conform to this, use the alternative method of the same name which takes
        ///    an array of texture names instead.
        /// </param>
        /// <param name="forUVW">
        ///    Set to true if you want a single 3D texture addressable with 3D texture coordinates rather than
        ///    6 separate textures. Useful for cubic environment mapping.
        /// </param>
        public void SetCubicTexture(string[] textureNames, bool forUVW) {
            numFrames = forUVW ? 1 : 6;
            currentFrame = 0;
            isCubic = true;
            is3D = forUVW;

            for(int i = 0; i < numFrames; i++) {
                frames[i] = textureNames[i];
            }

            if(IsLoaded) {
                // load immediately if parent is loaded
                Load();     

                // tell parent to recalc the hash code
                parent.RecalculateHash();
            }
        }

        /// <summary>
        ///		Determines how this texture layer is combined with the one below it (or the diffuse color of
        ///		the geometry if this is layer 0).
        /// </summary>
        /// <remarks>
        ///    This method is the simplest way to blend tetxure layers, because it requires only one parameter,
        ///    gives you the most common blending types, and automatically sets up 2 blending methods: one for
        ///    if single-pass multitexturing hardware is available, and another for if it is not and the blending must
        ///    be achieved through multiple rendering passes. It is, however, quite limited and does not expose
        ///    the more flexible multitexturing operations, simply because these can't be automatically supported in
        ///    multipass fallback mode. If want to use the fancier options, use <see cref="TextureUnitState.SetColorOperationEx"/>,
        ///    but you'll either have to be sure that enough multitexturing units will be available, or you should
        ///    explicitly set a fallback using <see cref="TextureUnitState.SetColorOpMultipassFallback"/>.
        ///    <p/>
        ///    The default method is LayerBlendOperation.Modulate for all layers.
        ///    <p/>
        ///    This option has no effect in the programmable pipeline.
        /// </remarks>
        /// <param name="operation">One of the LayerBlendOperation enumerated blending types.</param>
        public void SetColorOperation(LayerBlendOperation operation) {
            colorOp = operation;

            // configure the multitexturing operations
            switch(operation) {
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
        ///    For setting advanced blending options.
        /// </summary>
        /// <remarks>
        ///    This is an extended version of the <see cref="TextureUnitState.SetColourOperation"/> method which allows
        ///    extremely detailed control over the blending applied between this and earlier layers.
        ///    See the IMPORTANT note below about the issues between mulitpass and multitexturing that
        ///    using this method can create.
        ///    <p/>
        ///    Texture color operations determine how the final color of the surface appears when
        ///    rendered. Texture units are used to combine color values from various sources (ie. the
        ///    diffuse color of the surface from lighting calculations, combined with the color of
        ///    the texture). This method allows you to specify the 'operation' to be used, ie. the
        ///    calculation such as adds or multiplies, and which values to use as arguments, such as
        ///    a fixed value or a value from a previous calculation.
        ///    <p/>
        ///    The defaults for each layer are:
        ///    <ul>
        ///    <li>op = Modulate</li>
        ///    <li>source1 = Texture</li>
        ///    <li>source2 = Current</li>
        ///    </ul>
        ///    ie. each layer takes the color results of the previous layer, and multiplies them
        ///    with the new texture being applied. Bear in mind that colors are RGB values from
        ///    0.0 - 1.0 so multiplying them together will result in values in the same range,
        ///    'tinted' by the multiply. Note however that a straight multiply normally has the
        ///    effect of darkening the textures - for this reason there are brightening operations
        ///    like ModulateX2. See the LayerBlendOperation and LayerBlendSource enumerated
        ///    types for full details.
        ///    <p/>
        ///    Because of the limitations on some underlying APIs (Direct3D included)
        ///    the Texture argument can only be used as the first argument, not the second.
        ///    <p/>
        ///    The final 3 parameters are only required if you decide to pass values manually
        ///    into the operation, i.e. you want one or more of the inputs to the color calculation
        ///    to come from a fixed value that you supply. Hence you only need to fill these in if
        ///    you supply <code>Manual</code> to the corresponding source, or use the 
        ///    <code>BlendManual</code> operation.
        ///    <p/>
        ///    The engine tries to use multitexturing hardware to blend texture layers
        ///    together. However, if it runs out of texturing units (e.g. 2 of a GeForce2, 4 on a
        ///    GeForce3) it has to fall back on multipass rendering, i.e. rendering the same object
        ///    multiple times with different textures. This is both less efficient and there is a smaller
        ///    range of blending operations which can be performed. For this reason, if you use this method
        ///    you MUST also call <see cref="TextureUnitState.SetColorOpMultipassFallback"/> to specify which effect you
        ///    want to fall back on if sufficient hardware is not available.
        ///    <p/>
        ///    If you wish to avoid having to do this, use the simpler <see cref="TextureUnitState.SetColorOperation"/> method
        ///    which allows less flexible blending options but sets up the multipass fallback automatically,
        ///    since it only allows operations which have direct multipass equivalents.
        ///    <p/>
        ///    This has no effect in the programmable pipeline.
        /// </remarks>
        /// <param name="operation">The operation to be used, e.g. modulate (multiply), add, subtract.</param>
        /// <param name="source1">The source of the first color to the operation e.g. texture color.</param>
        /// <param name="source2">The source of the second color to the operation e.g. current surface color.</param>
        /// <param name="arg1">Manually supplied color value (only required if source1 = Manual).</param>
        /// <param name="arg2">Manually supplied color value (only required if source2 = Manual)</param>
        /// <param name="blendFactor">
        ///    Manually supplied 'blend' value - only required for operations
        ///    which require manual blend e.g. LayerBlendOperationEx.BlendManual
        /// </param>
        public void SetColorOperationEx(LayerBlendOperationEx operation, LayerBlendSource source1, LayerBlendSource source2, ColorEx arg1, ColorEx arg2, float blendFactor) {
            colorBlendMode.operation = operation;
            colorBlendMode.source1 = source1;
            colorBlendMode.source2 = source2;
            colorBlendMode.colorArg1 = arg1;
            colorBlendMode.colorArg2 = arg2;
            colorBlendMode.blendFactor = blendFactor;
        }

        /// <summary>
        ///		Overloaded method.
        /// </summary>
        /// <param name="operation">The operation to be used, e.g. modulate (multiply), add, subtract.</param>
        public void SetColorOperationEx(LayerBlendOperationEx operation) {
            SetColorOperationEx(operation, LayerBlendSource.Texture, LayerBlendSource.Current, ColorEx.FromColor(System.Drawing.Color.White), ColorEx.FromColor(System.Drawing.Color.White), 0.0f);
        }

        /// <summary>
        ///		Overloaded method.
        /// </summary>
        /// <param name="operation">The operation to be used, e.g. modulate (multiply), add, subtract.</param>
        /// <param name="source1">The source of the first color to the operation e.g. texture color.</param>
        /// <param name="source2">The source of the second color to the operation e.g. current surface color.</param>
        public void SetColorOperationEx(LayerBlendOperationEx operation, LayerBlendSource source1, LayerBlendSource source2) {
            SetColorOperationEx(operation, source1, source2, ColorEx.FromColor(System.Drawing.Color.White), ColorEx.FromColor(System.Drawing.Color.White), 0.0f);
        }

        /// <summary>
        ///    Sets the alpha operation to be applied to this texture.
        /// </summary>
        /// <remarks>
        ///    This works in exactly the same way as SetColorOperation, except
        ///    that the effect is applied to the level of alpha (i.e. transparency)
        ///    of the texture rather than its color. When the alpha of a texel (a pixel
        ///    on a texture) is 1.0, it is opaque, wheras it is fully transparent if the
        ///    alpha is 0.0. Please refer to the SetColorOperation method for more info.
        /// </remarks>
        /// <param name="operation">The operation to be used, e.g. modulate (multiply), add, subtract.</param>
        /// <param name="source1">The source of the first alpha value to the operation e.g. texture alpha.</param>
        /// <param name="source2">The source of the second alpha value to the operation e.g. current surface alpha.</param>
        /// <param name="arg1">Manually supplied alpha value (only required if source1 = LayerBlendSource.Manual).</param>
        /// <param name="arg2">Manually supplied alpha value (only required if source2 = LayerBlendSource.Manual).</param>
        /// <param name="blendFactor">Manually supplied 'blend' value - only required for operations
        ///    which require manual blend e.g. LayerBlendOperationEx.BlendManual.
        /// </param>
        public void SetAlphaOperation(LayerBlendOperationEx operation, LayerBlendSource source1, LayerBlendSource source2, float arg1, float arg2, float blendFactor) {
            colorBlendMode.operation = operation;
            colorBlendMode.source1 = source1;
            colorBlendMode.source2 = source2;
            colorBlendMode.alphaArg1 = arg1;
            colorBlendMode.alphaArg2 = arg2;
            colorBlendMode.blendFactor = blendFactor;
        }

        /// <summary>
        ///    Overloaded method.
        /// </summary>
        /// <param name="operation">The operation to be used, e.g. modulate (multiply), add, subtract.</param>
        public void SetAlphaOperation(LayerBlendOperationEx operation) {
            SetAlphaOperation(operation, LayerBlendSource.Texture, LayerBlendSource.Current, 1.0f, 1.0f, 0.0f);
        }

        /// <summary>
        ///    Overloaded method.
        /// </summary>
        /// <param name="enable"></param>
        public void SetEnvironmentMap(bool enable) {
            // call with Curved as the default value
            SetEnvironmentMap(enable, EnvironmentMap.Curved);
        }	

        /// <summary>
        ///    Turns on/off texture coordinate effect that makes this layer an environment map.
        /// </summary>
        /// <remarks>
        ///    Environment maps make an object look reflective by using the object's vertex normals relative
        ///    to the camera view to generate texture coordinates.
        ///    <p/>
        ///    The vectors generated can either be used to address a single 2D texture which
        ///    is a 'fish-eye' lens view of a scene, or a 3D cubic environment map which requires 6 textures
        ///    for each side of the inside of a cube. The type depends on what texture you set up - if you use the
        ///    setTextureName method then a 2D fisheye lens texture is required, whereas if you used setCubicTextureName
        ///    then a cubic environemnt map will be used.
        ///    <p/>
        ///    This effect works best if the object has lots of gradually changing normals. The texture also
        ///    has to be designed for this effect - see the example spheremap.png included with the sample
        ///    application for a 2D environment map; a cubic map can be generated by rendering 6 views of a
        ///    scene to each of the cube faces with orthoganal views.
        ///    <p/>
        ///    Enabling this disables any other texture coordinate generation effects.
        ///    However it can be combined with texture coordinate modification functions, which then operate on the
        ///    generated coordinates rather than static model texture coordinates.
        ///    <p/>
        ///    This option has no effect in the programmable pipeline.
        /// </remarks>
        /// <param name="enable">True to enable, false to disable.</param>
        /// <param name="envMap">
        ///    If set to true, instead of being based on normals the environment effect is based on
        ///    vertex positions. This is good for planar surfaces.
        /// </param>
        public void SetEnvironmentMap(bool enable, EnvironmentMap envMap) {
            if(enable) {
                TextureEffect effect = new TextureEffect();
                effect.type = TextureEffectType.EnvironmentMap;
                effect.subtype = envMap;
                AddEffect(effect);
            }
            else {
                // remove it from the list
                RemoveEffect(TextureEffectType.EnvironmentMap);
            }
        }	

        /// <summary>
        ///    Gets the name of the texture associated with a frame.
        /// </summary>
        /// <remarks>
        ///    Applies to both fixed-function and programmable pipeline.
        /// </remarks>
        /// <param name="frame">Index of the frame to retreive the texture name for.</param>
        /// <returns>The name of the texture at the specified frame index.</returns>
        public string GetFrameTextureName(int frame) {
            Debug.Assert(frame < numFrames, "Attempted to access a frame which is out of range.");
            return frames[frame];
        }

        /// <summary>
        ///    Sets the way the layer will have use alpha to totally reject pixels from the pipeline.
        /// </summary>
        /// <remarks>
        ///    This option applies in both the fixed function and the programmable pipeline.
        /// </remarks>
        /// <param name="func">The comparison which must pass for the pixel to be written.</param>
        /// <param name="val">1 byte value against which alpha values will be tested(0-255).</param>
        public void SetAlphaRejectSettings(CompareFunction func, byte val) {
            alphaRejectFunction = func;
            alphaRejectValue = val;
        }

        /// <summary>
        ///    Sets the translation offset of the texture, ie scrolls the texture.
        /// </summary>
        /// <remarks>
        ///    This method sets the translation element of the texture transformation, and is easier to use than setTextureTransform if
        ///    you are combining translation, scaling and rotation in your texture transformation. Again if you want
        ///    to animate these values you need to use a Controller
        ///    <p/>
        ///    Has no effect in the programmable pipeline.
        /// </remarks>
        /// <param name="u">The amount the texture should be moved horizontally (u direction).</param>
        /// <param name="v">The amount the texture should be moved vertically (v direction).</param>
        public void SetTextureScroll(float u, float v) {
            transU = u;
            transV = v;
            recalcTexMatrix = true;
        }

        /// <summary>
        ///    Same as in SetTextureScroll, but sets only U value.
        /// </summary>
        /// <remarks>
        ///    Has no effect in the programmable pipeline.
        /// </remarks>
        /// <param name="u">The amount the texture should be moved horizontally (u direction).</param>
        public void SetTextureScrollU(float u) {
            transU = u;
            recalcTexMatrix = true;
        }

        /// <summary>
        ///    Same as in SetTextureScroll, but sets only V value.
        /// </summary>
        /// <remarks>
        ///    Has no effect in the programmable pipeline.
        /// </remarks>
        /// <param name="v">The amount the texture should be moved vertically (v direction).</param>
        public void SetTextureScrollV(float v) {
            transV = v;
            recalcTexMatrix = true;
        }

        /// <summary>
        ///		Sets up an animated scroll for the texture layer.
        /// </summary>
        /// <remarks>
        ///    Useful for creating constant scrolling effects on a texture layer (for varying scrolls, <see cref="SetTransformAnimation"/>).
        ///    <p/>
        ///    This option has no effect in the programmable pipeline.
        /// </remarks>
        /// <param name="uSpeed">The number of horizontal loops per second (+ve=moving right, -ve = moving left).</param>
        /// <param name="vSpeed">The number of vertical loops per second (+ve=moving up, -ve= moving down).</param>
        public void SetScrollAnimation(float uSpeed, float vSpeed) {
            TextureEffect effect = new TextureEffect();
            effect.type = TextureEffectType.Scroll;
            effect.arg1 = uSpeed;
            effect.arg2 = vSpeed;

            // add this effect to the list of effects for this texture stage.
            AddEffect(effect);
        }

        /// <summary>
        ///		Sets up an animated texture rotation for this layer.
        /// </summary>
        /// <remarks>
        ///    Useful for constant rotations (for varying rotations, <see cref="setTransformAnimation"/>).
        ///    <p/>
        ///    This option has no effect in the programmable pipeline.
        /// </remarks>
        /// <param name="speed">The number of complete counter-clockwise revolutions per second (use -ve for clockwise)</param>
        public void SetRotateAnimation(float speed) {
            TextureEffect effect = new TextureEffect();
            effect.type = TextureEffectType.Rotate;
            effect.arg1 = speed;

            AddEffect(effect);
        }

        /// <summary>
        ///    Sets up a general time-relative texture modification effect.
        /// </summary>
        /// <remarks>
        ///    This can be called multiple times for different values of <paramref cref="transType"/>, but only the latest effect
        ///    applies if called multiple time for the same <paramref cref="transType"/>.
        ///    <p/>
        ///    This option has no effect in the programmable pipeline.
        /// </remarks>
        /// <param name="transType">The type of transform, either translate (scroll), scale (stretch) or rotate (spin).</param>
        /// <param name="waveType">The shape of the wave, see <see cref="WaveformType"/> enum for details</param>
        /// <param name="baseVal">The base value for the function (range of output = {base, base + amplitude}).</param>
        /// <param name="frequency">The speed of the wave in cycles per second.</param>
        /// <param name="phase">The offset of the start of the wave, e.g. 0.5 to start half-way through the wave.</param>
        /// <param name="amplitude">Scales the output so that instead of lying within [0..1] it lies within [0..(1 * amplitude)] for exaggerated effects.</param>
        public void SetTransformAnimation(TextureTransform transType, WaveformType waveType, float baseVal, float frequency, float phase, float amplitude) {
            TextureEffect effect = new TextureEffect();
            effect.type = TextureEffectType.Transform;
            effect.subtype = transType;
            effect.waveType = waveType;
            effect.baseVal = baseVal;
            effect.frequency = frequency;
            effect.phase = phase;
            effect.amplitude = amplitude;

            AddEffect(effect);
        }

        /// <summary>
        ///    Sets the scaling factor of the texture.
        /// </summary>
        /// <remarks>
        ///    This method sets the scale element of the texture transformation, and is easier to use than
        ///    setTextureTransform if you are combining translation, scaling and rotation in your texture transformation. Again if you want
        ///    to animate these values you need to use a Controller (see ControllerManager and it's methods for
        ///    more information).
        ///    <p/>
        ///    Has no effect in the programmable pipeline.
        /// </remarks>
        /// <param name="u">The value by which the texture is to be scaled horizontally.</param>
        /// <param name="v">The value by which the texture is to be scaled vertically.</param>
        public void SetTextureScale(float u, float v) {
            scaleU = u;
            scaleV = v;
            recalcTexMatrix = true;
        }

        /// <summary>
        ///    Same as in SetTextureScale, but sets only U value.
        /// </summary>
        /// <remarks>
        ///    Has no effect in the programmable pipeline.
        /// </remarks>
        /// <param name="u">The value by which the texture is to be scaled horizontally.</param>
        public void SetTextureScaleU(float u) {
            scaleU = u;
            recalcTexMatrix = true;
        }

        /// <summary>
        ///    Same as in SetTextureScale, but sets only V value.
        /// </summary>
        /// <remarks>
        ///    Has no effect in the programmable pipeline.
        /// </remarks>
        /// <param name="v">The value by which the texture is to be scaled vertically.</param>
        public void SetTextureScaleV(float v) {
            scaleV = v;
            recalcTexMatrix = true;
        }

        /// <summary>
        ///    Sets the counter-clockwise rotation factor applied to texture coordinates.
        /// </summary>
        /// <remarks>
        ///    This sets a fixed rotation angle - if you wish to animate this, see the
        ///    <see cref="ControllerManager.CreateTextureRotater"/> method.
        ///    <p/>
        ///    Has no effect in the programmable pipeline.
        /// </remarks>
        /// <param name="degrees">The angle of rotation in degrees (counter-clockwise).</param>
        public void SetTextureRotate(float degrees) {
            rotate = degrees;
            recalcTexMatrix = true;
        }

        /// <summary>
        ///		Used to update the texture matrix if need be.
        /// </summary>
        private void RecalcTextureMatrix() {
            Matrix3 xform = Matrix3.Identity;

            // texture scaling
            if(scaleU != 0 || scaleV != 0) {
                // offset to the center of the texture
                xform.m00 = 1 / scaleU;
                xform.m11 = 1 / scaleV;

                // skip matrix mult since first matrix update
                xform.m02 = (-0.5f * xform.m00) + 0.5f;
                xform.m12 = (-0.5f * xform.m11) + 0.5f;
            }

            // texture translation
            if(transU != 0 || transV != 0) {
                Matrix3 xlate = Matrix3.Identity;

                xlate.m02 = transU;
                xlate.m12 = transV;

                // multiplt the transform by the translation
                xform = xlate * xform;
            }

            if(rotate != 0.0f) {
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
        ///		Generic method for setting up texture effects.
        /// </summary>
        /// <remarks>
        ///    Allows you to specify effects directly by using the TextureEffectType enumeration. The
        ///    arguments that go with it depend on the effect type. Only one effect of
        ///    each type can be applied to a texture layer.
        ///    <p/>
        ///    This method is used internally, but it is better generally for applications to use the
        ///    more intuitive specialized methods such as SetEnvironmentMap and SetScroll.
        /// </remarks>
        /// <param name="effect"></param>
        public void AddEffect(TextureEffect effect) {
            effect.controller = null;

            // these effects must be unique, so remove any existing
            if(effect.type == TextureEffectType.EnvironmentMap ||
                effect.type == TextureEffectType.Scroll ||
                effect.type == TextureEffectType.Rotate) {

                for(int i = 0; i < effectList.Count; i++) {
                    if(((TextureEffect)effectList[i]).type == effect.type) {
                        effectList.RemoveAt(i);
                        break;
                    }
                } // for
            }

            // create controller
            if(IsLoaded)
                CreateEffectController(effect);

            // add to internal list
            effectList.Add(effect);
        }

        /// <summary>
        ///		Removes effects of the specified type from this layers effect list.
        /// </summary>
        /// <param name="type"></param>
        private void RemoveEffect(TextureEffectType type) {
            // TODO: Verify this works correctly since we are removing items during a loop
            for(int i = 0; i < effectList.Count; i++) {
                if(((TextureEffect)effectList[i]).type == type)
                    effectList.RemoveAt(i);
            }
        }

        /// <summary>
        ///		Used internally to create a new controller for this layer given the requested effect.
        /// </summary>
        /// <param name="effect"></param>
        private void CreateEffectController(TextureEffect effect) {
            // get a reference to the singleton controller manager
            ControllerManager cMgr = ControllerManager.Instance;

            // create an appropriate controller based on the specified animation
            switch(effect.type) {
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
        ///    Internal method for loading this texture stage as part of Material.Load.
        /// </summary>
        internal void Load() {
            // load all textures
            for(int i = 0; i < numFrames; i++) {
                if(frames[i].Length > 0) {
                    try {
                        // ensure the texture is loaded
                        if(Is3D) {
                            TextureManager.Instance.Load(frames[i], TextureType.CubeMap);
                        }
                        else {
                            TextureManager.Instance.Load(frames[i]);
                        }

                        isBlank = false;
                    }
                    catch(Exception) {
                        Trace.WriteLine(string.Format("Error loading texture {0}.  Layer will be left blank.", frames[i]));
                        isBlank = true;
                    }
                }
            }

            // TODO: Init animated textures

            // initialize texture effects
            for(int i = 0; i < effectList.Count; i++) {
                TextureEffect effect = (TextureEffect)effectList[i];
                CreateEffectController(effect);
            }
        }

        /// <summary>
        ///    Internal method for unloading this object as part of Material.Unload.
        /// </summary>
        public void Unload() {
        }

        /// <summary>
        ///    Notifies the parent that it needs recompilation.
        /// </summary>
        public void NotifyNeedsRecompile() {
            parent.NotifyNeedsRecompile();
        }

        #endregion

        #region Object cloning

        /// <summary>
        ///		Used to clone a texture layer.  Mainly used during a call to Clone on a Material.
        /// </summary>
        /// <returns></returns>
        public void CopyTo(TextureUnitState layer) {

            FieldInfo[] props = layer.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance);

            for(int i = 0; i < props.Length; i++) {
                FieldInfo prop = props[i];

                object srcVal = prop.GetValue(this);
                prop.SetValue(layer, srcVal);
            }
    
            layer.frames = new string[MAX_ANIMATION_FRAMES];
                
            // copy over animation frame texture names
            for(int i = 0; i < MAX_ANIMATION_FRAMES; i++) {
                layer.frames[i] = frames[i];
            }
        }

        /// <summary>
        ///		Used to clone a texture layer.  Mainly used during a call to Clone on a Material or Pass.
        /// </summary>
        /// <returns></returns>
        public TextureUnitState Clone(Pass parent) {
            TextureUnitState newLayer = (TextureUnitState)this.MemberwiseClone();

            newLayer.frames = new string[MAX_ANIMATION_FRAMES];

            // copy over animation frame texture names
            for(int i = 0; i < MAX_ANIMATION_FRAMES; i++) {
                newLayer.frames[i] = frames[i];
            }

            // TODO: Revisit to clone these as well
            newLayer.colorBlendMode = colorBlendMode;
            newLayer.alphaBlendMode = alphaBlendMode;

            // set the manually specified parent for this new texture state
            newLayer.parent = parent;

            return newLayer;
        }
        #endregion
    }

    /// <summary>
    ///		Utility class for handling texture layer blending parameters.
    /// </summary>
    public class LayerBlendModeEx {
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
        public static bool operator == (LayerBlendModeEx left, LayerBlendModeEx right) {
            if((object)left == null) {
                return false;
            }

            if(left.colorArg1 != right.colorArg1 ||
                left.colorArg2 != right.colorArg2 ||
                left.blendFactor != right.blendFactor ||
                left.source1 != right.source1 ||
                left.source2 != right.source2 ||
                left.operation != right.operation) {
                return false;
            }
            else {
                return true;
            }
        }

        /// <summary>
        ///		Compares to blending modes for inequality.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool operator != (LayerBlendModeEx left, LayerBlendModeEx right) {
            if(left.blendType != right.blendType)
                return false;

            if(left.blendType == LayerBlendType.Color) {
                if(left.colorArg1 != right.colorArg1 ||
                    left.colorArg2 != right.colorArg2 ||
                    left.blendFactor != right.blendFactor ||
                    left.source1 != right.source1 ||
                    left.source2 != right.source2 ||
                    left.operation != right.operation) {
                    return true;
                }
            }
            else {
                if(left.alphaArg1 != right.alphaArg1 ||
                    left.alphaArg2 != right.alphaArg2 ||
                    left.blendFactor != right.blendFactor ||
                    left.source1 != right.source1 ||
                    left.source2 != right.source2 ||
                    left.operation != right.operation) {
                    return true;
                }
            }

            return false;
        }

        #region Object overloads

        /// <summary>
        ///    Overide to use custom equality check.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj) {
            LayerBlendModeEx lbx = obj as LayerBlendModeEx;

            return (lbx == this);
        }

        /// <summary>
        ///    Override.
        /// </summary>
        /// <remarks>
        ///    Overriden to quash warnings, not necessarily needed right now.
        /// </remarks>
        /// <returns></returns>
        public override int GetHashCode() {
            return base.GetHashCode ();
        }

        #endregion Object overloads
    }

    /// <summary>
    ///		Class used to define parameters for a texture effect.
    /// </summary>
    public class TextureEffect {
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
