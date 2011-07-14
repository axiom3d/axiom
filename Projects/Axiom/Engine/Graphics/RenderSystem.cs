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
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Linq;

using Axiom.Core;
using Axiom.Collections;
using Axiom.Configuration;
using Axiom.Graphics;
using Axiom.Math;
using Axiom.Media;
using Axiom.Graphics.Collections;
using Axiom.Core.Collections;

#endregion Namespace Declarations

namespace Axiom.Graphics
{
	/// <summary>
	///    Defines the functionality of a 3D API
	/// </summary>
	///	<remarks>
	///		The RenderSystem class provides a base class
	///		which abstracts the general functionality of the 3D API
	///		e.g. Direct3D or OpenGL. Whilst a few of the general
	///		methods have implementations, most of this class is
	///		abstract, requiring a subclass based on a specific API
	///		to be constructed to provide the full functionality.
	///		<p/>
	///		Note there are 2 levels to the interface - one which
	///		will be used often by the caller of the engine library,
	///		and one which is at a lower level and will be used by the
	///		other classes provided by the engine. These lower level
	///		methods are marked as internal, and are not accessible outside
	///		of the Core library.
	///	</remarks>
	public abstract class RenderSystem : DisposableObject
	{
		#region Constants

		/// <summary>
		///		Default window title if one is not specified upon a call to <see cref="Initialize"/>.
		/// </summary>
		const string DefaultWindowTitle = "Axiom Window";

		#endregion Constants

		#region Fields

		/// <summary>
		///		List of current render targets (i.e. a <see cref="RenderWindow"/>, or a<see cref="RenderTexture"/>) by priority
		/// </summary>
		protected List<RenderTarget> prioritizedRenderTargets = new List<RenderTarget>();
		/// <summary>
		///		List of current render targets (i.e. a <see cref="RenderWindow"/>, or a<see cref="RenderTexture"/>)
		/// </summary>
		protected Dictionary<string, RenderTarget> renderTargets = new Dictionary<string, RenderTarget>();
		/// <summary>
		///		A reference to the texture management class specific to this implementation.
		/// </summary>
		protected TextureManager textureManager;
		/// <summary>
		///		A reference to the hardware vertex/index buffer manager specific to this API.
		/// </summary>
		protected HardwareBufferManager hardwareBufferManager;
		/// <summary>
		///		Current hardware culling mode.
		/// </summary>
		protected CullingMode cullingMode;
		/// <summary>
		///		Are we syncing frames with the refresh rate of the screen?
		/// </summary>
		protected bool isVSync;
		/// <summary>
		///		Current depth write setting.
		/// </summary>
		protected bool depthWrite;
		/// <summary>
		///		Number of current active lights.
		/// </summary>
		protected int numCurrentLights;
		/// <summary>
		///		Reference to the config options for the graphics engine.
		/// </summary>
		protected ConfigOptionCollection engineConfig = new ConfigOptionCollection();
		/// <summary>
		///		Active viewport (dest for future rendering operations) and target.
		/// </summary>
		protected Viewport activeViewport;
		/// <summary>
		///		Active render target.
		/// </summary>
		protected RenderTarget activeRenderTarget;
		/// <summary>
		///		Number of faces currently rendered this frame.
		/// </summary>
		protected int faceCount;
		/// <summary>
		/// Number of batches currently rendered this frame.
		/// </summary>
		protected int batchCount;
		/// <summary>
		///		Number of vertexes currently rendered this frame.
		/// </summary>
		protected int vertexCount;
		/// <summary>
		/// Number of times to render the current state
		/// </summary>
		protected int currentPassIterationCount;
		/// <summary>
		///		Capabilites of the current hardware (populated at startup).
		/// </summary>
		protected RenderSystemCapabilities _rsCapabilities = new RenderSystemCapabilities();
		/// <summary>
		///		Saved set of world matrices.
		/// </summary>
		protected Matrix4[] worldMatrices = new Matrix4[ 256 ];
		/// <summary>
		///     Flag for whether vertex winding needs to be inverted, useful for reflections.
		/// </summary>
		protected bool invertVertexWinding;

		protected bool vertexProgramBound = false;
		protected bool fragmentProgramBound = false;
        /// <summary>
        /// Saved manual color blends
        /// </summary>
        protected ColorEx[,] manualBlendColors = new ColorEx[Config.MaxTextureLayers, 2];
		protected static long totalRenderCalls = 0;
        protected int disabledTexUnitsFrom = 0;
        protected bool derivedDepthBias = false;
        protected float derivedDepthBiasBase;
        protected float derivedDepthBiasMultiplier;
        protected float derivedDepthBiasSlopeScale;
		#endregion Fields

		#region Constructor

		/// <summary>
		///		Base constructor.
		/// </summary>
		public RenderSystem()
			: base()
		{
			// default to true
			isVSync = true;

			// default to true
			depthWrite = true;

			// This means CULL clockwise vertices, i.e. front of poly is counter-clockwise
			// This makes it the same as OpenGL and other right-handed systems
			cullingMode = Axiom.Graphics.CullingMode.Clockwise;
		}

		#endregion

		#region Virtual Members

		#region Properties

		/// <summary>
		///		Gets the currently-active viewport
		/// </summary>
		public Viewport ActiveViewport
		{
			get
			{
				return activeViewport;
			}
		}

		/// <summary>
		///		Gets a set of hardware capabilities queryed by the current render system.
		/// </summary>
		public virtual RenderSystemCapabilities HardwareCapabilities
		{
			get
			{
				return _rsCapabilities;
			}
		}

		/// <summary>
		/// Gets a dataset with the options set for the rendering system.
		/// </summary>
		public virtual ConfigOptionCollection ConfigOptions
		{
			get
			{
				return engineConfig;
			}
		}

		/// <summary>
		///		Number of faces rendered during the current frame so far.
		/// </summary>
		public int FacesRendered
		{
			get
			{
				return faceCount;
			}
		}

		/// <summary>
		///		Number of batches rendered during the current frame so far.
		/// </summary>
		public int BatchesRendered
		{
			get
			{
				return batchCount;
			}
		}

		/// <summary>
		///	Number of times to render the current state.
		/// </summary>
		/// <remarks>Set the current multi pass count value.  This must be set prior to 
		/// calling render() if multiple renderings of the same pass state are 
		/// required.
		/// </remarks>
		public int CurrentPassIterationCount
		{
			get
			{
				return currentPassIterationCount;
			}
			set
			{
				currentPassIterationCount = value;
			}
		}

		/// <summary>
		///     Sets whether or not vertex windings set should be inverted; this can be important
		///     for rendering reflections.
		/// </summary>
		public virtual bool InvertVertexWinding
		{
			get
			{
				return invertVertexWinding;
			}
			set
			{
				invertVertexWinding = value;
			}
		}

		/// <summary>
		/// Gets/Sets a value that determines whether or not to wait for the screen to finish refreshing
		/// before drawing the next frame.
		/// </summary>
		public virtual bool IsVSync
		{
			get
			{
				return isVSync;
			}
			set
			{
				isVSync = value;
			}
		}

		/// <summary>
		/// Gets the name of this RenderSystem based on it's assembly attribute Title.
		/// </summary>
		public virtual string Name
		{
			get
			{
				AssemblyTitleAttribute attribute =
					(AssemblyTitleAttribute)Attribute.GetCustomAttribute( this.GetType().Assembly, typeof( AssemblyTitleAttribute ), false );

				if ( attribute != null )
					return attribute.Title;
				else
					return "Not Found";
			}
		}

		public int RenderTargetCount
		{
			get
			{
				return renderTargets.Count;
			}
		}

		public static long TotalRenderCalls
		{
			get
			{
				return totalRenderCalls;
			}
		}

		#endregion Properties

        #region Methods
        /// <summary>
        /// 
        /// </summary>
        /// <param name="constantBias"></param>
        public virtual void SetDepthBias(float constantBias)
        {
            SetDepthBias(constantBias, 0);
        }
        /// <summary>
        /// Sets the depth bias, NB you should use the Material version of this.
        /// </summary>
        /// <param name="constantBias"></param>
        /// <param name="slopeScaleBias"></param>
        public virtual void SetDepthBias(float constantBias, float slopeScaleBias)
        {
            //need to be implemented by the rendersystem should be abstract, but this will course compiler error's.
        }
        /// <summary>
        /// Tell the render system whether to derive a depth bias on its own based on 
        /// the values passed to it in setCurrentPassIterationCount.
        /// The depth bias set will be baseValue + iteration * multiplier
        /// </summary>
        /// <param name="derive">true to tell the RS to derive this automatically</param>
        public virtual void SetDerivedDepthBias(bool derive)
        {
            SetDerivedDepthBias(derive, 0, 0, 0);
        }
        /// <summary>
        /// Tell the render system whether to derive a depth bias on its own based on 
        /// the values passed to it in setCurrentPassIterationCount.
        /// The depth bias set will be baseValue + iteration * multiplier
        /// </summary>
        /// <param name="derive">true to tell the RS to derive this automatically</param>
        /// <param name="baseValue">The base value to which the multiplier should be added</param>
        public virtual void SetDerivedDepthBias(bool derive, float baseValue)
        {
            SetDerivedDepthBias(derive, baseValue, 0, 0);
        }
        /// <summary>
        /// Tell the render system whether to derive a depth bias on its own based on 
        /// the values passed to it in setCurrentPassIterationCount.
        /// The depth bias set will be baseValue + iteration * multiplier
        /// </summary>
        /// <param name="derive">true to tell the RS to derive this automatically</param>
        /// <param name="baseValue">The base value to which the multiplier should be added</param>
        /// <param name="multiplier">The amount of depth bias to apply per iteration</param>
        public virtual void SetDerivedDepthBias(bool derive, float baseValue, float multiplier)
        {
            SetDerivedDepthBias(derive, baseValue, multiplier, 0);
        }
        /// <summary>
        /// Tell the render system whether to derive a depth bias on its own based on 
		/// the values passed to it in setCurrentPassIterationCount.
		/// The depth bias set will be baseValue + iteration * multiplier
        /// </summary>
        /// <param name="derive">true to tell the RS to derive this automatically</param>
        /// <param name="baseValue">The base value to which the multiplier should be added</param>
        /// <param name="multiplier">The amount of depth bias to apply per iteration</param>
        /// <param name="slopeScale">The constant slope scale bias for completeness</param>
        public virtual void SetDerivedDepthBias(bool derive, float baseValue, float multiplier, float slopeScale)
        {
            derivedDepthBias = derive;
            derivedDepthBiasBase = baseValue;
            derivedDepthBiasMultiplier = multiplier;
            derivedDepthBiasSlopeScale = slopeScale;
        }
        /// <summary>
        /// Validates the configuration of the rendering system
        /// </summary>
        /// <remarks>Calling this method can cause the rendering system to modify the ConfigOptions collection.</remarks>
        /// <returns>Error message is configuration is invalid <see cref="String.Empty"/> if valid.</returns>
        public virtual string ValidateConfiguration()
        {
            return String.Empty;
        }

		/// <summary>
		///    Attaches a render target to this render system.
		/// </summary>
		/// <param name="target">Reference to the render target to attach to this render system.</param>
		public virtual void AttachRenderTarget( RenderTarget target )
		{
			renderTargets.Add( target.Name, target );

			if ( target.Priority == RenderTargetPriority.RenderToTexture )
			{
				// insert at the front of the list
				prioritizedRenderTargets.Insert( 0, target );
			}
			else
			{
				// add to the end
				prioritizedRenderTargets.Add( target );
			}
		}

		/// <summary>
		///		The RenderSystem will keep a count of tris rendered, this resets the count.
		/// </summary>
		public virtual void BeginGeometryCount()
		{
			batchCount = vertexCount = faceCount = 0;
		}

		/// <summary>
		///		Detaches the render target with the specified name from this render system.
		/// </summary>
		/// <param name="name">Name of the render target to detach.</param>
		/// <returns>the render target that was detached</returns>
		public RenderTarget DetachRenderTarget( string name )
		{
			var target = (from item in prioritizedRenderTargets
						  where item.Name == name
						  select item).First();

			return DetachRenderTarget( target );
		}

		/// <summary>
		///		Detaches the render target from this render system.
		/// </summary>
		/// <param name="target">Reference to the render target to detach.</param>
		/// <returns>the render target that was detached</returns>
		public virtual RenderTarget DetachRenderTarget( RenderTarget target )
		{
			if ( target != null )
			{
				prioritizedRenderTargets.Remove( target );
				renderTargets.Remove( target.Name );

				/// If detached render target is the active render target, 
				/// reset active render target
				if ( target == activeRenderTarget )
					activeRenderTarget = null;
			}
			return target;
		}

		/// <summary>
		///		Turns off a texture unit if not needed.
		/// </summary>
		/// <param name="stage"></param>
		public virtual void DisableTextureUnit( int stage )
		{
			SetTexture( stage, false, "" );
			SetTextureMatrix( stage, Matrix4.Identity );
		}

        public virtual void DisableTextureUnitsFrom( int texUnit )
        {
            int disableTo = Config.MaxTextureLayers;
            if (disableTo > disabledTexUnitsFrom)
                disableTo = disabledTexUnitsFrom;
            disabledTexUnitsFrom = texUnit;
            for ( int i = texUnit; i < disableTo; ++i )
            {
                try
                {
                    DisableTextureUnit( i );
                }
				catch ( Exception )
                {
                }
            }
        }


		/// <summary>
		///     Utility method for initializing all render targets attached to this rendering system.
		/// </summary>
		public virtual void InitRenderTargets()
		{
			// init stats for each render target
			foreach ( KeyValuePair<string, RenderTarget> item in renderTargets )
			{
				item.Value.ResetStatistics();
			}
		}

		/// <summary>
		/// Set a clipping plane
		///</summary>
		public void SetClipPlane( ushort index, Plane p )
		{
			SetClipPlane( index, p.Normal.x, p.Normal.y, p.Normal.z, p.D );
		}

		/// <summary>
		/// Set a clipping plane
		/// </summary>
		/// <param name="index">Index of plane</param>
		/// <param name="A"></param>
		/// <param name="B"></param>
		/// <param name="C"></param>
		/// <param name="D"></param>
		public abstract void SetClipPlane( ushort index, float A, float B, float C, float D );

		/// <summary>
		/// Enable the clipping plane
		/// </summary>
		/// <param name="index">Index of plane</param>
		/// <param name="enable">Enable True or False</param>
		public abstract void EnableClipPlane( ushort index, bool enable );

		/// <summary>
		///		Utility method to notify all render targets that a camera has been removed, 
		///		incase they were referring to it as their viewer. 
		/// </summary>
		/// <param name="camera">Camera being removed.</param>
		internal virtual void NotifyCameraRemoved( Camera camera )
		{
			foreach ( KeyValuePair<string, RenderTarget> item in renderTargets )
			{
				item.Value.NotifyCameraRemoved( camera );
			}
		}

		/// <summary>
		///		Render something to the active viewport.
		/// </summary>
		/// <remarks>
		///		Low-level rendering interface to perform rendering
		///		operations. Unlikely to be used directly by client
		///		applications, since the <see cref="SceneManager"/> and various support
		///		classes will be responsible for calling this method.
		///		Can only be called between <see cref="BeginScene"/> and <see cref="EndScene"/>
		/// </remarks>
		/// <param name="op">
		///		A rendering operation instance, which contains details of the operation to be performed.
		///	</param>
		public virtual void Render( RenderOperation op )
		{
			int val;

			if ( op.useIndices )
			{
				val = op.indexData.indexCount;
			}
			else
			{
				val = op.vertexData.vertexCount;
			}

			// account for a pass having multiple iterations
			if ( currentPassIterationCount > 1 )
				val *= currentPassIterationCount;

			// calculate faces
			switch ( op.operationType )
			{
				case OperationType.TriangleList:
					faceCount += val / 3;
					break;
				case OperationType.TriangleStrip:
				case OperationType.TriangleFan:
					faceCount += val - 2;
					break;
				case OperationType.PointList:
				case OperationType.LineList:
				case OperationType.LineStrip:
					break;
			}

			// increment running vertex count
			vertexCount += op.vertexData.vertexCount;

			batchCount += currentPassIterationCount;
		}
		/// <summary>
		/// updates pass iteration rendering state including bound gpu program parameter pass iteration auto constant entry
		/// </summary>
		/// <returns>True if more iterations are required</returns>
		protected virtual bool UpdatePassIterationRenderState()
		{
			if ( currentPassIterationCount <= 1 )
				return false;

			--currentPassIterationCount;

			// TODO: Implement ActiveGpuProgramParameters
			//if ( ActiveVertexGpuProgramParameters != null )
			//{
			//    ActiveVertexGpuProgramParameters.IncrementPassIterationNumber();
			//    bindGpuProgramPassIterationParameters( GpuProgramType.Vertex );
			//}
			//if ( ActiveFragmentGpuProgramParameters != null )
			//{
			//    ActiveFragmentGpuProgramParameters.IncrementPassIterationNumber();
			//    bindGpuProgramPassIterationParameters( GpuProgramType.Fragement );
			//}
			return true;

		}

		/// <summary>
		///		Utility function for setting all the properties of a texture unit at once.
		///		This method is also worth using over the individual texture unit settings because it
		///		only sets those settings which are different from the current settings for this
		///		unit, thus minimising render state changes.
		/// </summary>
		/// <param name="textureUnit">Index of the texture unit to configure</param>
		/// <param name="layer">Reference to a TextureLayer object which defines all the settings.</param>
		public virtual void SetTextureUnit( int unit, TextureUnitState unitState, bool fixedFunction )
		{
			// This method is only ever called to set a texture unit to valid details
			// The method DisableTextureUnit is called to turn a unit off

			Texture texture = (Texture)TextureManager.Instance.GetByName( unitState.TextureName );
			// Vertex Texture Binding?
			if ( this.HardwareCapabilities.HasCapability( Capabilities.VertexTextureFetch )
				 && !this.HardwareCapabilities.VertexTextureUnitsShared )
			{
				if ( unitState.BindingType == TextureBindingType.Vertex )
				{
					// Bind Vertex Texture
					SetVertexTexture( unit, texture );
					// bind nothing to fragment unit (hardware isn't shared but fragment
					// unit can't be using the same index
					this.SetTexture( unit, true, (Texture)null );
				}
				else
				{
					// vice versa
					SetVertexTexture( unit, null );
					this.SetTexture( unit, true, unitState.TextureName );
				}

			}
			else
			{
				// Shared vertex / fragment textures or no vertex texture support
				// Bind texture (may be blank)
				this.SetTexture( unit, true, unitState.TextureName );
			}

			// Tex Coord Set
			SetTextureCoordSet( unit, unitState.TextureCoordSet );

			// Texture layer filtering
			SetTextureUnitFiltering(
				unit,
				unitState.GetTextureFiltering( FilterType.Min ),
				unitState.GetTextureFiltering( FilterType.Mag ),
				unitState.GetTextureFiltering( FilterType.Mip ) );

			// Texture layer anistropy
			SetTextureLayerAnisotropy( unit, unitState.TextureAnisotropy );

			// Set mipmap biasing
			// TODO: implement SetTextureMipmapBias( unit, unitState.TextureMipmapBias );

			// set the texture blending modes
			// NOTE: Color before Alpha is important
			SetTextureBlendMode( unit, unitState.ColorBlendMode );
			SetTextureBlendMode( unit, unitState.AlphaBlendMode );

			// this must always be set for OpenGL.  DX9 will ignore dupe render states like this (observed in the
			// output window when debugging with high verbosity), so there is no harm
			// TODO: Implement UVWTextureAddressMode
            UVWAddressing uvw = unitState.TextureAddressingMode;
			SetTextureAddressingMode( unit, uvw );
			// Set the texture border color only if needed.
			
			if (    uvw.U == TextureAddressing.Border
				 || uvw.V == TextureAddressing.Border
				 || uvw.W == TextureAddressing.Border )
			{
				SetTextureBorderColor( unit, unitState.TextureBorderColor );
			}

			// Set texture Effects
			bool anyCalcs = false;
			// TODO: Change TextureUnitState Effects to use Enumeration
			for ( int i = 0; i < unitState.NumEffects; i++ )
			{
				TextureEffect effect = unitState.GetEffect( i );

				switch ( effect.type )
				{
					case TextureEffectType.EnvironmentMap:
						switch ( (EnvironmentMap)effect.subtype )
						{
							case EnvironmentMap.Curved:
								SetTextureCoordCalculation( unit, TexCoordCalcMethod.EnvironmentMap );
								break;
							case EnvironmentMap.Planar:
								SetTextureCoordCalculation( unit, TexCoordCalcMethod.EnvironmentMapPlanar );
								break;
							case EnvironmentMap.Reflection:
								SetTextureCoordCalculation( unit, TexCoordCalcMethod.EnvironmentMapReflection );
								break;
							case EnvironmentMap.Normal:
								SetTextureCoordCalculation( unit, TexCoordCalcMethod.EnvironmentMapNormal );
								break;
						}
						anyCalcs = true;
						break;

					case TextureEffectType.UVScroll:
					case TextureEffectType.UScroll:
					case TextureEffectType.VScroll:
					case TextureEffectType.Rotate:
					case TextureEffectType.Transform:
						break;

					case TextureEffectType.ProjectiveTexture:
						SetTextureCoordCalculation( unit, TexCoordCalcMethod.ProjectiveTexture, effect.frustum );
						anyCalcs = true;
						break;
				} // switch
			} // for

			// Ensure any previous texcoord calc settings are reset if there are now none
			if ( !anyCalcs )
			{
				SetTextureCoordCalculation( unit, TexCoordCalcMethod.None );
			}

			// set the texture matrix to that of the current layer for any transformations
			SetTextureMatrix( unit, unitState.TextureMatrix );
		}

		/// <summary>
		///    Sets the filtering options for a given texture unit.
		/// </summary>
		/// <param name="unit">The texture unit to set the filtering options for.</param>
		/// <param name="minFilter">The filter used when a texture is reduced in size.</param>
		/// <param name="magFilter">The filter used when a texture is magnified.</param>
		/// <param name="mipFilter">
		///		The filter used between mipmap levels, <see cref="FilterOptions.None"/> disables mipmapping.
		/// </param>
		public void SetTextureUnitFiltering( int unit, FilterOptions minFilter, FilterOptions magFilter, FilterOptions mipFilter )
		{
			SetTextureUnitFiltering( unit, FilterType.Min, minFilter );
			SetTextureUnitFiltering( unit, FilterType.Mag, magFilter );
			SetTextureUnitFiltering( unit, FilterType.Mip, mipFilter );
		}

		/// <summary>
		///	
		/// </summary>
		/// <param name="matrices"></param>
		/// <param name="count"></param>
		public virtual void SetWorldMatrices( Matrix4[] matrices, ushort count )
		{
			if ( !_rsCapabilities.HasCapability( Capabilities.VertexBlending ) )
			{
				// save these for later during software vertex blending
				for ( int i = 0; i < count; i++ )
				{
					worldMatrices[ i ] = matrices[ i ];
				}

				// reset the hardware world matrix to identity
				WorldMatrix = Matrix4.Identity;
			}
		}

		public virtual void RemoveRenderTargets()
		{
			// destroy each render window
			RenderTarget primary = null;
			while ( renderTargets.Count > 0 )
			{
				Dictionary<string, RenderTarget>.Enumerator iter = renderTargets.GetEnumerator();
				iter.MoveNext();
				KeyValuePair<string, RenderTarget> item = iter.Current;
				RenderTarget target = item.Value;
				//if ( primary == null && item.Value.IsPrimary )
				//{
				//  primary = target;
				//}
				//else
				//{
				DetachRenderTarget( target );
				target.Dispose();
				//}
			}
			if ( primary != null )
			{
				DetachRenderTarget( primary );
				primary.Dispose();
			}

			renderTargets.Clear();
			prioritizedRenderTargets.Clear();
		}

		/// <summary>
		///		Shuts down the RenderSystem.
		/// </summary>
		public virtual void Shutdown()
		{
			RemoveRenderTargets();

			// dispose of the render system
			this.Dispose();
		}

		/// <summary>
		/// Internal method for updating all render targets attached to this rendering system.
		/// </summary>
		public virtual void UpdateAllRenderTargets()
		{
			this.UpdateAllRenderTargets( true );
		}

		/// <summary>
		/// Internal method for updating all render targets attached to this rendering system.
		/// </summary>
		/// <param name="swapBuffers"></param>
		public virtual void UpdateAllRenderTargets( bool swapBuffers )
		{
			// Update all in order of priority
			// This ensures render-to-texture targets get updated before render windows
			foreach ( RenderTarget target in prioritizedRenderTargets )
			{
				// only update if it is active
				if ( target.IsActive && target.IsAutoUpdated )
				{
					target.Update( swapBuffers );
				}
			}
		}

		/// <summary>
		/// Internal method for swapping all the buffers on all render targets,
		/// if <see cref="UpdateAllRenderTargets"/> was called with a 'false' parameter.
		/// </summary>
		/// <param name="swapBuffers"></param>
		public virtual void SwapAllRenderTargetBuffers( bool waitForVSync )
		{
			// Update all in order of priority
			// This ensures render-to-texture targets get updated before render windows
			foreach ( RenderTarget target in prioritizedRenderTargets )
			{
				// only update if it is active
				if ( target.IsActive && target.IsAutoUpdated )
				{
					target.SwapBuffers( waitForVSync );
				}
			}
		}

		#endregion Methods

		#endregion Virtual Members

		#region Abstract Members

		#region Properties

		/// <summary>
		///		Sets the color & strength of the ambient (global directionless) light in the world.
		/// </summary>
		public abstract ColorEx AmbientLight
		{
			get;
			set;
		}

		/// <summary>
		///    Gets/Sets the culling mode for the render system based on the 'vertex winding'.
		/// </summary>
		/// <remarks>
		///		A typical way for the rendering engine to cull triangles is based on the
		///		'vertex winding' of triangles. Vertex winding refers to the direction in
		///		which the vertices are passed or indexed to in the rendering operation as viewed
		///		from the camera, and will wither be clockwise or counterclockwise.  The default is <see cref="CullingMode.Clockwise"/>  
		///		i.e. that only triangles whose vertices are passed/indexed in counterclockwise order are rendered - this 
		///		is a common approach and is used in 3D studio models for example. You can alter this culling mode 
		///		if you wish but it is not advised unless you know what you are doing. You may wish to use the 
		///		<see cref="CullingMode.None"/> option for mesh data that you cull yourself where the vertex winding is uncertain.
		/// </remarks>
		public abstract CullingMode CullingMode
		{
			get;
			set;
		}

		/// <summary>
		///		Gets/Sets whether or not the depth buffer is updated after a pixel write.
		/// </summary>
		/// <value>
		///		If true, the depth buffer is updated with the depth of the new pixel if the depth test succeeds.
		///		If false, the depth buffer is left unchanged even if a new pixel is written.
		/// </value>
		public abstract bool DepthWrite
		{
			get;
			set;
		}

		/// <summary>
		///		Gets/Sets whether or not the depth buffer check is performed before a pixel write.
		/// </summary>
		/// <value>
		///		If true, the depth buffer is tested for each pixel and the frame buffer is only updated
		///		if the depth function test succeeds. If false, no test is performed and pixels are always written.
		/// </value>
		public abstract bool DepthCheck
		{
			get;
			set;
		}

		/// <summary>
		///		Gets/Sets the comparison function for the depth buffer check.
		/// </summary>
		/// <remarks>
		///		Advanced use only - allows you to choose the function applied to compare the depth values of
		///		new and existing pixels in the depth buffer. Only an issue if the depth buffer check is enabled.
		/// <seealso cref="DepthCheck"/>
		/// </remarks>
		/// <value>
		///		The comparison between the new depth and the existing depth which must return true
		///		for the new pixel to be written.
		/// </value>
		public abstract CompareFunction DepthFunction
		{
			get;
			set;
		}

		/// <summary>
		///		Gets/Sets the depth bias.
		/// </summary>
		/// <remarks>
		///		When polygons are coplanar, you can get problems with 'depth fighting' where
		///		the pixels from the two polys compete for the same screen pixel. This is particularly
		///		a problem for decals (polys attached to another surface to represent details such as
		///		bulletholes etc.).
		///		<p/>
		///		A way to combat this problem is to use a depth bias to adjust the depth buffer value
		///		used for the decal such that it is slightly higher than the true value, ensuring that
		///		the decal appears on top.
		/// </remarks>
		/// <value>The bias value, should be between 0 and 16.</value>
		public abstract float DepthBias
		{
			get;
			set;
		}

		/// <summary>
		///		Returns the horizontal texel offset value required for mapping 
		///		texel origins to pixel origins in this rendersystem.
		/// </summary>
		/// <remarks>
		///		Since rendersystems sometimes disagree on the origin of a texel, 
		///		mapping from texels to pixels can sometimes be problematic to 
		///		implement generically. This method allows you to retrieve the offset
		///		required to map the origin of a texel to the origin of a pixel in
		///		the horizontal direction.
		/// </remarks>
		public abstract float HorizontalTexelOffset
		{
			get;
		}

		/// <summary>
		///		Gets/Sets whether or not dynamic lighting is enabled.
		///		<p/>
		///		If true, dynamic lighting is performed on geometry with normals supplied, geometry without
		///		normals will not be displayed. If false, no lighting is applied and all geometry will be full brightness.
		/// </summary>
		public abstract bool LightingEnabled
		{
			get;
			set;
		}

		/// <summary>
		///    Get/Sets whether or not normals are to be automatically normalized.
		/// </summary>
		/// <remarks>
		///    This is useful when, for example, you are scaling SceneNodes such that
		///    normals may not be unit-length anymore. Note though that this has an
		///    overhead so should not be turn on unless you really need it.
		///    <p/>
		///    You should not normally call this direct unless you are rendering
		///    world geometry; set it on the Renderable because otherwise it will be
		///    overridden by material settings. 
		/// </remarks>
		public abstract bool NormalizeNormals
		{
			get;
			set;
		}

		/// <summary>
		///		Gets/Sets the current projection matrix.
		///	</summary>
		public abstract Matrix4 ProjectionMatrix
		{
			get;
			set;
		}

		/// <summary>
		///		Gets/Sets how to rasterise triangles, as points, wireframe or solid polys.
		/// </summary>
		public abstract PolygonMode PolygonMode
		{
			get;
			set;
		}

		/// <summary>
		///		Gets/Sets the type of light shading required (default = Gouraud).
		/// </summary>
		public abstract Shading ShadingMode
		{
			get;
			set;
		}

		/// <summary>
		///		Turns stencil buffer checking on or off. 
		/// </summary>
		///	<remarks>
		///		Stencilling (masking off areas of the rendering target based on the stencil 
		///		buffer) can be turned on or off using this method. By default, stencilling is
		///		disabled.
		///	</remarks>
		public abstract bool StencilCheckEnabled
		{
			get;
			set;
		}

		/// <summary>
		///		Returns the vertical texel offset value required for mapping 
		///		texel origins to pixel origins in this rendersystem.
		/// </summary>
		/// <remarks>
		///		Since rendersystems sometimes disagree on the origin of a texel, 
		///		mapping from texels to pixels can sometimes be problematic to 
		///		implement generically. This method allows you to retrieve the offset
		///		required to map the origin of a texel to the origin of a pixel in
		///		the vertical direction.
		/// </remarks>
		public abstract float VerticalTexelOffset
		{
			get;
		}

		/// <summary>
		///		Gets/Sets the current view matrix.
		///	</summary>
		public abstract Matrix4 ViewMatrix
		{
			get;
			set;
		}

		/// <summary>
		///		Gets/Sets the current world matrix.
		/// </summary>
		public abstract Matrix4 WorldMatrix
		{
			get;
			set;
		}

		/// <summary>
		/// Gets the maximum (closest) depth value to be used when rendering using identity transforms.
		/// </summary>
		/// <remarks>
		/// When using identity transforms you can manually set the depth
		/// of a vertex; however the input values required differ per
		/// rendersystem. This method lets you retrieve the correct value.
		/// <see cref="SimpleRenderable.UseIdentityView"/>
		/// <see cref="SimpleRenderable.UseIdentityProjection"/>
		/// </remarks>
		public abstract Real MinimumDepthInputValue
		{
			get;
		}

		/// <summary>
		/// Gets the maximum (farthest) depth value to be used when rendering using identity transforms.
		/// </summary>
		/// <remarks>
		/// When using identity transforms you can manually set the depth
		/// of a vertex; however the input values required differ per
		/// rendersystem. This method lets you retrieve the correct value.
		/// <see cref="SimpleRenderable.UseIdentityView"/>
		/// <see cref="SimpleRenderable.UseIdentityProjection"/>
		/// </remarks>
		public abstract Real MaximumDepthInputValue
		{
			get;
		}

		#endregion Properties

		#region Methods

		/// <summary>
		///		Update a perspective projection matrix to use 'oblique depth projection'.
		/// </summary>
		/// <remarks>
		///		This method can be used to change the nature of a perspective 
		///		transform in order to make the near plane not perpendicular to the 
		///		camera view direction, but to be at some different orientation. 
		///		This can be useful for performing arbitrary clipping (e.g. to a 
		///		reflection plane) which could otherwise only be done using user
		///		clip planes, which are more expensive, and not necessarily supported
		///		on all cards.
		/// </remarks>
		/// <param name="projMatrix">
		///		The existing projection matrix. Note that this must be a
		///		perspective transform (not orthographic), and must not have already
		///		been altered by this method. The matrix will be altered in-place.
		/// </param>
		/// <param name="plane">
		///		The plane which is to be used as the clipping plane. This
		///		plane must be in CAMERA (view) space.
		///	</param>
		/// <param name="forGpuProgram">Is this for use with a Gpu program or fixed-function transforms?</param>
		public abstract void ApplyObliqueDepthProjection( ref Matrix4 projMatrix, Plane plane, bool forGpuProgram );

		/// <summary>
		///		Signifies the beginning of a frame, ie the start of rendering on a single viewport. Will occur
		///		several times per complete frame if multiple viewports exist.
		/// </summary>
		public abstract void BeginFrame();

		/// <summary>
		///    Binds a given GpuProgram (but not the parameters). 
		/// </summary>
		/// <remarks>
		///    Only one GpuProgram of each type can be bound at once, binding another
		///    one will simply replace the existing one.
		/// </remarks>
		/// <param name="program"></param>
		public virtual void BindGpuProgram( GpuProgram program )
		{
			switch ( program.Type )
			{
				case GpuProgramType.Vertex:
					vertexProgramBound = true;
					break;
				case GpuProgramType.Fragment:
					fragmentProgramBound = true;
					break;
			}
		}

		/// <summary>
		///    Bind Gpu program parameters.
		/// </summary>
		/// <param name="parms"></param>
		public abstract void BindGpuProgramParameters( GpuProgramType type, GpuProgramParameters parms );

		/// <summary>
		///		Clears one or more frame buffers on the active render target.
		/// </summary>
		/// <param name="buffers">
		///		Combination of one or more elements of <see cref="FrameBuffer"/>
		///		denoting which buffers are to be cleared.
		/// </param>
		/// <param name="color">The color to clear the color buffer with, if enabled.</param>
		/// <param name="depth">The value to initialize the depth buffer with, if enabled.</param>
		/// <param name="stencil">The value to initialize the stencil buffer with, if enabled.</param>
		public abstract void ClearFrameBuffer( FrameBufferType buffers, ColorEx color, float depth, int stencil );

		/// <summary>
		///		Converts the Axiom.Core.ColorEx value to a int.  Each API may need the 
		///		bytes of the packed color data in different orders. i.e. OpenGL - ABGR, D3D - ARGB
		/// </summary>
		/// <param name="color"></param>
		/// <returns></returns>
		public abstract int ConvertColor( ColorEx color );

		/// <summary>
		///		Converts the int value to an Axiom.Core.ColorEx object.  Each API may have the 
		///		bytes of the packed color data in different orders. i.e. OpenGL - ABGR, D3D - ARGB
		/// </summary>
		/// <param name="color"></param>
		/// <returns></returns>
		public abstract ColorEx ConvertColor( int color );

		/// <summary>
		///		Creates a new render window.
		/// </summary>
		/// <remarks>
		///		This method creates a new rendering window as specified
		///		by the paramteters. The rendering system could be
		///		responible for only a single window (e.g. in the case
		///		of a game), or could be in charge of multiple ones (in the
		///		case of a level editor). The option to create the window
		///		as a child of another is therefore given.
		///		This method will create an appropriate subclass of
		///		RenderWindow depending on the API and platform implementation.
		/// </remarks>
		/// <param name="name"></param>
		/// <param name="width"></param>
		/// <param name="height"></param>
		/// <param name="isFullscreen"></param>
		/// <param name="miscParams">
		///		A collection of addition rendersystem specific options.
		///	</param>
		/// <returns></returns>
		public abstract RenderWindow CreateRenderWindow( string name, int width, int height, bool isFullScreen, NamedParameterList miscParams );

		/// <summary>
		/// Create a MultiRenderTarget, which is a render target that renders to multiple RenderTextures at once.
		/// </summary>
		/// <Remarks>
		/// Surfaces can be bound and unbound at will. This fails if Capabilities.MultiRenderTargetsCount is smaller than 2.
		/// </Remarks>
		/// <returns></returns>
		public abstract MultiRenderTarget CreateMultiRenderTarget( string name );

		/// <summary>
		///		Requests an API implementation of a hardware occlusion query used to test for the number
		///		of fragments rendered between calls to <see cref="HardwareOcclusionQuery.Begin"/> and 
		///		<see cref="HardwareOcclusionQuery.End"/> that pass the depth buffer test.
		/// </summary>
		/// <returns>An API specific implementation of an occlusion query.</returns>
		public abstract HardwareOcclusionQuery CreateHardwareOcclusionQuery();

		/// <summary>
		///		Ends rendering of a frame to the current viewport.
		/// </summary>
		public abstract void EndFrame();

		/// <summary>
		/// Initialize the rendering engine.
		/// </summary>
		/// <param name="autoCreateWindow">If true, a default window is created to serve as a rendering target.</param>
		/// <param name="windowTitle">Text to display on the window caption if not fullscreen.</param>
		/// <returns>A RenderWindow implementation specific to this RenderSystem.</returns>
		/// <remarks>All subclasses should call this method from within thier own intialize methods.</remarks>
		public virtual RenderWindow Initialize( bool autoCreateWindow, string windowTitle )
		{
			vertexProgramBound = false;
			fragmentProgramBound = false;
			return null;
		}

		/// <summary>
		///	Initialize the rendering engine.
		/// </summary>
		/// <param name="autoCreateWindow">If true, a default window is created to serve as a rendering target.</param>
		/// <returns>A RenderWindow implementation specific to this RenderSystem.</returns>
		public RenderWindow Initialize( bool autoCreateWindow )
		{
			return Initialize( autoCreateWindow, DefaultWindowTitle );
		}

		/// <summary>
		///		Builds an orthographic projection matrix suitable for this render system.
		/// </summary>
		/// <remarks>
		///		Because different APIs have different requirements (some incompatible) for the
		///		projection matrix, this method allows each to implement their own correctly and pass
		///		back a generic Matrix4 for storage in the engine.
		///	 </remarks>
		/// <param name="fov">Field of view angle.</param>
		/// <param name="aspectRatio">Aspect ratio.</param>
		/// <param name="near">Near clipping plane distance.</param>
		/// <param name="far">Far clipping plane distance.</param>
		/// <param name="forGpuProgram"></param>
		/// <returns></returns>
		public abstract Matrix4 MakeOrthoMatrix( float fov, float aspectRatio, float near, float far, bool forGpuPrograms );

		/// <summary>
		/// 	Converts a uniform projection matrix to one suitable for this render system.
		/// </summary>
		/// <remarks>
		///		Because different APIs have different requirements (some incompatible) for the
		///		projection matrix, this method allows each to implement their own correctly and pass
		///		back a generic Matrix4 for storage in the engine.
		///	 </remarks>
		/// <param name="matrix"></param>
		/// <param name="forGpuProgram"></param>
		/// <returns></returns>
		public abstract Matrix4 ConvertProjectionMatrix( Matrix4 matrix, bool forGpuProgram );

		/// <summary>
		///		Builds a perspective projection matrix suitable for this render system.
		/// </summary>
		/// <remarks>
		///		Because different APIs have different requirements (some incompatible) for the
		///		projection matrix, this method allows each to implement their own correctly and pass
		///		back a generic Matrix4 for storage in the engine.
		///	 </remarks>
		/// <param name="fov">Field of view angle.</param>
		/// <param name="aspectRatio">Aspect ratio.</param>
		/// <param name="near">Near clipping plane distance.</param>
		/// <param name="far">Far clipping plane distance.</param>
		/// <param name="forGpuProgram"></param>
		/// <returns></returns>
		public abstract Matrix4 MakeProjectionMatrix( float fov, float aspectRatio, float near, float far, bool forGpuProgram );

		/// <summary>
		///  Sets the global alpha rejection approach for future renders.
		/// </summary>
		/// <param name="func">The comparison function which must pass for a pixel to be written.</param>
		/// <param name="val">The value to compare each pixels alpha value to (0-255)</param>
		/// <param name="alphaToCoverage">Whether to enable alpha to coverage, if supported</param>
		public abstract void SetAlphaRejectSettings( CompareFunction func, int val, bool alphaToCoverage );

		/// <summary>
		///   Used to confirm the settings (normally chosen by the user) in
		///   order to make the renderer able to inialize with the settings as required.
		///   This make be video mode, D3D driver, full screen / windowed etc.
		///   Called automatically by the default configuration
		///   dialog, and by the restoration of saved settings.
		///   These settings are stored and only activeated when 
		///   RenderSystem::Initalize or RenderSystem::Reinitialize are called
		/// </summary>
		/// <param name="name">the name of the option to alter</param>
		/// <param name="value">the value to set the option to</param>
		public abstract void SetConfigOption( string name, string value );

		/// <summary>
		///    Sets whether or not color buffer writing is enabled, and for which channels. 
		/// </summary>
		/// <remarks>
		///    For some advanced effects, you may wish to turn off the writing of certain color
		///    channels, or even all of the color channels so that only the depth buffer is updated
		///    in a rendering pass. However, the chances are that you really want to use this option
		///    through the Material class.
		/// </remarks>
		/// <param name="red">Writing enabled for red channel.</param>
		/// <param name="green">Writing enabled for green channel.</param>
		/// <param name="blue">Writing enabled for blue channel.</param>
		/// <param name="alpha">Writing enabled for alpha channel.</param>
		public abstract void SetColorBufferWriteEnabled( bool red, bool green, bool blue, bool alpha );

		/// <summary>
		///		Sets the mode of operation for depth buffer tests from this point onwards.
		/// </summary>
		/// <remarks>
		///		Sometimes you may wish to alter the behavior of the depth buffer to achieve
		///		special effects. Because it's unlikely that you'll set these options for an entire frame,
		///		but rather use them to tweak settings between rendering objects, this is intended for internal
		///		uses, which will be used by a <see cref="SceneManager"/> implementation rather than directly from 
		///		the client application.
		/// </remarks>
		/// <param name="depthTest">
		///		If true, the depth buffer is tested for each pixel and the frame buffer is only updated
		///		if the depth function test succeeds. If false, no test is performed and pixels are always written.
		/// </param>
		/// <param name="depthWrite">
		///		If true, the depth buffer is updated with the depth of the new pixel if the depth test succeeds.
		///		If false, the depth buffer is left unchanged even if a new pixel is written.
		/// </param>
		/// <param name="depthFunction">Sets the function required for the depth test.</param>
		public abstract void SetDepthBufferParams( bool depthTest, bool depthWrite, CompareFunction depthFunction );

		public void SetFog()
		{
			SetFog( FogMode.None, ColorEx.White, 1.0f, 0.0f, 1.0f );
		}

		public void SetFog( FogMode mode )
		{
			SetFog( mode, ColorEx.White, 1.0f, 0.0f, 1.0f );
		}

		public void SetFog( FogMode mode, ColorEx color )
		{
			SetFog( mode, color, 1.0f, 0.0f, 1.0f );
		}

		/// <summary>
		///		Sets the fog with the given params.
		/// </summary>
		/// <param name="mode"></param>
		/// <param name="color"></param>
		/// <param name="density"></param>
		/// <param name="start"></param>
		/// <param name="end"></param>
		public abstract void SetFog( FogMode mode, ColorEx color, float density, float start, float end );

		/// <summary>
		///		Sets the global blending factors for combining subsequent renders with the existing frame contents.
		///		The result of the blending operation is:</p>
		///		<p align="center">final = (texture * src) + (pixel * dest)</p>
		///		Each of the factors is specified as one of a number of options, as specified in the SceneBlendFactor
		///		enumerated type.
		/// </summary>
		/// <param name="src">The source factor in the above calculation, i.e. multiplied by the texture color components.</param>
		/// <param name="dest">The destination factor in the above calculation, i.e. multiplied by the pixel color components.</param>
		public abstract void SetSceneBlending( SceneBlendFactor src, SceneBlendFactor dest );

		/// <summary>
		/// Sets the global blending factors for combining subsequent renders with the existing frame contents.
		/// The result of the blending operation is:
		/// final = (texture * sourceFactor) + (pixel * destFactor).
		/// Each of the factors is specified as one of a number of options, as specified in the SceneBlendFactor
		/// enumerated type.
		/// </summary>
		/// <param name="sourceFactor">The source factor in the above calculation, i.e. multiplied by the texture colour components.</param>
		/// <param name="destFactor">The destination factor in the above calculation, i.e. multiplied by the pixel colour components.</param>
		/// <param name="sourceFactorAlpha">The source factor in the above calculation for the alpha channel, i.e. multiplied by the texture alpha components.</param>
		/// <param name="destFactorAlpha">The destination factor in the above calculation for the alpha channel, i.e. multiplied by the pixel alpha components.</param>
		public abstract void SetSeparateSceneBlending( SceneBlendFactor sourceFactor, SceneBlendFactor destFactor, SceneBlendFactor sourceFactorAlpha, SceneBlendFactor destFactorAlpha );

		/// <summary>
		///     Sets the 'scissor region' ie the region of the target in which rendering can take place.
		/// </summary>
		/// <remarks>
		///     This method allows you to 'mask off' rendering in all but a given rectangular area
		///     as identified by the parameters to this method.
		///     <p/>
		///     Not all systems support this method. Check the <see cref="Axiom.Graphics.Capabilites"/> enum for the
		///     ScissorTest capability to see if it is supported.
		/// </remarks>
		/// <param name="enabled">True to enable the scissor test, false to disable it.</param>
		/// <param name="left">Left corner (in pixels).</param>
		/// <param name="top">Top corner (in pixels).</param>
		/// <param name="right">Right corner (in pixels).</param>
		/// <param name="bottom">Bottom corner (in pixels).</param>
		public abstract void SetScissorTest( bool enable, int left, int top, int right, int bottom );

		public void SetScissorTest( bool enable )
		{
			SetScissorTest( enable, 0, 0, 800, 600 );
		}

		/// <summary>
		///		This method allows you to set all the stencil buffer parameters in one call.
		/// </summary>
		/// <remarks>
		///		<para>
		///		The stencil buffer is used to mask out pixels in the render target, allowing
		///		you to do effects like mirrors, cut-outs, stencil shadows and more. Each of
		///		your batches of rendering is likely to ignore the stencil buffer, 
		///		update it with new values, or apply it to mask the output of the render.
		///		The stencil test is:<PRE>
		///		(Reference Value & Mask) CompareFunction (Stencil Buffer Value & Mask)</PRE>
		///		The result of this will cause one of 3 actions depending on whether the test fails,
		///		succeeds but with the depth buffer check still failing, or succeeds with the
		///		depth buffer check passing too.</para>
		///		<para>
		///		Unlike other render states, stencilling is left for the application to turn
		///		on and off when it requires. This is because you are likely to want to change
		///		parameters between batches of arbitrary objects and control the ordering yourself.
		///		In order to batch things this way, you'll want to use OGRE's separate render queue
		///		groups (see RenderQueue) and register a RenderQueueListener to get notifications
		///		between batches.</para>
		///		<para>
		///		There are individual state change methods for each of the parameters set using 
		///		this method. 
		///		Note that the default values in this method represent the defaults at system 
		///		start up too.</para>
		/// </remarks>
		/// <param name="function">The comparison function applied.</param>
		/// <param name="refValue">The reference value used in the comparison.</param>
		/// <param name="mask">
		///		The bitmask applied to both the stencil value and the reference value 
		///		before comparison.
		/// </param>
		/// <param name="stencilFailOp">The action to perform when the stencil check fails.</param>
		/// <param name="depthFailOp">
		///		The action to perform when the stencil check passes, but the depth buffer check still fails.
		/// </param>
		/// <param name="passOp">The action to take when both the stencil and depth check pass.</param>
		/// <param name="twoSidedOperation">
		///		If set to true, then if you render both back and front faces 
		///		(you'll have to turn off culling) then these parameters will apply for front faces, 
		///		and the inverse of them will happen for back faces (keep remains the same).
		/// </param>
		public abstract void SetStencilBufferParams( CompareFunction function, int refValue, int mask,
			StencilOperation stencilFailOp, StencilOperation depthFailOp, StencilOperation passOp, bool twoSidedOperation );

		/// <summary>
		///		Sets the surface parameters to be used during rendering an object.
		/// </summary>
		/// <param name="ambient"></param>
		/// <param name="diffuse"></param>
		/// <param name="specular"></param>
		/// <param name="emissive"></param>
		/// <param name="shininess"></param>
		/// <param name="tracking"></param>
		public abstract void SetSurfaceParams( ColorEx ambient, ColorEx diffuse, ColorEx specular, ColorEx emissive, float shininess, TrackVertexColor tracking );

		/// <summary>
		/// Sets whether or not rendering points using PointList will 
		/// render point sprites (textured quads) or plain points.
		/// </summary>
		/// <value></value>
		public abstract bool PointSprites
		{
			set;
		}

		/// <summary>
		/// Sets the size of points and how they are attenuated with distance.
		/// <remarks>
		/// When performing point rendering or point sprite rendering,
		/// point size can be attenuated with distance. The equation for
		/// doing this is attenuation = 1 / (constant + linear * dist + quadratic * d^2) .
		/// </remarks>
		/// </summary>
		/// <param name="size"></param>
		/// <param name="attenuationEnabled"></param>
		/// <param name="constant"></param>
		/// <param name="linear"></param>
		/// <param name="quadratic"></param>
		/// <param name="minSize"></param>
		/// <param name="maxSize"></param>
		public abstract void SetPointParameters( float size, bool attenuationEnabled, float constant, float linear, float quadratic, float minSize, float maxSize );

		/// <summary>
		///		Sets the details of a texture stage, to be used for all primitives
		///		rendered afterwards. User processes would
		///		not normally call this direct unless rendering
		///		primitives themselves - the SubEntity class
		///		is designed to manage materials for objects.
		///		Note that this method is called by SetMaterial.
		/// </summary>
		/// <param name="stage">The index of the texture unit to modify. Multitexturing hardware 
		/// can support multiple units (see TextureUnitCount)</param>
		/// <param name="enabled">Boolean to turn the unit on/off</param>
		/// <param name="textureName">The name of the texture to use - this should have
		///		already been loaded with TextureManager.Load.</param>
		public void SetTexture( int stage, bool enabled, string textureName )
		{
			// load the texture
			Texture texture = (Texture)TextureManager.Instance.GetByName( textureName );
			SetTexture( stage, enabled, texture );
		}

		public abstract void SetTexture( int stage, bool enabled, Texture texture );

		/// <summary>
		/// Binds a texture to a vertex sampler.
		/// </summary>
		/// <remarks>
		/// Not all rendersystems support separate vertex samplers. For those that
		/// do, you can set a texture for them, separate to the regular texture
		/// samplers, using this method. For those that don't, you should use the
		/// regular texture samplers which are shared between the vertex and
		/// fragment units; calling this method will throw an exception.
		/// <see>RenderSystemCapabilites.VertexTextureUnitsShared</see>
		/// </remarks>
		/// <param name="unit"></param>
		/// <param name="texture"></param>
		public virtual void SetVertexTexture( int unit, Texture texture )
		{
			throw new NotSupportedException(
				"This rendersystem does not support separate vertex texture samplers, " +
				"you should use the regular texture samplers which are shared between " +
				"the vertex and fragment units." );
		}

		/// <summary>
		///		Tells the hardware how to treat texture coordinates.
		/// </summary>
		/// <param name="stage"></param>
		/// <param name="texAddressingMode"></param>
        public abstract void SetTextureAddressingMode( int stage, UVWAddressing uvw );

		/// <summary>
		///    Tells the hardware what border color to use when texture addressing mode is set to Border
		/// </summary>
		/// <param name="state"></param>
		/// <param name="borderColor"></param>
		public abstract void SetTextureBorderColor( int stage, ColorEx borderColor );

		/// <summary>
		///		Sets the texture blend modes from a TextureLayer record.
		///		Meant for use internally only - apps should use the Material
		///		and TextureLayer classes.
		/// </summary>
		/// <param name="stage">Texture unit.</param>
		/// <param name="blendMode">Details of the blending modes.</param>
		public abstract void SetTextureBlendMode( int stage, LayerBlendModeEx blendMode );

		/// <summary>
		///		Sets a method for automatically calculating texture coordinates for a stage.
		/// </summary>
		/// <param name="stage">Texture stage to modify.</param>
		/// <param name="method">Calculation method to use</param>
		/// <param name="frustum">Frustum, only used for projective effects</param>
		public abstract void SetTextureCoordCalculation( int stage, TexCoordCalcMethod method, Frustum frustum );

		/// <summary>
		///		Sets the index into the set of tex coords that will be currently used by the render system.
		/// </summary>
		/// <param name="stage"></param>
		/// <param name="index"></param>
		public abstract void SetTextureCoordSet( int stage, int index );

		/// <summary>
		///		Sets the maximal anisotropy for the specified texture unit.
		/// </summary>
		/// <param name="stage"></param>
		/// <param name="index">maxAnisotropy</param>
		public abstract void SetTextureLayerAnisotropy( int stage, int maxAnisotropy );

		/// <summary>
		///		Sets the texture matrix for the specified stage.  Used to apply rotations, translations,
		///		and scaling to textures.
		/// </summary>
		/// <param name="stage"></param>
		/// <param name="xform"></param>
		public abstract void SetTextureMatrix( int stage, Matrix4 xform );

		/// <summary>
		///    Sets a single filter for a given texture unit.
		/// </summary>
		/// <param name="stage">The texture unit to set the filtering options for.</param>
		/// <param name="type">The filter type.</param>
		/// <param name="filter">The filter to be used.</param>
		public abstract void SetTextureUnitFiltering( int stage, FilterType type, FilterOptions filter );

		/// <summary>
		///		Sets the current viewport that will be rendered to.
		/// </summary>
		/// <param name="viewport"></param>
		public abstract void SetViewport( Viewport viewport );

		/// <summary>
		///    Unbinds the current GpuProgram of a given GpuProgramType.
		/// </summary>
		/// <param name="type"></param>
		public virtual void UnbindGpuProgram( GpuProgramType type )
		{
			switch ( type )
			{
				case GpuProgramType.Vertex:
					vertexProgramBound = false;
					break;
				case GpuProgramType.Fragment:
					fragmentProgramBound = false;
					break;
			}
		}

		/// <summary>
		///    Gets the bound status of a given GpuProgramType.
		/// </summary>
		/// <param name="type"></param>
		public bool IsGpuProgramBound( GpuProgramType type )
		{
			switch ( type )
			{
				case GpuProgramType.Vertex:
					return vertexProgramBound;
				case GpuProgramType.Fragment:
					return fragmentProgramBound;
			}
			return false;
		}

		/// <summary>
		///    Tells the rendersystem to use the attached set of lights (and no others) 
		///    up to the number specified (this allows the same list to be used with different
		///    count limits).
		/// </summary>
		/// <param name="lightList">List of lights.</param>
		/// <param name="limit">Max number of lights that can be used from the list currently.</param>
		public abstract void UseLights( LightList lightList, int limit );

		#endregion Methods

		#endregion Abstract Members

		/// <summary>
		///   Destroys a render target of any sort
		/// </summary>
		/// <param name="name"></param>
		public virtual void DestroyRenderTarget( string name )
		{
			RenderTarget rt = DetachRenderTarget( name );
			rt.Dispose();
		}

		/// <summary>
		///   Destroys a render window
		/// </summary>
		/// <param name="name"></param>
		public virtual void DestroyRenderWindow( string name )
		{
			DestroyRenderTarget( name );
		}

		/// <summary>
		///   Destroys a render texture
		/// </summary>
		/// <param name="name"></param>
		public virtual void DestroyRenderTexture( string name )
		{
			DestroyRenderTarget( name );
		}

		#region Overloaded Methods

		/// <summary>
		///		Converts a uniform projection matrix to one suitable for this render system.
		/// </summary>
		/// <remarks>
		///		Because different APIs have different requirements (some incompatible) for the
		///		projection matrix, this method allows each to implement their own correctly and pass
		///		back a generic Matrix4 for storage in the engine.
		///	 </remarks>
		/// <param name="matrix"></param>
		/// <returns></returns>
		public Matrix4 ConvertProjectionMatrix( Matrix4 matrix )
		{
			// create without consideration for Gpu programs by default
			return ConvertProjectionMatrix( matrix, false );
		}

		/// <summary>
		///		Builds a perspective projection matrix suitable for this render system.
		/// </summary>
		/// <remarks>
		///		Because different APIs have different requirements (some incompatible) for the
		///		projection matrix, this method allows each to implement their own correctly and pass
		///		back a generic Matrix4 for storage in the engine.
		///	 </remarks>
		/// <param name="fov">Field of view angle.</param>
		/// <param name="aspectRatio">Aspect ratio.</param>
		/// <param name="near">Near clipping plane distance.</param>
		/// <param name="far">Far clipping plane distance.</param>
		/// <returns></returns>
		public Matrix4 MakeProjectionMatrix( float fov, float aspectRatio, float near, float far )
		{
			// create without consideration for Gpu programs by default
			return MakeProjectionMatrix( fov, aspectRatio, near, far, false );
		}

		/// <summary>
		/// Builds a perspective projection matrix for the case when frustum is
		/// not centered around camera.
		/// <remarks>Viewport coordinates are in camera coordinate frame, i.e. camera is at the origin.</remarks>
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <param name="bottom"></param>
		/// <param name="top"></param>
		/// <param name="nearPlane"></param>
		/// <param name="farPlane"></param>
		/// <param name="forGpuProgram"></param>
		public abstract Matrix4 MakeProjectionMatrix( float left, float right, float bottom, float top, float nearPlane, float farPlane, bool forGpuProgram );

		/// <summary>
		/// Builds a perspective projection matrix for the case when frustum is
		/// not centered around camera.
		/// <remarks>Viewport coordinates are in camera coordinate frame, i.e. camera is at the origin.</remarks>
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <param name="bottom"></param>
		/// <param name="top"></param>
		/// <param name="nearPlane"></param>
		/// <param name="farPlane"></param>
		public Matrix4 MakeProjectionMatrix( float left, float right, float bottom, float top, float nearPlane, float farPlane )
		{
			return MakeProjectionMatrix( left, right, bottom, top, nearPlane, farPlane, false );
		}

		/// <summary>
		///		Builds a orthographic projection matrix suitable for this render system.
		/// </summary>
		/// <remarks>
		///		Because different APIs have different requirements (some incompatible) for the
		///		orthographic matrix, this method allows each to implement their own correctly and pass
		///		back a generic Matrix4 for storage in the engine.
		///	 </remarks>
		/// <param name="fov">Field of view angle.</param>
		/// <param name="aspectRatio">Aspect ratio.</param>
		/// <param name="near">Near clipping plane distance.</param>
		/// <param name="far">Far clipping plane distance.</param>
		/// <returns></returns>
		public Matrix4 MakeOrthoMatrix( float fov, float aspectRatio, float near, float far )
		{
			return MakeOrthoMatrix( fov, aspectRatio, near, far, false );
		}

		/// <summary>
		///		Sets a method for automatically calculating texture coordinates for a stage.
		/// </summary>
		/// <param name="stage">Texture stage to modify.</param>
		/// <param name="method">Calculation method to use</param>
		public void SetTextureCoordCalculation( int stage, TexCoordCalcMethod method )
		{
			SetTextureCoordCalculation( stage, method, null );
		}

		#region SetDepthBufferParams()

		public void SetDepthBufferParams()
		{
			SetDepthBufferParams( true, true, CompareFunction.LessEqual );
		}

		public void SetDepthBufferParams( bool depthTest )
		{
			SetDepthBufferParams( depthTest, true, CompareFunction.LessEqual );
		}

		public void SetDepthBufferParams( bool depthTest, bool depthWrite )
		{
			SetDepthBufferParams( depthTest, depthWrite, CompareFunction.LessEqual );
		}

		#endregion SetDepthBufferParams()

		#region SetStencilBufferParams()

		public void SetStencilBufferParams()
		{
			SetStencilBufferParams( CompareFunction.AlwaysPass, 0, unchecked( (int)0xffffffff ),
				StencilOperation.Keep, StencilOperation.Keep, StencilOperation.Keep, false );
		}

		public void SetStencilBufferParams( CompareFunction function )
		{
			SetStencilBufferParams( function, 0, unchecked( (int)0xffffffff ),
				StencilOperation.Keep, StencilOperation.Keep, StencilOperation.Keep, false );
		}

		public void SetStencilBufferParams( CompareFunction function, int refValue )
		{
			SetStencilBufferParams( function, refValue, unchecked( (int)0xffffffff ),
				StencilOperation.Keep, StencilOperation.Keep, StencilOperation.Keep, false );
		}

		public void SetStencilBufferParams( CompareFunction function, int refValue, int mask )
		{
			SetStencilBufferParams( function, refValue, mask,
				StencilOperation.Keep, StencilOperation.Keep, StencilOperation.Keep, false );
		}

		public void SetStencilBufferParams( CompareFunction function, int refValue, int mask,
			StencilOperation stencilFailOp )
		{

			SetStencilBufferParams( function, refValue, mask,
				stencilFailOp, StencilOperation.Keep, StencilOperation.Keep, false );
		}

		public void SetStencilBufferParams( CompareFunction function, int refValue, int mask,
			StencilOperation stencilFailOp, StencilOperation depthFailOp )
		{

			SetStencilBufferParams( function, refValue, mask,
				stencilFailOp, depthFailOp, StencilOperation.Keep, false );
		}

		public void SetStencilBufferParams( CompareFunction function, int refValue, int mask,
			StencilOperation stencilFailOp, StencilOperation depthFailOp, StencilOperation passOp )
		{

			SetStencilBufferParams( function, refValue, mask,
				stencilFailOp, depthFailOp, passOp, false );
		}

		#endregion SetStencilBufferParams()

		#region ClearFrameBuffer()

		public void ClearFrameBuffer( FrameBufferType buffers, ColorEx color, float depth )
		{
			ClearFrameBuffer( buffers, color, depth, 0 );
		}

		public void ClearFrameBuffer( FrameBufferType buffers, ColorEx color )
		{
			ClearFrameBuffer( buffers, color, 1.0f, 0 );
		}

		public void ClearFrameBuffer( FrameBufferType buffers )
		{
			ClearFrameBuffer( buffers, ColorEx.Black, 1.0f, 0 );
		}

		#endregion ClearFrameBuffer()

		#endregion Overloaded Methods

		#region Object overrides

		/// <summary>
		/// Returns the name of this RenderSystem.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return this.Name;
		}

		#endregion Object overrides

		#region DisposableObject Members

		/// <summary>
		/// Class level dispose method
		/// </summary>
		protected override void dispose( bool disposeManagedResources )
		{
			if ( !this.IsDisposed )
			{
				if ( disposeManagedResources )
				{
					if ( this.hardwareBufferManager != null )
					{
						if ( !this.hardwareBufferManager.IsDisposed )
							this.hardwareBufferManager.Dispose();

						this.hardwareBufferManager = null;
					}

					if ( this.textureManager != null )
					{
						if ( !textureManager.IsDisposed )
							textureManager.Dispose();

						this.textureManager = null;
					}
				}

				// There are no unmanaged resources to release, but
				// if we add them, they need to be released here.
			}

			// If it is available, make the call to the
			// base class's Dispose(Boolean) method
			base.dispose( disposeManagedResources );
		}

		#endregion DisposableObject Members
	}

}