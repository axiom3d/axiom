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
using Axiom.Math.Collections;
using Axiom.Media;
using Axiom.Graphics.Collections;
using Axiom.Core.Collections;

#endregion Namespace Declarations

namespace Axiom.Graphics
{
    /// <summary>
    ///    Defines the functionality of a 3D API
    /// </summary>
    ///    <remarks>
    ///        The RenderSystem class provides a base class
    ///        which abstracts the general functionality of the 3D API
    ///        e.g. Direct3D or OpenGL. Whilst a few of the general
    ///        methods have implementations, most of this class is
    ///        abstract, requiring a subclass based on a specific API
    ///        to be constructed to provide the full functionality.
    ///        <p/>
    ///        Note there are 2 levels to the interface - one which
    ///        will be used often by the caller of the engine library,
    ///        and one which is at a lower level and will be used by the
    ///        other classes provided by the engine. These lower level
    ///        methods are marked as internal, and are not accessible outside
    ///        of the Core library.
    ///    </remarks>
    public abstract class RenderSystem : DisposableObject
    {
        #region Constants

        /// <summary>
        ///        Default window title if one is not specified upon a call to <see cref="Initialize"/>.
        /// </summary>
        const string DefaultWindowTitle = "Axiom Window";

        // TODO: should this go into Config?
        const int NumRendertargetGroups = 10;

        #endregion Constants

        #region Inner Types
        
        public class RenderTargetMap: Dictionary<string, RenderTarget>
        {
        }

        public class RenderTargetPriorityMap : MultiMap<RenderTargetPriority, RenderTarget>
        {
        }

        /// <summary>
        /// Dummy structure for render system contexts - implementing RenderSystems can extend
        /// as needed
        /// </summary>
        public class RenderSystemContext: IDisposable
        {
            public virtual void Dispose()
            {
            }
        }

        #endregion

        #region Fields

        /// <summary>
        ///        List of current render targets (i.e. a <see cref="RenderWindow"/>, or a<see cref="RenderTexture"/>) by priority
        /// </summary>
        protected RenderTargetPriorityMap prioritizedRenderTargets = new RenderTargetPriorityMap();
        
        /// <summary>
        ///        List of current render targets (i.e. a <see cref="RenderWindow"/>, or a<see cref="RenderTexture"/>)
        /// </summary>
        protected RenderTargetMap renderTargets = new RenderTargetMap();
        /// <summary>
        ///        A reference to the texture management class specific to this implementation.
        /// </summary>
        protected TextureManager textureManager;
        /// <summary>
        ///        A reference to the hardware vertex/index buffer manager specific to this API.
        /// </summary>
        protected HardwareBufferManager hardwareBufferManager;
        /// <summary>
        ///        Current hardware culling mode.
        /// </summary>
        protected CullingMode _cullingMode;
        /// <summary>
        ///        Are we syncing frames with the refresh rate of the screen?
        /// </summary>
        protected bool isVSync;
        /// <summary>
        ///        Current depth write setting.
        /// </summary>
        protected bool depthWrite;
        /// <summary>
        ///        Number of current active lights.
        /// </summary>
        protected int numCurrentLights;
        /// <summary>
        ///        Reference to the config options for the graphics engine.
        /// </summary>
        protected ConfigOptionMap engineConfig = new ConfigOptionMap();
        /// <summary>
        ///        Active viewport (dest for future rendering operations) and target.
        /// </summary>
        private Viewport _activeViewport;
        /// <summary>
        ///        Active render target.
        /// </summary>
        protected RenderTarget activeRenderTarget;
        /// <summary>
        ///        Number of faces currently rendered this frame.
        /// </summary>
        protected int _faceCount;
        /// <summary>
        /// Number of batches currently rendered this frame.
        /// </summary>
        protected int batchCount;
        /// <summary>
        ///        Number of vertexes currently rendered this frame.
        /// </summary>
        protected int vertexCount;
        /// <summary>
        /// Number of times to render the current state
        /// </summary>
        protected int currentPassIterationCount;
        
        /// <summary>
        ///        Capabilites of the current hardware (populated at startup).
        /// </summary>
        private RenderSystemCapabilities _realCapabilities;

        private bool _useCustomCapabilities;

        /// <summary>
        ///        Saved set of world matrices.
        /// </summary>
        protected Matrix4[] worldMatrices = new Matrix4[ 256 ];
        /// <summary>
        ///     Flag for whether vertex winding needs to be inverted, useful for reflections.
        /// </summary>
        protected bool invertVertexWinding;

        protected bool vertexProgramBound = false;
        protected bool fragmentProgramBound = false;
        protected bool geometryProgramBound;

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


        /** The Active GPU programs and gpu program parameters*/
        protected GpuProgramParameters activeVertexGpuProgramParameters;
        protected GpuProgramParameters activeGeometryGpuProgramParameters;
        protected GpuProgramParameters activeFragmentGpuProgramParameters;

        [OgreVersion( 1, 7 )] protected PlaneList clipPlanes = new PlaneList();
        [OgreVersion( 1, 7 )] protected bool clipPlanesDirty;

        #endregion Fields

        #region Constructor

        /// <summary>
        ///        Base constructor.
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
            _cullingMode = CullingMode.Clockwise;
        }

        #endregion

        #region Virtual Members

        #region Properties

        #region RenderTarget

        /// <summary>
        ///    Set current render target to target, enabling its device context if needed
        /// </summary>
        [OgreVersion( 1, 7 )]
        public abstract RenderTarget RenderTarget { set; }

        #endregion

        #region WaitForVerticalBlank

        /// <summary>
        /// Defines whether or now fullscreen render windows wait for the vertical blank before flipping buffers.
        /// </summary>
        /// <remarks>
        /// By default, all rendering windows wait for a vertical blank (when the CRT beam turns off briefly to move
        /// from the bottom right of the screen back to the top left) before flipping the screen buffers. This ensures
        /// that the image you see on the screen is steady. However it restricts the frame rate to the refresh rate of
        /// the monitor, and can slow the frame rate down. You can speed this up by not waiting for the blank, but
        /// this has the downside of introducing 'tearing' artefacts where part of the previous frame is still displayed
        /// as the buffers are switched. Speed vs quality, you choose.
        /// </remarks>
        /// <note>
        /// Has NO effect on windowed mode render targets. Only affects fullscreen mode.
        /// </note>
        [OgreVersion(1, 7)]
        public bool WaitForVerticalBlank { get; set; }

        #endregion

        #region GlobalInstanceVertexBuffer

        private HardwareVertexBuffer _globalInstanceVertexBuffer;

        /// <summary>
        ///        a global vertex buffer for global instancing
        /// </summary>
        [OgreVersion(1, 7)]
        public HardwareVertexBuffer GlobalInstanceVertexBuffer
        {
            get
            {
                return _globalInstanceVertexBuffer;
            }
            set
            {
                if (value != null && !value.IsInstanceData)
                {
                    throw new AxiomException( "A none instance data vertex buffer was set to be the global instance vertex buffer." );
                }
                _globalInstanceVertexBuffer = value;
            }
        }

        #endregion

        #region GlobalInstanceVertexBufferVertexDeclaration

        /// <summary>
        ///        a vertex declaration for the global vertex buffer for the global instancing
        /// </summary>
        [OgreVersion(1, 7)]
        public VertexDeclaration GlobalInstanceVertexBufferVertexDeclaration { get; set; }

        #endregion

        #region GlobalNumberOfInstances

        /// <summary>
        ///        the number of global instances (this number will be multiply by the render op instance number) 
        /// </summary>
        [OgreVersion(1, 7)]
        public int GlobalNumberOfInstances { get; set; }

        #endregion

        #region AreFixedFunctionLightsInViewSpace

        /// <summary>
        ///        Are fixed-function lights provided in view space? Affects optimisation.
        /// </summary>
        [OgreVersion(1, 7)]
        public bool AreFixedFunctionLightsInViewSpace
        {
            get
            {
                return false;
            }
        }

        #endregion

        #region PointSpritesEnabled

        /// <summary>
        /// Sets whether or not rendering points using OT_POINT_LIST will 
        /// render point sprites (textured quads) or plain points.
        /// </summary>
        [OgreVersion( 1, 7 )]
        public abstract bool PointSpritesEnabled { set; }

        #endregion

        #region RenderSystemCapabilities

        /// <summary>
        ///        Gets a set of hardware capabilities queryed by the current render system.
        /// </summary>
        public RenderSystemCapabilities MutableCapabilities { get; private set; }

        /// <summary>
        ///  Gets the capabilities of the render system
        /// </summary>
        public RenderSystemCapabilities Capabilities
        {
            get
            {
                return MutableCapabilities;
            }
        }

        #endregion

        #region Viewport

        /// <summary>
        /// Get or set the current active viewport for rendering.
        /// </summary>
        [OgreVersion(1, 7)]
        public virtual Viewport Viewport
        {
            get
            {
                return _activeViewport;
            }
            set
            {
                throw new MethodAccessException( "Abstract call" );
            }
        }

        #endregion

        #region CullingMode

        /// <summary>
        ///    Gets/Sets the culling mode for the render system based on the 'vertex winding'.
        /// </summary>
        /// <remarks>
        ///        A typical way for the rendering engine to cull triangles is based on the
        ///        'vertex winding' of triangles. Vertex winding refers to the direction in
        ///        which the vertices are passed or indexed to in the rendering operation as viewed
        ///        from the camera, and will wither be clockwise or counterclockwise.  The default is <see cref="CullingMode.Clockwise"/>  
        ///        i.e. that only triangles whose vertices are passed/indexed in counterclockwise order are rendered - this 
        ///        is a common approach and is used in 3D studio models for example. You can alter this culling mode 
        ///        if you wish but it is not advised unless you know what you are doing. You may wish to use the 
        ///        <see cref="CullingMode.None"/> option for mesh data that you cull yourself where the vertex winding is uncertain.
        /// </remarks>
        [OgreVersion(1, 7)]
        public virtual CullingMode CullingMode
        {
            get
            {
                return _cullingMode;
            }
            set
            {
                throw new MethodAccessException("Abstract call");
            }
        }

        #endregion

        #region DepthBufferCheckEnabled

        /// <summary>
        ///    Sets whether or not the depth buffer check is performed before a pixel write
        /// </summary>
        [OgreVersion( 1, 7, "Default = true" )]
        public abstract bool DepthBufferCheckEnabled { set; }

        #endregion

        #region DepthBufferWriteEnabled

        /// <summary>
        /// Sets whether or not the depth buffer is updated after a pixel write.
        /// </summary>
        [OgreVersion(1, 7, "Default = true")]
        public abstract bool DepthBufferWriteEnabled { set; }

        #endregion

        #region DepthBufferFunction

        /// <summary>
        /// Sets the comparison function for the depth buffer check.
        /// Advanced use only - allows you to choose the function applied to compare the depth values of
        /// new and existing pixels in the depth buffer. Only an issue if the depth buffer check is enabled
        /// <see cref="DepthBufferCheckEnabled"/>
        /// </summary>
        [OgreVersion(1, 7, "Default = (CompareFunction.LessEqual")]
        public abstract CompareFunction DepthBufferFunction { set; }

        #endregion

        #region DriverVersion

        protected DriverVersion _driverVersion;

        /// <summary>
        ///  Returns the driver version.
        /// </summary>
        [OgreVersion(1, 7)]
        public virtual DriverVersion DriverVersion
        {
            get
            {
                return _driverVersion;
            }
        }

        #endregion

        #region DefaultViewportMaterialScheme

        /// <summary>
        ///  Returns the driver version.
        /// </summary>
        [OgreVersion(1, 7, "No RTSHADER support")]
        public virtual string DefaultViewportMaterialScheme
        {
            get
            {
                return MaterialManager.DefaultSchemeName;
            }
        }

        #endregion

        #region ClipPlanes

        /// <summary>
        ///  Returns the driver version.
        /// </summary>
        [OgreVersion(1, 7)]
        public virtual PlaneList ClipPlanes
        {
            set
            {
                if (value == null)
                    throw new ArgumentNullException(); // Ogre passes this as ref so must be non null
                if (!value.Equals(clipPlanes))
                {
                    clipPlanes = value;
                    clipPlanesDirty = true;
                }
            }
        }

        #endregion

        #region ConfigOptions

        /// <summary>
        /// Gets a dataset with the options set for the rendering system.
        /// </summary>
        [OgreVersion(1, 7)]
        public abstract ConfigOptionMap ConfigOptions { get; }

        #endregion

        #region FaceCount

        /// <summary>
        ///        Number of faces rendered during the current frame so far.
        /// </summary>
        [OgreVersion(1, 7)]
        public virtual int FaceCount
        {
            get
            {
                return _faceCount;
            }
        }

        #endregion

        #region BatchCount

        /// <summary>
        ///        Number of batches rendered during the current frame so far.
        /// </summary>
        [OgreVersion(1, 7)]
        public virtual int BatchCount
        {
            get
            {
                return batchCount;
            }
        }

        #endregion

        #region VertexCount

        /// <summary>
        ///        Number of vertices processed during the current frame so far.
        /// </summary>
        [OgreVersion(1, 7)]
        public virtual int VertexCount
        {
            get
            {
                return vertexCount;
            }
        }

        #endregion

        #region ColorVertexElementType

        [OgreVersion(1, 7)]
        public abstract VertexElementType ColorVertexElementType { get; }

        #endregion

        #region VertexDeclaration

        /// <summary>
        /// Sets the current vertex declaration, ie the source of vertex data.
        /// </summary>
        [OgreVersion(1, 7)]
        public abstract VertexDeclaration VertexDeclaration { get; }

        #endregion

        #region VertexBufferBinding

        /// <summary>
        /// Sets the current vertex buffer binding state.
        /// </summary>
        [OgreVersion(1, 7)]
        public abstract VertexBufferBinding VertexBufferBinding { get; }

        #endregion

        #region CurrentPassIterationCount

        /// <summary>
        ///    Number of times to render the current state.
        /// </summary>
        /// <remarks>Set the current multi pass count value.  This must be set prior to 
        /// calling render() if multiple renderings of the same pass state are 
        /// required.
        /// </remarks>
        [OgreVersion(1, 7)]
        public virtual int CurrentPassIterationCount
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

        #endregion

        #region InvertVertexWinding

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

        #endregion

        #region IsVSync

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

        #endregion

        #region Name

        /// <summary>
        /// Gets the name of this RenderSystem based on it's assembly attribute Title.
        /// </summary>
        [OgreVersion(1,7, "abstract in Ogre; Axiom uses reflection to supply a default value")]
        public virtual string Name
        {
            get
            {
                var attribute =
                    (AssemblyTitleAttribute)Attribute.GetCustomAttribute( GetType().Assembly, typeof( AssemblyTitleAttribute ), false );

                if ( attribute != null )
                    return attribute.Title;
                //else
                    return "Not Found";
            }
        }

        #endregion

        #region RenderTargetCount

        public int RenderTargetCount
        {
            get
            {
                return renderTargets.Count;
            }
        }

        #endregion

        #region TotalRenderCalls

        public static long TotalRenderCalls
        {
            get
            {
                return totalRenderCalls;
            }
        }

        #endregion

        #region Listener

        protected event Action<string, NameValuePairList> eventListeners;

        [OgreVersion(1, 7)]
        public virtual event Action<string, NameValuePairList> Listener
        {
            add
            {
                eventListeners += value;
            }
            remove
            {
                eventListeners -= value;
            }
        }

        #endregion

        #region RenderSystemEvents

        protected List<string> eventNames = new List<string>();

        public virtual List<string> RenderSystemEvents
        {
            get
            {
                return eventNames;
            }
        }

        #endregion

        #region DisplayMonitorCount

        public abstract int DisplayMonitorCount { get; }

        #endregion

        #endregion Properties

        #region Methods


        /// <summary>
        /// Force the render system to use the special capabilities. Can only be called
        /// before the render system has been fully initializer (before createWindow is called) 
        /// </summary>
        /// <param name="capabilities">
        /// capabilities has to be a subset of the real capabilities and the caller is 
        /// responsible for deallocating capabilities.
        /// </param>
        [OgreVersion(1, 7)]
        public virtual void UseCustomRenderSystemCapabilities(RenderSystemCapabilities capabilities)
        {
            if (_realCapabilities != null)
            {
                throw new AxiomException("Custom render capabilities must be set before the RenderSystem is initialised.");
            }

            MutableCapabilities = capabilities;
            _useCustomCapabilities = true;
        }

        /// <summary>
        /// Retrieves an existing DepthBuffer or creates a new one suited for the given RenderTarget and sets it.
        /// </summary>
        /// <remarks>
        /// RenderTarget's pool ID is respected. <see cref="RenderTarget.DepthBufferPool"/>
        /// </remarks>
        /// <param name="renderTarget"></param>
        [OgreVersion(1, 7, "Not implemented, yet")]
        public virtual void SetDepthBufferFor(RenderTarget renderTarget)
        {
            var poolId = renderTarget.DepthBufferPool;
            throw new NotImplementedException();
        }

        /// <summary>
        /// Sets the depth bias, NB you should use the Material version of this.
        /// </summary>
        /// <param name="constantBias"></param>
        /// <param name="slopeScaleBias"></param>
        [OgreVersion(1, 7)]
        public abstract void SetDepthBias( float constantBias, float slopeScaleBias = 0.0f );

       
        /// <summary>
        /// Tell the render system whether to derive a depth bias on its own based on 
        /// the values passed to it in setCurrentPassIterationCount.
        /// The depth bias set will be baseValue + iteration * multiplier
        /// </summary>
        /// <param name="derive">true to tell the RS to derive this automatically</param>
        /// <param name="baseValue">The base value to which the multiplier should be added</param>
        /// <param name="multiplier">The amount of depth bias to apply per iteration</param>
        /// <param name="slopeScale">The constant slope scale bias for completeness</param>
        [OgreVersion(1, 7)]
        public virtual void SetDerivedDepthBias(bool derive, float baseValue = 0.0f, float multiplier = 0.0f, float slopeScale = 0.0f )
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
        [OgreVersion(1, 7)]
        public abstract string ValidateConfigOptions();
       

        /// <summary>
        ///    Attaches a render target to this render system.
        /// </summary>
        /// <param name="target">Reference to the render target to attach to this render system.</param>
        [OgreVersion(1, 7)]
        public virtual void AttachRenderTarget( RenderTarget target )
        {
            Debug.Assert((int)target.Priority < NumRendertargetGroups);
            renderTargets.Add(target.Name, target);
            prioritizedRenderTargets.Add( target.Priority, target );
        }

        /// <summary>
        ///        The RenderSystem will keep a count of tris rendered, this resets the count.
        /// </summary>
        [OgreVersion(1, 7)]
        public virtual void BeginGeometryCount()
        {
            batchCount = vertexCount = _faceCount = 0;
        }


        /// <summary>
        ///        Detaches the render target from this render system.
        /// </summary>
        /// <param name="name">Name of the render target to detach.</param>
        /// <returns>the render target that was detached</returns>
        [OgreVersion(1, 7)]
        public virtual RenderTarget DetachRenderTarget( string name )
        {
            RenderTarget ret;
            if (renderTargets.TryGetValue(name, out ret))
            {
                // Remove the render target from the priority groups.
                prioritizedRenderTargets.RemoveWhere((k, v) => v == ret);
            }

            // If detached render target is the active render target, reset active render target
            if (ret == activeRenderTarget)
                activeRenderTarget = null;

            return ret;
        }

        [OgreVersion(1, 7)]
        public abstract string GetErrorDescription( int errorNumber );

        /// <summary>
        ///        Turns off a texture unit if not needed.
        /// </summary>
        [OgreVersion(1, 7)]
        public virtual void DisableTextureUnit( int texUnit )
        {
            SetTexture( texUnit, false, (Texture)null );
        }

        /// <summary>
        /// Disables all texture units from the given unit upwards
        /// </summary>
        [OgreVersion(1, 7)]
        public virtual void DisableTextureUnitsFrom( int texUnit )
        {
            var disableTo = Config.MaxTextureLayers;
            if (disableTo > disabledTexUnitsFrom)
                disableTo = disabledTexUnitsFrom;
            disabledTexUnitsFrom = texUnit;
            for ( var i = texUnit; i < disableTo; ++i )
            {
                DisableTextureUnit( i );
            }
        }

        /// <summary>
        ///     Utility method for initializing all render targets attached to this rendering system.
        /// </summary>
        [OgreVersion(1, 7)]
        public virtual void InitRenderTargets()
        {
            // init stats for each render target
            foreach ( KeyValuePair<string, RenderTarget> item in renderTargets )
            {
                item.Value.ResetStatistics();
            }
        }

        /// <summary>
        /// Add a user clipping plane.
        /// </summary>
        [OgreVersion(1, 7)]
        public virtual void AddClipPlane(Plane p)
        {
            clipPlanes.Add(p);
            clipPlanesDirty = true;
        }

        /// <summary>
        /// Add a user clipping plane.
        /// </summary>
        [OgreVersion(1, 7)]
        public virtual void AddClipPlane(Real a, Real b, Real c, Real d)
        {
            AddClipPlane(new Plane(new Vector3(a, b, c), d));
        }

        /// <summary>
        /// Clears the user clipping region.
        /// </summary>
        [OgreVersion(1, 7)]
        public virtual void ResetClipPlanes()
        {
            if (clipPlanes.Count != 0)
            {
                clipPlanes.Clear();
                clipPlanesDirty = true;
            }
        }

        /// <summary>
        ///        Utility method to notify all render targets that a camera has been removed, 
        ///        incase they were referring to it as their viewer. 
        /// </summary>
        /// <param name="camera">Camera being removed.</param>
        [OgreVersion(1, 7)]
        public virtual void NotifyCameraRemoved( Camera camera )
        {
            foreach ( var item in renderTargets )
            {
                item.Value.NotifyCameraRemoved( camera );
            }
        }

        /// <summary>
        ///        Render something to the active viewport.
        /// </summary>
        /// <remarks>
        ///        Low-level rendering interface to perform rendering
        ///        operations. Unlikely to be used directly by client
        ///        applications, since the <see cref="SceneManager"/> and various support
        ///        classes will be responsible for calling this method.
        ///        Can only be called between <see cref="BeginScene"/> and <see cref="EndScene"/>
        /// </remarks>
        /// <param name="op">
        ///        A rendering operation instance, which contains details of the operation to be performed.
        ///    </param>
        public virtual void Render( RenderOperation op )
        {
            int val;

            val = op.useIndices ? op.indexData.indexCount : op.vertexData.vertexCount;

            val *= op.NumberOfInstances;

            // account for a pass having multiple iterations
            if ( currentPassIterationCount > 1 )
                val *= currentPassIterationCount;

            currentPassIterationNum = 0;

            // calculate faces
            switch ( op.operationType )
            {
                case OperationType.TriangleList:
                    _faceCount += val / 3;
                    break;
                case OperationType.TriangleStrip:
                case OperationType.TriangleFan:
                    _faceCount += val - 2;
                    break;
                case OperationType.PointList:
                case OperationType.LineList:
                case OperationType.LineStrip:
                    break;
            }

            // increment running vertex count
            vertexCount += op.vertexData.vertexCount;
            batchCount += currentPassIterationCount;

            // sort out clip planes
            // have to do it here in case of matrix issues
            if (clipPlanesDirty)
            {
                SetClipPlanesImpl(clipPlanes);
                clipPlanesDirty = false;
            }
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
        /// Utility function for setting all the properties of a texture unit at once.
        /// This method is also worth using over the individual texture unit settings because it
        /// only sets those settings which are different from the current settings for this
        /// unit, thus minimising render state changes.
        /// </summary>
        /// <param name="texUnit"></param>
        /// <param name="tl"></param>
        [OgreVersion(1, 7, "resolving texture from resourcemanager atm")]
        public virtual void SetTextureUnitSettings(int texUnit, TextureUnitState tl)
        {
            // TODO: implement TextureUnitState.TexturePtr
            // var tex = tl.TexturePtr
            var tex = (Texture)TextureManager.Instance.GetByName( tl.TextureName );

            // Vertex Texture Binding?
            if ( Capabilities.HasCapability(Graphics.Capabilities.VertexTextureFetch)
                 && !Capabilities.VertexTextureUnitsShared )
            {
                if ( tl.BindingType == TextureBindingType.Vertex )
                {
                    // Bind Vertex Texture
                    SetVertexTexture(texUnit, tex);
                    // bind nothing to fragment unit (hardware isn't shared but fragment
                    // unit can't be using the same index
                    SetTexture( texUnit, true, (Texture)null );
                }
                else
                {
                    // vice versa
                    SetVertexTexture(texUnit, null);
                    SetTexture(texUnit, true, tex);
                }

            }
            else
            {
                // Shared vertex / fragment textures or no vertex texture support
                // Bind texture (may be blank)
                SetTexture(texUnit, true, tex);
            }

            // Set texture coordinate set
            SetTextureCoordSet(texUnit, tl.TextureCoordSet);

            // Texture layer filtering
            SetTextureUnitFiltering(
                texUnit,
                tl.GetTextureFiltering( FilterType.Min ),
                tl.GetTextureFiltering( FilterType.Mag ),
                tl.GetTextureFiltering( FilterType.Mip ) );

            // Texture layer anistropy
            SetTextureLayerAnisotropy( texUnit, tl.TextureAnisotropy );

            // Set mipmap biasing
            SetTextureMipmapBias( texUnit, tl.TextureMipmapBias );

            // set the texture blending modes
            // NOTE: Color before Alpha is important
            SetTextureBlendMode(texUnit, tl.ColorBlendMode);
            SetTextureBlendMode(texUnit, tl.AlphaBlendMode);

            // Texture addressing mode
            var uvw = tl.TextureAddressingMode;
            SetTextureAddressingMode( texUnit, uvw );

            // Set the texture border color only if needed.
            if (    uvw.U == TextureAddressing.Border
                 || uvw.V == TextureAddressing.Border
                 || uvw.W == TextureAddressing.Border )
            {
                SetTextureBorderColour(texUnit, tl.TextureBorderColor);
            }

            // Set texture Effects
            var anyCalcs = false;
            // TODO: Change TextureUnitState Effects to use Enumerator
            for ( var i = 0; i < tl.NumEffects; i++ )
            {
                var effect = tl.GetEffect( i );

                switch ( effect.type )
                {
                    case TextureEffectType.EnvironmentMap:
                        switch ( (EnvironmentMap)effect.subtype )
                        {
                            case EnvironmentMap.Curved:
                                SetTextureCoordCalculation( texUnit, TexCoordCalcMethod.EnvironmentMap );
                                break;
                            case EnvironmentMap.Planar:
                                SetTextureCoordCalculation( texUnit, TexCoordCalcMethod.EnvironmentMapPlanar );
                                break;
                            case EnvironmentMap.Reflection:
                                SetTextureCoordCalculation( texUnit, TexCoordCalcMethod.EnvironmentMapReflection );
                                break;
                            case EnvironmentMap.Normal:
                                SetTextureCoordCalculation( texUnit, TexCoordCalcMethod.EnvironmentMapNormal );
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
                        SetTextureCoordCalculation( texUnit, TexCoordCalcMethod.ProjectiveTexture, effect.frustum );
                        anyCalcs = true;
                        break;
                } // switch
            } // for

            // Ensure any previous texcoord calc settings are reset if there are now none
            if ( !anyCalcs )
            {
                SetTextureCoordCalculation( texUnit, TexCoordCalcMethod.None );
            }

            // set the texture matrix to that of the current layer for any transformations
            SetTextureMatrix( texUnit, tl.TextureMatrix );
        }

        /// <summary>
        ///    Sets multiple world matrices (vertex blending).
        /// </summary>
        [OgreVersion(1, 7)]
        public virtual void SetWorldMatrices(Matrix4[] matrices, ushort count)
        {
            // Do nothing with these matrices here, it never used for now,
            // derived class should take care with them if required.

            // reset the hardware world matrix to identity
            WorldMatrix = Matrix4.Identity;
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
        ///        Shuts down the RenderSystem.
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
        /// <param name="swapBuffers"></param>
        [OgreVersion(1, 7)]
        public virtual void UpdateAllRenderTargets( bool swapBuffers = true )
        {
            // Update all in order of priority
            // This ensures render-to-texture targets get updated before render windows
            foreach ( var targets in prioritizedRenderTargets )
            {
                foreach (var target in targets.Value)
                {
                    // only update if it is active
                    if ( target.IsActive && target.IsAutoUpdated )
                    {
                        target.Update( swapBuffers );
                    }
                }
            }
        }

        /// <summary>
        /// Internal method for swapping all the buffers on all render targets,
        /// if <see cref="UpdateAllRenderTargets"/> was called with a 'false' parameter.
        /// </summary>
        [OgreVersion(1, 7)]
        public virtual void SwapAllRenderTargetBuffers( bool waitForVSync = true )
        {
            // Update all in order of priority
            // This ensures render-to-texture targets get updated before render windows
            foreach (var targets in prioritizedRenderTargets)
            {
                foreach ( var target in targets.Value )
                {
                    // only update if it is active
                    if ( target.IsActive && target.IsAutoUpdated )
                    {
                        target.SwapBuffers( waitForVSync );
                    }
                }
            }
        }

        /// <summary>
        /// Returns a pointer to the render target with the passed name, or null if that
        /// render target cannot be found.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        [OgreVersion(1, 7)]
        public virtual RenderTarget GetRenderTarget(string name)
        {
            RenderTarget ret;
            renderTargets.TryGetValue(name, out ret);
            return ret;
        }

        #endregion Methods

        #endregion Virtual Members

        #region Abstract Members

        #region Properties

        /// <summary>
        ///        Sets the color & strength of the ambient (global directionless) light in the world.
        /// </summary>
        [OgreVersion(1, 7, "Axiom interface uses ColorEx while Ogre uses a ternary (r,g,b) setter")]
        public abstract ColorEx AmbientLight { set; }

        [OgreVersion(1, 7)]
        public abstract ShadeOptions ShadingType { set; }



        /// <summary>
        ///        Gets/Sets whether or not the depth buffer is updated after a pixel write.
        /// </summary>
        /// <value>
        ///        If true, the depth buffer is updated with the depth of the new pixel if the depth test succeeds.
        ///        If false, the depth buffer is left unchanged even if a new pixel is written.
        /// </value>
        public abstract bool DepthWrite
        {
            get;
            set;
        }

        /// <summary>
        ///        Gets/Sets whether or not the depth buffer check is performed before a pixel write.
        /// </summary>
        /// <value>
        ///        If true, the depth buffer is tested for each pixel and the frame buffer is only updated
        ///        if the depth function test succeeds. If false, no test is performed and pixels are always written.
        /// </value>
        public abstract bool DepthCheck
        {
            get;
            set;
        }

        /// <summary>
        ///        Gets/Sets the comparison function for the depth buffer check.
        /// </summary>
        /// <remarks>
        ///        Advanced use only - allows you to choose the function applied to compare the depth values of
        ///        new and existing pixels in the depth buffer. Only an issue if the depth buffer check is enabled.
        /// <seealso cref="DepthCheck"/>
        /// </remarks>
        /// <value>
        ///        The comparison between the new depth and the existing depth which must return true
        ///        for the new pixel to be written.
        /// </value>
        public abstract CompareFunction DepthFunction
        {
            get;
            set;
        }

        /// <summary>
        ///        Gets/Sets the depth bias.
        /// </summary>
        /// <remarks>
        ///        When polygons are coplanar, you can get problems with 'depth fighting' where
        ///        the pixels from the two polys compete for the same screen pixel. This is particularly
        ///        a problem for decals (polys attached to another surface to represent details such as
        ///        bulletholes etc.).
        ///        <p/>
        ///        A way to combat this problem is to use a depth bias to adjust the depth buffer value
        ///        used for the decal such that it is slightly higher than the true value, ensuring that
        ///        the decal appears on top.
        /// </remarks>
        /// <value>The bias value, should be between 0 and 16.</value>
        public abstract float DepthBias
        {
            get;
            set;
        }

        /// <summary>
        ///        Returns the horizontal texel offset value required for mapping 
        ///        texel origins to pixel origins in this rendersystem.
        /// </summary>
        /// <remarks>
        ///        Since rendersystems sometimes disagree on the origin of a texel, 
        ///        mapping from texels to pixels can sometimes be problematic to 
        ///        implement generically. This method allows you to retrieve the offset
        ///        required to map the origin of a texel to the origin of a pixel in
        ///        the horizontal direction.
        /// </remarks>
        [OgreVersion(1, 7)]
        public abstract float HorizontalTexelOffset
        {
            get;
        }

        /// <summary>
        ///        Gets/Sets whether or not dynamic lighting is enabled.
        ///        <p/>
        ///        If true, dynamic lighting is performed on geometry with normals supplied, geometry without
        ///        normals will not be displayed. If false, no lighting is applied and all geometry will be full brightness.
        /// </summary>
        public abstract bool LightingEnabled
        {
            set;
        }

        [OgreVersion(1, 7)]
        public bool WBufferEnabled
        { get; set; }

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
        public abstract bool NormaliseNormals
        {
            set;
        }

        /// <summary>
        ///        Gets/Sets the current projection matrix.
        ///    </summary>
        [OgreVersion(1, 7)]
        public abstract Matrix4 ProjectionMatrix
        {
            set;
        }

        /// <summary>
        ///        Gets/Sets how to rasterise triangles, as points, wireframe or solid polys.
        /// </summary>
        [OgreVersion(1, 7)]
        public abstract PolygonMode PolygonMode
        {
            set;
        }

        /// <summary>
        ///        Gets/Sets the type of light shading required (default = Gouraud).
        /// </summary>
        public abstract Shading ShadingMode
        {
            get;
            set;
        }

        /// <summary>
        ///        Turns stencil buffer checking on or off. 
        /// </summary>
        ///    <remarks>
        ///        Stencilling (masking off areas of the rendering target based on the stencil 
        ///        buffer) can be turned on or off using this method. By default, stencilling is
        ///        disabled.
        ///    </remarks>
        [OgreVersion(1, 7)]
        public abstract bool StencilCheckEnabled
        {
            set;
        }

        /// <summary>
        ///        Returns the vertical texel offset value required for mapping 
        ///        texel origins to pixel origins in this rendersystem.
        /// </summary>
        /// <remarks>
        ///        Since rendersystems sometimes disagree on the origin of a texel, 
        ///        mapping from texels to pixels can sometimes be problematic to 
        ///        implement generically. This method allows you to retrieve the offset
        ///        required to map the origin of a texel to the origin of a pixel in
        ///        the vertical direction.
        /// </remarks>
        [OgreVersion(1, 7)]
        public abstract float VerticalTexelOffset
        {
            get;
        }

        /// <summary>
        ///        Gets/Sets the current view matrix.
        ///    </summary>
        [OgreVersion(1, 7)]
        public abstract Matrix4 ViewMatrix
        {
            set;
        }

        /// <summary>
        ///    Sets the current world matrix.
        /// </summary>
        [OgreVersion(1, 7)]
        public abstract Matrix4 WorldMatrix
        {
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
        [OgreVersion(1, 7)]
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
        [OgreVersion(1, 7)]
        public abstract Real MaximumDepthInputValue
        {
            get;
        }

        #endregion Properties

        #region Methods

        /// <summary>
        ///        Update a perspective projection matrix to use 'oblique depth projection'.
        /// </summary>
        /// <remarks>
        ///        This method can be used to change the nature of a perspective 
        ///        transform in order to make the near plane not perpendicular to the 
        ///        camera view direction, but to be at some different orientation. 
        ///        This can be useful for performing arbitrary clipping (e.g. to a 
        ///        reflection plane) which could otherwise only be done using user
        ///        clip planes, which are more expensive, and not necessarily supported
        ///        on all cards.
        /// </remarks>
        /// <param name="projMatrix">
        ///        The existing projection matrix. Note that this must be a
        ///        perspective transform (not orthographic), and must not have already
        ///        been altered by this method. The matrix will be altered in-place.
        /// </param>
        /// <param name="plane">
        ///        The plane which is to be used as the clipping plane. This
        ///        plane must be in CAMERA (view) space.
        ///    </param>
        /// <param name="forGpuProgram">Is this for use with a Gpu program or fixed-function transforms?</param>
        [OgreVersion(1, 7)]
        public abstract void ApplyObliqueDepthProjection( ref Matrix4 projMatrix, Plane plane, bool forGpuProgram );

        /// <summary>
        ///        Signifies the beginning of a frame, ie the start of rendering on a single viewport. Will occur
        ///        several times per complete frame if multiple viewports exist.
        /// </summary>
        [OgreVersion(1, 7)]
        public abstract void BeginFrame();

        /// <summary>
        /// Pause rendering for a frame. This has to be called after 
        /// <see cref="BeginFrame"/> and before <see cref="EndFrame"/>.
        /// will usually be called by the SceneManager, don't use this manually unless you know what
        /// you are doing.
        /// </summary>
        [OgreVersion(1, 7)]
        public virtual RenderSystemContext PauseFrame()
        {
            EndFrame();
            return new RenderSystemContext();
        }


        /// <summary>
        /// Resume rendering for a frame. This has to be called after a <see cref="PauseFrame"/> call
        /// Will usually be called by the SceneManager, don't use this manually unless you know what
        /// you are doing.
        /// </summary>
        /// <param name="context">the render system context, as returned by <see cref="PauseFrame"/></param>
        [OgreVersion(1, 7)]
        public virtual void ResumeFrame(RenderSystemContext context)
        {
            BeginFrame();
            context.Dispose();
        }

        /// <summary>
        ///    Binds a given GpuProgram (but not the parameters). 
        /// </summary>
        /// <remarks>
        ///    Only one GpuProgram of each type can be bound at once, binding another
        ///    one will simply replace the existing one.
        /// </remarks>
        /// <param name="program"></param>
        [OgreVersion(1, 7)]
        public virtual void BindGpuProgram( GpuProgram program )
        {
            switch ( program.Type )
            {
                case GpuProgramType.Vertex:
                    // mark clip planes dirty if changed (programmable can change space)
                    if (!vertexProgramBound && !clipPlanes.empty())
                        clipPlanesDirty = true;

                    vertexProgramBound = true;
                    break;
                case GpuProgramType.Geometry:
                    geometryProgramBound = true;
                    break;
                case GpuProgramType.Fragment:
                    fragmentProgramBound = true;
                    break;
            }
        }

        /// <summary>
        /// Bind Gpu program parameters.
        /// </summary>
        [OgreVersion(1, 7)]
        public abstract void BindGpuProgramParameters( GpuProgramType type, GpuProgramParameters parms,
            GpuProgramParameters.GpuParamVariability mask);



        /// <summary>
        /// Only binds Gpu program parameters used for passes that have more than one iteration rendering
        /// </summary>
        /// <param name="gptype"></param>
        [OgreVersion(1, 7)]
        public abstract void BindGpuProgramPassIterationParameters(GpuProgramType gptype);

        #region ClearFrameBuffer

        /// <summary>
        ///        Clears one or more frame buffers on the active render target.
        /// </summary>
        ///<param name="buffers">
        ///  Combination of one or more elements of <see cref="Graphics.RenderTarget.FrameBuffer"/>
        ///  denoting which buffers are to be cleared.
        ///</param>
        ///<param name="color">The color to clear the color buffer with, if enabled.</param>
        ///<param name="depth">The value to initialize the depth buffer with, if enabled.</param>
        ///<param name="stencil">The value to initialize the stencil buffer with, if enabled.</param>
        [OgreVersion(1, 7)]
        public abstract void ClearFrameBuffer( FrameBufferType buffers, ColorEx color, Real depth, ushort stencil );

        public void ClearFrameBuffer(FrameBufferType buffers, ColorEx color, Real depth)
        {
            ClearFrameBuffer(buffers, color, depth, 0);
        }

        public void ClearFrameBuffer(FrameBufferType buffers, ColorEx color)
        {
            ClearFrameBuffer(buffers, color, Real.One, 0);
        }

        public void ClearFrameBuffer(FrameBufferType buffers)
        {
            ClearFrameBuffer(buffers, ColorEx.Black, Real.One, 0);
        }

        #endregion

        /// <summary>
        ///        Converts the Axiom.Core.ColorEx value to a int.  Each API may need the 
        ///        bytes of the packed color data in different orders. i.e. OpenGL - ABGR, D3D - ARGB
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        [OgreVersion(1, 7, "Axiom uses slightly different interface")]
        public abstract int ConvertColor( ColorEx color );

        /// <summary>
        ///        Converts the int value to an Axiom.Core.ColorEx object.  Each API may have the 
        ///        bytes of the packed color data in different orders. i.e. OpenGL - ABGR, D3D - ARGB
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        [OgreVersion(1, 7, "Axiom only")]
        public abstract ColorEx ConvertColor( int color );

        /// <summary>
        ///        Creates a new render window.
        /// </summary>
        /// <remarks>
        ///        This method creates a new rendering window as specified
        ///        by the paramteters. The rendering system could be
        ///        responible for only a single window (e.g. in the case
        ///        of a game), or could be in charge of multiple ones (in the
        ///        case of a level editor). The option to create the window
        ///        as a child of another is therefore given.
        ///        This method will create an appropriate subclass of
        ///        RenderWindow depending on the API and platform implementation.
        /// </remarks>
        /// <param name="name"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="isFullScreen"></param>
        /// <param name="miscParams">
        ///        A collection of addition rendersystem specific options.
        ///    </param>
        /// <returns></returns>
        [OgreVersion(1, 7)]
        public abstract RenderWindow CreateRenderWindow( string name, int width, int height, bool isFullScreen, NamedParameterList miscParams );


        /// <summary>
        /// Creates multiple rendering windows.
        /// </summary>
        /// <param name="renderWindowDescriptions">
        /// Array of structures containing the descriptions of each render window.
        /// The structure's members are the same as the parameters of CreateRenderWindow:
        /// <see cref="CreateRenderWindow"/>
        /// </param>
        /// <param name="createdWindows">This array will hold the created render windows.</param>
        /// <returns>true on success.</returns>
        [OgreVersion(1, 7)]
        public virtual bool CreateRenderWindows(RenderWindowDescriptionList renderWindowDescriptions, 
                        RenderWindowList createdWindows)
        {
            var fullscreenWindowsCount = 0;

            for (var nWindow = 0; nWindow < renderWindowDescriptions.Count; ++nWindow)
            {
                var curDesc = renderWindowDescriptions[ nWindow ];
                if ( curDesc.UseFullScreen )
                    fullscreenWindowsCount++;

                var renderWindowFound = false;

                if ( renderTargets.ContainsKey( curDesc.Name ) )
                    renderWindowFound = true;
                else
                {
                    for ( var nSecWindow = nWindow + 1; nSecWindow < renderWindowDescriptions.Count; ++nSecWindow )
                    {
                        if ( curDesc.Name == renderWindowDescriptions[ nSecWindow ].Name )
                        {
                            renderWindowFound = true;
                            break;
                        }
                    }
                }

                // Make sure we don't already have a render target of the 
                // same name as the one supplied
                if ( renderWindowFound )
                {
                    throw new AxiomException(
                        "A render target of the same name '{0}' already exists.  You cannot create a new window with this name.",
                        curDesc.Name );
                }
            }

             // Case we have to create some full screen rendering windows.
            if (fullscreenWindowsCount > 0)
            {
                // Can not mix full screen and windowed rendering windows.
                if ( fullscreenWindowsCount != renderWindowDescriptions.Count )
                {
                    throw new AxiomException( "Can not create mix of full screen and windowed rendering windows" );
                }
            }

            return true;
        }

        /// <summary>
        /// Create a MultiRenderTarget, which is a render target that renders to multiple RenderTextures at once.
        /// </summary>
        /// <Remarks>
        /// Surfaces can be bound and unbound at will. This fails if Capabilities.MultiRenderTargetsCount is smaller than 2.
        /// </Remarks>
        /// <returns></returns>
        [OgreVersion(1, 7)]
        public abstract MultiRenderTarget CreateMultiRenderTarget( string name );

        /// <summary>
        ///        Requests an API implementation of a hardware occlusion query used to test for the number
        ///        of fragments rendered between calls to <see cref="HardwareOcclusionQuery.Begin"/> and 
        ///        <see cref="HardwareOcclusionQuery.End"/> that pass the depth buffer test.
        /// </summary>
        /// <returns>An API specific implementation of an occlusion query.</returns>
        public abstract HardwareOcclusionQuery CreateHardwareOcclusionQuery();

        /// <summary>
        ///        Ends rendering of a frame to the current viewport.
        /// </summary>
        [OgreVersion(1, 7)]
        public abstract void EndFrame();

        /// <summary>
        /// Initialize the rendering engine.
        /// </summary>
        /// <param name="autoCreateWindow">If true, a default window is created to serve as a rendering target.</param>
        /// <param name="windowTitle">Text to display on the window caption if not fullscreen.</param>
        /// <returns>A RenderWindow implementation specific to this RenderSystem.</returns>
        /// <remarks>All subclasses should call this method from within thier own intialize methods.</remarks>
        [OgreVersion(1, 7)]
        public virtual RenderWindow Initialise(bool autoCreateWindow, string windowTitle = DefaultWindowTitle)
        {
            vertexProgramBound = false;
            geometryProgramBound = false;
            fragmentProgramBound = false;
            return null;
        }

        [OgreVersion(1, 7)]
        public abstract void Reinitialise();

        /// <summary>
        /// Query the real capabilities of the GPU and driver in the RenderSystem
        /// </summary>
        [OgreVersion(1, 7)]
        public abstract RenderSystemCapabilities CreateRenderSystemCapabilities();

        /// <summary>
        ///        Builds an orthographic projection matrix suitable for this render system.
        /// </summary>
        /// <remarks>
        ///        Because different APIs have different requirements (some incompatible) for the
        ///        projection matrix, this method allows each to implement their own correctly and pass
        ///        back a generic Matrix4 for storage in the engine.
        ///     </remarks>
        ///<param name="fov">Field of view angle.</param>
        ///<param name="aspectRatio">Aspect ratio.</param>
        ///<param name="near">Near clipping plane distance.</param>
        ///<param name="far">Far clipping plane distance.</param>
        ///<param name="dest"></param>
        ///<param name="forGpuPrograms"></param>
        /// <returns></returns>
        [OgreVersion(1, 7)]
        public abstract void MakeOrthoMatrix( Radian fov, Real aspectRatio, Real near, Real far, out Matrix4 dest, bool forGpuPrograms = false );

        /// <summary>
        ///     Converts a uniform projection matrix to one suitable for this render system.
        /// </summary>
        /// <remarks>
        ///        Because different APIs have different requirements (some incompatible) for the
        ///        projection matrix, this method allows each to implement their own correctly and pass
        ///        back a generic Matrix4 for storage in the engine.
        ///     </remarks>
        ///<param name="matrix"></param>
        ///<param name="dest"></param>
        ///<param name="forGpuProgram"></param>
        ///<returns></returns>
        [OgreVersion(1, 7)]
        public abstract void ConvertProjectionMatrix( Matrix4 matrix, out Matrix4 dest, bool forGpuProgram = false );

        /// <summary>
        ///        Builds a perspective projection matrix suitable for this render system.
        /// </summary>
        /// <remarks>
        ///        Because different APIs have different requirements (some incompatible) for the
        ///        projection matrix, this method allows each to implement their own correctly and pass
        ///        back a generic Matrix4 for storage in the engine.
        ///     </remarks>
        ///<param name="fov">Field of view angle.</param>
        ///<param name="aspectRatio">Aspect ratio.</param>
        ///<param name="near">Near clipping plane distance.</param>
        ///<param name="far">Far clipping plane distance.</param>
        ///<param name="dest"></param>
        ///<param name="forGpuProgram"></param>
        ///<returns></returns>
        [OgreVersion(1, 7)]
        public abstract void MakeProjectionMatrix( Radian fov, Real aspectRatio, Real near, Real far, out Matrix4 dest, bool forGpuProgram = false );

        /// <summary>
        ///  Sets the global alpha rejection approach for future renders.
        /// </summary>
        /// <param name="func">The comparison function which must pass for a pixel to be written.</param>
        /// <param name="value">The value to compare each pixels alpha value to (0-255)</param>
        /// <param name="alphaToCoverage">Whether to enable alpha to coverage, if supported</param>
        [OgreVersion(1, 7)]
        public abstract void SetAlphaRejectSettings( CompareFunction func, byte value, bool alphaToCoverage );

        [OgreVersion(1, 7)]
        public virtual void SetTextureProjectionRelativeTo(bool enabled, Vector3 pos)
        {
            _texProjRelative = true;
            _texProjRelativeOrigin = pos;
        }

        /// <summary>
        /// Creates a DepthBuffer that can be attached to the specified RenderTarget
        /// </summary>
        /// <remarks>
        /// It doesn't attach anything, it just returns a pointer to a new DepthBuffer
        /// Caller is responsible for putting this buffer into the right pool, for
        /// attaching, and deleting it. Here's where API-specific magic happens.
        /// Don't call this directly unless you know what you're doing.
        /// </remarks>
        [OgreVersion(1, 7)]
        public abstract DepthBuffer CreateDepthBufferFor( RenderTarget renderTarget );


        /// <summary>
        /// Removes all depth buffers. Should be called on device lost and shutdown
        /// </summary>
        /// <remarks>
        /// Advanced users can call this directly with bCleanManualBuffers=false to
        /// remove all depth buffers created for RTTs; when they think the pool has
        /// grown too big or they've used lots of depth buffers they don't need anymore,
        /// freeing GPU RAM.
        /// </remarks>
        /// <param name="bCleanManualBuffers"></param>
        [OgreVersion(1, 7, "not implemented yet")]
        public void CleanupDepthBuffers(bool bCleanManualBuffers = true)
        {
            throw new NotImplementedException();
        }

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
        [OgreVersion(1, 7)]
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
        public abstract void SetColourBufferWriteEnabled( bool red, bool green, bool blue, bool alpha );

        /// <summary>
        ///        Sets the mode of operation for depth buffer tests from this point onwards.
        /// </summary>
        /// <remarks>
        ///        Sometimes you may wish to alter the behavior of the depth buffer to achieve
        ///        special effects. Because it's unlikely that you'll set these options for an entire frame,
        ///        but rather use them to tweak settings between rendering objects, this is intended for internal
        ///        uses, which will be used by a <see cref="SceneManager"/> implementation rather than directly from 
        ///        the client application.
        /// </remarks>
        /// <param name="depthTest">
        ///        If true, the depth buffer is tested for each pixel and the frame buffer is only updated
        ///        if the depth function test succeeds. If false, no test is performed and pixels are always written.
        /// </param>
        /// <param name="depthWrite">
        ///        If true, the depth buffer is updated with the depth of the new pixel if the depth test succeeds.
        ///        If false, the depth buffer is left unchanged even if a new pixel is written.
        /// </param>
        /// <param name="depthFunction">Sets the function required for the depth test.</param>
        [OgreVersion(1, 7)]
        public abstract void SetDepthBufferParams( bool depthTest = true, bool depthWrite = true, CompareFunction depthFunction = CompareFunction.LessEqual );


        /// <summary> Axiom util override as ColorEx cant be defaulted</summary>
        public void SetFog(FogMode mode = FogMode.None)
        {
            SetFog(mode, ColorEx.White, Real.One, Real.Zero, Real.One);
        }

        /// <summary> Axiom util override as Real cant be defaulted</summary>
        public void SetFog(FogMode mode, ColorEx color)
        {
            SetFog(mode, color, Real.One, Real.Zero, Real.One);
        }

        /// <summary> Axiom util override as Real cant be defaulted</summary>
        public void SetFog(FogMode mode, ColorEx color, Real density)
        {
            SetFog(mode, color, density, Real.Zero, Real.One);
        }

        /// <summary> Axiom util override as Real cant be defaulted</summary>
        public void SetFog(FogMode mode, ColorEx color, Real density, Real linearStart)
        {
            SetFog(mode, color, density, linearStart, Real.One);
        }

        /// <summary>
        ///        Sets the fog with the given params.
        /// </summary>
        [OgreVersion(1, 7)]
        public abstract void SetFog( FogMode mode, ColorEx color, Real density,
            Real linearStart, Real linearEnd);

        /// <summary>
        ///        Sets the global blending factors for combining subsequent renders with the existing frame contents.
        ///        The result of the blending operation is:
        ///        <p align="center">final = (texture * src) + (pixel * dest)</p>
        ///        Each of the factors is specified as one of a number of options, as specified in the SceneBlendFactor
        ///        enumerated type.
        /// </summary>
        /// <param name="src">The source factor in the above calculation, i.e. multiplied by the texture color components.</param>
        /// <param name="dest">The destination factor in the above calculation, i.e. multiplied by the pixel color components.</param>
        /// <param name="op">The blend operation mode for combining pixels</param>
        [OgreVersion(1, 7)]
        public abstract void SetSceneBlending(SceneBlendFactor src, SceneBlendFactor dest, SceneBlendOperation op = SceneBlendOperation.Add);

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
        /// <param name="op">The blend operation mode for combining pixels</param>
        /// <param name="alphaOp">The blend operation mode for combining pixel alpha values</param>
        [OgreVersion(1, 7)]
        public abstract void SetSeparateSceneBlending( SceneBlendFactor sourceFactor, SceneBlendFactor destFactor, SceneBlendFactor sourceFactorAlpha,
            SceneBlendFactor destFactorAlpha, SceneBlendOperation op = SceneBlendOperation.Add, SceneBlendOperation alphaOp = SceneBlendOperation.Add);

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
        /// <param name="enable">True to enable the scissor test, false to disable it.</param>
        /// <param name="left">Left corner (in pixels).</param>
        /// <param name="top">Top corner (in pixels).</param>
        /// <param name="right">Right corner (in pixels).</param>
        /// <param name="bottom">Bottom corner (in pixels).</param>
        public abstract void SetScissorTest( bool enable, int left = 0, int top = 0, int right = 800, int bottom = 600 );

        /// <summary>
        ///        This method allows you to set all the stencil buffer parameters in one call.
        /// </summary>
        /// <remarks>
        ///        <para>
        ///        The stencil buffer is used to mask out pixels in the render target, allowing
        ///        you to do effects like mirrors, cut-outs, stencil shadows and more. Each of
        ///        your batches of rendering is likely to ignore the stencil buffer, 
        ///        update it with new values, or apply it to mask the output of the render.
        ///        The stencil test is:<PRE>
        ///        (Reference Value & Mask) CompareFunction (Stencil Buffer Value & Mask)</PRE>
        ///        The result of this will cause one of 3 actions depending on whether the test fails,
        ///        succeeds but with the depth buffer check still failing, or succeeds with the
        ///        depth buffer check passing too.</para>
        ///        <para>
        ///        Unlike other render states, stencilling is left for the application to turn
        ///        on and off when it requires. This is because you are likely to want to change
        ///        parameters between batches of arbitrary objects and control the ordering yourself.
        ///        In order to batch things this way, you'll want to use OGRE's separate render queue
        ///        groups (see RenderQueue) and register a RenderQueueListener to get notifications
        ///        between batches.</para>
        ///        <para>
        ///        There are individual state change methods for each of the parameters set using 
        ///        this method. 
        ///        Note that the default values in this method represent the defaults at system 
        ///        start up too.</para>
        /// </remarks>
        /// <param name="function">The comparison function applied.</param>
        /// <param name="refValue">The reference value used in the comparison.</param>
        /// <param name="mask">
        ///        The bitmask applied to both the stencil value and the reference value 
        ///        before comparison.
        /// </param>
        /// <param name="stencilFailOp">The action to perform when the stencil check fails.</param>
        /// <param name="depthFailOp">
        ///        The action to perform when the stencil check passes, but the depth buffer check still fails.
        /// </param>
        /// <param name="passOp">The action to take when both the stencil and depth check pass.</param>
        /// <param name="twoSidedOperation">
        ///        If set to true, then if you render both back and front faces 
        ///        (you'll have to turn off culling) then these parameters will apply for front faces, 
        ///        and the inverse of them will happen for back faces (keep remains the same).
        /// </param>
        [OgreVersion(1, 7)]
        public abstract void SetStencilBufferParams( CompareFunction function = CompareFunction.AlwaysPass, 
            int refValue = 0, int mask = -1,
            StencilOperation stencilFailOp = StencilOperation.Keep,
            StencilOperation depthFailOp = StencilOperation.Keep, 
            StencilOperation passOp = StencilOperation.Keep, 
            bool twoSidedOperation = false);

        /// <summary>
        ///    Sets the surface properties to be used for future rendering.
        /// 
        /// This method sets the the properties of the surfaces of objects
        /// to be rendered after it. In this context these surface properties
        /// are the amount of each type of light the object reflects (determining
        /// it's colour under different types of light), whether it emits light
        /// itself, and how shiny it is. Textures are not dealt with here,
        /// <see cref="SetTexture"/> method for details.
        /// This method is used by <see cref="SetMaterial"/> so does not need to be called
        /// direct if that method is being used.
        /// </summary>
        /// <param name="ambient">
        /// The amount of ambient (sourceless and directionless)
        /// light an object reflects. Affected by the colour/amount of ambient light in the scene.
        /// </param>
        /// <param name="diffuse">
        /// The amount of light from directed sources that is
        /// reflected (affected by colour/amount of point, directed and spot light sources)
        /// </param>
        /// <param name="specular">
        /// The amount of specular light reflected. This is also
        /// affected by directed light sources but represents the colour at the
        /// highlights of the object.
        /// </param>
        /// <param name="emissive">
        /// The colour of light emitted from the object. Note that
        /// this will make an object seem brighter and not dependent on lights in
        /// the scene, but it will not act as a light, so will not illuminate other
        /// objects. Use a light attached to the same SceneNode as the object for this purpose.
        /// </param>
        /// <param name="shininess">
        /// A value which only has an effect on specular highlights (so
        /// specular must be non-black). The higher this value, the smaller and crisper the
        /// specular highlights will be, imitating a more highly polished surface.
        /// This value is not constrained to 0.0-1.0, in fact it is likely to
        /// be more (10.0 gives a modest sheen to an object).
        /// </param>
        /// <param name="tracking">
        /// A bit field that describes which of the ambient, diffuse, specular
        /// and emissive colours follow the vertex colour of the primitive. When a bit in this field is set
        /// its ColourValue is ignored. This is a combination of TVC_AMBIENT, TVC_DIFFUSE, TVC_SPECULAR(note that the shininess value is still
        /// taken from shininess) and TVC_EMISSIVE. TVC_NONE means that there will be no material property
        /// tracking the vertex colours.
        /// </param>
        [OgreVersion(1, 7)]
        public abstract void SetSurfaceParams( ColorEx ambient, ColorEx diffuse, ColorEx specular, 
            ColorEx emissive, Real shininess, TrackVertexColor tracking = TrackVertexColor.None );

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
        [OgreVersion(1, 7)]
        public abstract void SetPointParameters( Real size, bool attenuationEnabled, 
            Real constant, Real linear, Real quadratic, Real minSize, Real maxSize );

        /// <summary>
        ///        Sets the details of a texture stage, to be used for all primitives
        ///        rendered afterwards. User processes would
        ///        not normally call this direct unless rendering
        ///        primitives themselves - the SubEntity class
        ///        is designed to manage materials for objects.
        ///        Note that this method is called by SetMaterial.
        /// </summary>
        /// <param name="unit">The index of the texture unit to modify. Multitexturing hardware 
        /// can support multiple units (see TextureUnitCount)</param>
        /// <param name="enabled">Boolean to turn the unit on/off</param>
        /// <param name="textureName">The name of the texture to use - this should have
        ///        already been loaded with TextureManager.Load.</param>
        [OgreVersion(1, 7)]
        public void SetTexture( int unit, bool enabled, string textureName )
        {
            // load the texture
            var texture = (Texture)TextureManager.Instance.GetByName( textureName );
            SetTexture(unit, enabled, texture);
        }

        /// <summary>
        /// Sets the texture to bind to a given texture unit.
        /// 
        /// User processes would not normally call this direct unless rendering
        /// primitives themselves.
        /// </summary>
        /// <param name="unit">
        /// The index of the texture unit to modify. Multitexturing
        /// hardware can support multiple units <see cref="RenderSystemCapabilities.TextureUnitCount"/> 
        /// </param>
        /// <param name="enabled"></param>
        /// <param name="texture"></param>
        [OgreVersion(1, 7)]
        public abstract void SetTexture( int unit, bool enabled, Texture texture );

        /// <summary>
        /// Binds a texture to a vertex sampler.
        /// </summary>
        /// <remarks>
        /// Not all rendersystems support separate vertex samplers. For those that
        /// do, you can set a texture for them, separate to the regular texture
        /// samplers, using this method. For those that don't, you should use the
        /// regular texture samplers which are shared between the vertex and
        /// fragment units; calling this method will throw an exception.
        /// <see cref="RenderSystemCapabilities.VertexTextureUnitsShared"/>
        /// </remarks>
        /// <param name="unit"></param>
        /// <param name="texture"></param>
        [OgreVersion(1, 7)]
        public virtual void SetVertexTexture( int unit, Texture texture )
        {
            throw new NotSupportedException(
                "This rendersystem does not support separate vertex texture samplers, " +
                "you should use the regular texture samplers which are shared between " +
                "the vertex and fragment units." );
        }

        /// <summary>
        ///        Tells the hardware how to treat texture coordinates.
        /// </summary>
        [OgreVersion(1, 7)]
        public abstract void SetTextureAddressingMode( int unit, UVWAddressing uvw );

        /// <summary>
        /// Sets the mipmap bias value for a given texture unit.
        /// </summary>
        /// <remarks>
        /// This allows you to adjust the mipmap calculation up or down for a
        /// given texture unit. Negative values force a larger mipmap to be used, 
        /// positive values force a smaller mipmap to be used. Units are in numbers
        /// of levels, so +1 forces the mipmaps to one smaller level.
        /// </remarks>
        /// <note>Only does something if render system has capability RSC_MIPMAP_LOD_BIAS.</note>
        [OgreVersion(1, 7)]
        public abstract void SetTextureMipmapBias( int unit, float bias );

        /// <summary>
        ///    Tells the hardware what border color to use when texture addressing mode is set to Border
        /// </summary>
        /// <param name="unit"></param>
        /// <param name="borderColor"></param>
        public abstract void SetTextureBorderColour(int unit, ColorEx borderColor);

        /// <summary>
        ///        Sets the texture blend modes from a TextureLayer record.
        ///        Meant for use internally only - apps should use the Material
        ///        and TextureLayer classes.
        /// </summary>
        /// <param name="unit">Texture unit.</param>
        /// <param name="blendMode">Details of the blending modes.</param>
        [OgreVersion(1, 7)]
        public abstract void SetTextureBlendMode( int unit, LayerBlendModeEx blendMode );

        /// <summary>
        ///        Sets a method for automatically calculating texture coordinates for a stage.
        /// </summary>
        /// <param name="unit">Texture stage to modify.</param>
        /// <param name="method">Calculation method to use</param>
        /// <param name="frustum">Frustum, only used for projective effects</param>
        [OgreVersion(1, 7)]
        public abstract void SetTextureCoordCalculation( int unit, TexCoordCalcMethod method, Frustum frustum = null );

        /// <summary>
        ///        Sets the index into the set of tex coords that will be currently used by the render system.
        /// </summary>
        [OgreVersion(1, 7)]
        public abstract void SetTextureCoordSet( int stage, int index );

        /// <summary>
        ///        Sets the maximal anisotropy for the specified texture unit.
        /// </summary>
        [OgreVersion(1, 7)]
        public abstract void SetTextureLayerAnisotropy( int unit, int maxAnisotropy );

        /// <summary>
        ///        Sets the texture matrix for the specified stage.  Used to apply rotations, translations,
        ///        and scaling to textures.
        /// </summary>
        [OgreVersion(1, 7)]
        public abstract void SetTextureMatrix( int stage, Matrix4 xform );

        /// <summary>
        ///    Sets a single filter for a given texture unit.
        /// </summary>
        /// <param name="unit">The texture unit to set the filtering options for.</param>
        /// <param name="type">The filter type.</param>
        /// <param name="filter">The filter to be used.</param>
        [OgreVersion(1, 7)]
        public abstract void SetTextureUnitFiltering( int unit, FilterType type, FilterOptions filter );

        /// <summary>
        ///    Sets the filtering options for a given texture unit.
        /// </summary>
        /// <param name="unit">The texture unit to set the filtering options for.</param>
        /// <param name="minFilter">The filter used when a texture is reduced in size.</param>
        /// <param name="magFilter">The filter used when a texture is magnified.</param>
        /// <param name="mipFilter">
        ///        The filter used between mipmap levels, <see cref="FilterOptions.None"/> disables mipmapping.
        /// </param>
        [OgreVersion(1, 7)]
        public virtual void SetTextureUnitFiltering(int unit, FilterOptions minFilter, FilterOptions magFilter, FilterOptions mipFilter)
        {
            SetTextureUnitFiltering(unit, FilterType.Min, minFilter);
            SetTextureUnitFiltering(unit, FilterType.Mag, magFilter);
            SetTextureUnitFiltering(unit, FilterType.Mip, mipFilter);
        }

        /// <summary>
        ///    Unbinds the current GpuProgram of a given GpuProgramType.
        /// </summary>
        [OgreVersion(1, 7)]
        public virtual void UnbindGpuProgram( GpuProgramType type )
        {
            switch (type)
            {
                case GpuProgramType.Vertex:
                    // mark clip planes dirty if changed (programmable can change space)
                    if (vertexProgramBound && !clipPlanes.empty())
                        clipPlanesDirty = true;

                    vertexProgramBound = false;
                    break;
                case GpuProgramType.Geometry:
                    geometryProgramBound = false;
                    break;
                case GpuProgramType.Fragment:
                    fragmentProgramBound = false;
                    break;
            }
        }

        /// <summary>
        ///    Gets the bound status of a given GpuProgramType.
        /// </summary>
        [OgreVersion(1, 7)]
        public bool IsGpuProgramBound( GpuProgramType type )
        {
            switch ( type )
            {
                case GpuProgramType.Vertex:
                    return vertexProgramBound;
                case GpuProgramType.Geometry:
                    return geometryProgramBound;
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
        [OgreVersion(1, 7)]
        public abstract void UseLights( LightList lightList, int limit );

        #endregion Methods

        #endregion Abstract Members

        /// <summary>
        ///   Destroys a render target of any sort
        /// </summary>
        /// <param name="name"></param>
        [OgreVersion(1, 7)]
        public virtual void DestroyRenderTarget( string name )
        {
            var rt = DetachRenderTarget( name );
            rt.Dispose();
        }

        /// <summary>
        ///   Destroys a render window
        /// </summary>
        /// <param name="name"></param>
        [OgreVersion(1, 7)]
        public virtual void DestroyRenderWindow( string name )
        {
            DestroyRenderTarget( name );
        }

        /// <summary>
        ///   Destroys a render texture
        /// </summary>
        /// <param name="name"></param>
        [OgreVersion(1, 7)]
        public virtual void DestroyRenderTexture( string name )
        {
            DestroyRenderTarget( name );
        }

        #region Overloaded Methods

        /// <summary>
        ///        Converts a uniform projection matrix to one suitable for this render system.
        /// </summary>
        /// <remarks>
        ///        Because different APIs have different requirements (some incompatible) for the
        ///        projection matrix, this method allows each to implement their own correctly and pass
        ///        back a generic Matrix4 for storage in the engine.
        ///     </remarks>
        /// <param name="matrix"></param>
        /// <returns></returns>
        public Matrix4 ConvertProjectionMatrix( Matrix4 matrix )
        {
            // create without consideration for Gpu programs by default
            return ConvertProjectionMatrix( matrix, out null, false );
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
        /// <param name="dest"></param>
        /// <param name="forGpuProgram"></param>
        public abstract void MakeProjectionMatrix( Real left, Real right, Real bottom, Real top, Real nearPlane, Real farPlane, out Matrix4 dest, bool forGpuProgram = false );

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