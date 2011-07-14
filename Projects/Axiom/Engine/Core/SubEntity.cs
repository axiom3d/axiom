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

#endregion LGPL License

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

using Axiom.Math;
using Axiom.Graphics;
using Axiom.Animating;
using Axiom.Core.Collections;

#endregion Namespace Declarations

namespace Axiom.Core
{
	/// <summary>
	///		Utility class which defines the sub-parts of an Entity.
	/// </summary>
	/// <remarks>
	///		<para>
	///		Just as models are split into meshes, an Entity is made up of
	///		potentially multiple SubEntities. These are mainly here to provide the
	///		link between the Material which the SubEntity uses (which may be the
	///		default Material for the SubMesh or may have been changed for this
	///		object) and the SubMesh data.
	///		</para>
	///		<para>
	///		SubEntity instances are never created manually. They are created at
	///		the same time as their parent Entity by the SceneManager method
	///		CreateEntity.
	///		</para>
	/// </remarks>
	public class SubEntity : DisposableObject, IRenderable
	{
		#region Fields

		/// <summary>
		///    Reference to the parent Entity.
		/// </summary>
		protected Entity parent;
		/// <summary>
		///    Name of the material being used.
		/// </summary>
		protected string materialName;
		/// <summary>
		///    Reference to the material being used by this SubEntity.
		/// </summary>
		protected Material material;
		/// <summary>
		///    Reference to the subMesh that represents the geometry for this SubEntity.
		/// </summary>
		protected SubMesh subMesh;

		private Camera cachedCamera;
		private Real cachedCameraDist;

		/// <summary>
		///		Flag indicating whether this sub entity should be rendered or not.
		/// </summary>
		protected bool isVisible;
		/// <summary>
		///		Blend buffer details for dedicated geometry.
		/// </summary>
		protected internal VertexData skelAnimVertexData;
		/// <summary>
		///		Temp buffer details for software skeletal anim geometry
		/// </summary>
		protected internal TempBlendedBufferInfo tempSkelAnimInfo = new TempBlendedBufferInfo();
		/// <summary>
		///		Temp buffer details for software Vertex anim geometry
		/// </summary>
		protected TempBlendedBufferInfo tempVertexAnimInfo = new TempBlendedBufferInfo();
		/// <summary>
		///		Vertex data details for software Vertex anim of shared geometry
		/// </summary>
		/// Temp buffer details for software Vertex anim geometry
		protected VertexData softwareVertexAnimVertexData;
		/// <summary>
		///     Vertex data details for hardware Vertex anim of shared geometry
		///     - separate since we need to s/w anim for shadows whilst still altering
		///       the vertex data for hardware morphing (pos2 binding)
		/// </summary>
		protected VertexData hardwareVertexAnimVertexData;
		/// <summary>
		///		Have we applied any vertex animation to geometry?
		/// </summary>
		protected bool vertexAnimationAppliedThisFrame;
		/// <summary>
		///		Number of hardware blended poses supported by material
		/// </summary>
		protected ushort hardwarePoseCount;
		/// <summary>
		///		Flag indicating whether hardware skinning is supported by this subentity's materials.
		/// </summary>
		protected bool hardwareSkinningEnabled;
		/// <summary>
		///		Flag indicating whether vertex programs are used by this subentity's materials.
		/// </summary>
		protected bool useVertexProgram;

		protected List<Vector4> customParams = new List<Vector4>();

		#endregion Fields

		#region Constructor

		/// <summary>
		///		Internal constructor, only allows creation of SubEntities within the engine core.
		/// </summary>
		internal SubEntity()
            : base()
		{
			material = (Material)MaterialManager.Instance[ "BaseWhite" ];

			isVisible = true;
		}

		#endregion Constructor

		#region Properties

		/// <summary>
		///		Gets a flag indicating whether or not this sub entity should be rendered or not.
		/// </summary>
		public bool IsVisible
		{
			get
			{
				return isVisible;
			}
			set
			{
				isVisible = value;
			}
		}

		/// <summary>
		/// Name of this SubEntity.
		/// It is the name of the associated SubMesh.
		/// </summary>
		public string Name
		{
			get
			{
				return subMesh.Name;
			}
		}

		/// <summary>
		///		Gets/Sets the name of the material used for this SubEntity.
		/// </summary>
		public string MaterialName
		{
			get
			{
				return materialName;
			}
			set
			{
				if ( value == null )
					throw new AxiomException( "Cannot set the subentity material to null." );

				materialName = value;

				// load the material from the material manager (it should already exist)
				material = (Material)MaterialManager.Instance[ materialName ];

				if ( material == null )
				{
					LogManager.Instance.Write(
						"Cannot assign material '{0}' to Entity.SubEntity '{1}.{2}' because the material doesn't exist.", materialName, parent.Name, this.Name );

					// give it base white so we can continue
					material = (Material)MaterialManager.Instance[ "BaseWhite" ];
				}

				// ensure the material is loaded.  It will skip it if it already is
				material.Load();

				// since the material has changed, re-evaulate its support of skeletal animation
				parent.ReevaluateVertexProcessing();
			}
		}

		/// <summary>
		///		Gets/Sets the subMesh to be used for rendering this SubEntity.
		/// </summary>
		public SubMesh SubMesh
		{
			get
			{
				return subMesh;
			}
			set
			{
				subMesh = value;
			}
		}

		/// <summary>
		///		Gets/Sets the parent entity of this SubEntity.
		/// </summary>
		public Entity Parent
		{
			get
			{
				return parent;
			}
			set
			{
				parent = value;
			}
		}

		public VertexData SkelAnimVertexData
		{
			get
			{
				return skelAnimVertexData;
			}
		}

		public TempBlendedBufferInfo TempSkelAnimInfo
		{
			get
			{
				return tempSkelAnimInfo;
			}
		}

		public TempBlendedBufferInfo TempVertexAnimInfo
		{
			get
			{
				return tempVertexAnimInfo;
			}
		}

		public VertexData SoftwareVertexAnimVertexData
		{
			get
			{
				return softwareVertexAnimVertexData;
			}
		}

		public VertexData HardwareVertexAnimVertexData
		{
			get
			{
				return hardwareVertexAnimVertexData;
			}
		}

		public ushort HardwarePoseCount
		{
			get
			{
				return hardwarePoseCount;
			}
			set
			{
				hardwarePoseCount = value;
			}
		}

		/// <summary>
		///		Are buffers already marked as vertex animated?
		/// </summary>
		public bool BuffersMarkedForAnimation
		{
			get
			{
				return vertexAnimationAppliedThisFrame;
			}
		}

		#endregion Properties

		#region Methods

		/// <summary>
		///		Internal method for preparing this sub entity for use in animation.
		/// </summary>
		protected internal void PrepareTempBlendBuffers()
		{
			// Handle the case where we have no submesh vertex data (probably shared)
			if ( subMesh.useSharedVertices )
				return;
			if ( skelAnimVertexData != null )
				skelAnimVertexData = null;
			if ( softwareVertexAnimVertexData != null )
				softwareVertexAnimVertexData = null;
			if ( hardwareVertexAnimVertexData != null )
				hardwareVertexAnimVertexData = null;

			if ( !subMesh.useSharedVertices )
			{
				if ( subMesh.VertexAnimationType != VertexAnimationType.None )
				{
					// Create temporary vertex blend info
					// Prepare temp vertex data if needed
					// Clone without copying data, don't remove any blending info
					// (since if we skeletally animate too, we need it)
					softwareVertexAnimVertexData = subMesh.vertexData.Clone( false );
					parent.ExtractTempBufferInfo( softwareVertexAnimVertexData, tempVertexAnimInfo );

					// Also clone for hardware usage, don't remove blend info since we'll
					// need it if we also hardware skeletally animate
					hardwareVertexAnimVertexData = subMesh.vertexData.Clone( false );
				}

				if ( parent.HasSkeleton )
				{
					// Create temporary vertex blend info
					// Prepare temp vertex data if needed
					// Clone without copying data, remove blending info
					// (since blend is performed in software)
					skelAnimVertexData = parent.CloneVertexDataRemoveBlendInfo( subMesh.vertexData );
					parent.ExtractTempBufferInfo( skelAnimVertexData, tempSkelAnimInfo );
				}
			}
		}

		#endregion Methods

		#region SubMesh Level of Detail

		/// <summary>
		///	current LOD index to use.
		/// </summary>
		private int _materialLodIndex;
		public int MaterialLodIndex
		{
			get
			{
				return _materialLodIndex;
			}
			set
			{
				_materialLodIndex = value;
			}
		}

		#endregion SubMesh Level of Detail

		#region IRenderable Members

		public bool CastsShadows
		{
			get
			{
				return parent.CastShadows;
			}
		}

		/// <summary>
		///		Gets/Sets a reference to the material being used by this SubEntity.
		/// </summary>
		/// <remarks>
		///		By default, the SubEntity will use the material defined by the SubMesh.  However,
		///		this can be overridden by the SubEntity in the case where several entities use the
		///		same SubMesh instance, but want to shade it different.
		///     This should probably call parent.ReevaluateVertexProcessing.
		/// </remarks>
		public Material Material
		{
			get
			{
				return material;
			}
			set
			{
				material = value;
				// We may have switched to a material with a vertex shader
				// or something similar.
				parent.ReevaluateVertexProcessing();
			}
		}

		// TODO: In the ogre version, it gets the value of these from the parent Entity.
		public bool NormalizeNormals
		{
			get
			{
				return false;
			}
		}

		public Technique Technique
		{
			get
			{
				return material.GetBestTechnique( _materialLodIndex, this );
			}
		}

		protected RenderOperation renderOperation = new RenderOperation();
		/// <summary>
		///
		/// </summary>
		/// <param name="value"></param>
		public RenderOperation RenderOperation
		{
			get
			{
				// use LOD
				this.subMesh.GetRenderOperation( renderOperation, this.parent.MeshLodIndex );
				// Deal with any vertex data overrides
				renderOperation.vertexData = this.GetVertexDataForBinding();
				return renderOperation;
			}
		}

		public VertexData GetVertexDataForBinding()
		{
			if ( subMesh.useSharedVertices )
				return parent.GetVertexDataForBinding();
			else
			{
				VertexDataBindChoice c =
					parent.ChooseVertexDataForBinding(
						subMesh.VertexAnimationType != VertexAnimationType.None );
				switch ( c )
				{
					case VertexDataBindChoice.Original:
						return subMesh.vertexData;
					case VertexDataBindChoice.HardwareMorph:
						return hardwareVertexAnimVertexData;
					case VertexDataBindChoice.SoftwareMorph:
						return softwareVertexAnimVertexData;
					case VertexDataBindChoice.SoftwareSkeletal:
						return skelAnimVertexData;
				};
				// keep compiler happy
				return subMesh.vertexData;
			}
		}

		Material IRenderable.Material
		{
			get
			{
				return material;
			}
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="matrices"></param>
		public void GetWorldTransforms( Matrix4[] matrices )
		{
			if ( parent.numBoneMatrices == 0 || !parent.IsHardwareAnimationEnabled )
			{
				matrices[ 0 ] = parent.ParentNodeFullTransform;
			}
			else
			{
				// TODO : Look at OGRE version
				// ??? In the ogre version, it chooses the transforms based on
				// ??? an index map maintained by the mesh.  Is that appropriate here?
				if ( parent.IsSkeletonAnimated )
				{
					// use cached bone matrices of the parent entity
					for ( int i = 0; i < parent.numBoneMatrices; i++ )
					{
						matrices[ i ] = parent.boneMatrices[ i ];
					}
				}
				else
					for ( int i = 0; i < matrices.Length; i++ )
					{
						matrices[ i ] = parent.ParentNodeFullTransform;
					}
			}
		}

		/// <summary>
		///
		/// </summary>
		public ushort NumWorldTransforms
		{
			get
			{
				if ( parent.numBoneMatrices == 0 )
				{
					return 1;
				}
				else
				{
					return (ushort)parent.numBoneMatrices;
				}
			}
		}

		/// <summary>
		///
		/// </summary>
		public bool UseIdentityProjection
		{
			get
			{
				return false;
			}
		}

		/// <summary>
		///
		/// </summary>
		public bool UseIdentityView
		{
			get
			{
				return false;
			}
		}

		public virtual bool PolygonModeOverrideable
		{
			get
			{
				return true;
			}
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="camera"></param>
		/// <returns></returns>
		public float GetSquaredViewDepth( Camera camera )
		{
			// First of all, check the cached value
			// NB this is manually invalidated by parent each _notifyCurrentCamera call
			// Done this here rather than there since we only need this for transparent objects
			if ( cachedCamera == camera )
				return cachedCameraDist;

			Node node = Parent.ParentNode;
			Debug.Assert( node != null );
			Real dist;
#warning SubMesh.ExtremityPoints implementation needed.
			//if (!subMesh.extremityPoints.empty())
			//{
			//    const Vector3 &cp = cam->getDerivedPosition();
			//    const Matrix4 &l2w = mParentEntity->_getParentNodeFullTransform();
			//    dist = std::numeric_limits<Real>::infinity();
			//    for (vector<Vector3>::type::const_iterator i = mSubMesh->extremityPoints.begin();
			//         i != mSubMesh->extremityPoints.end (); ++i)
			//    {
			//        Vector3 v = l2w * (*i);
			//        Real d = (v - cp).squaredLength();

			//        dist = std::min(d, dist);
			//    }
			//}
			//else
			{
				dist = node.GetSquaredViewDepth( camera );
			}
			cachedCameraDist = dist;
			cachedCamera = camera;

			return dist;
		}

		/// <summary>
		///
		/// </summary>
		public Quaternion WorldOrientation
		{
			get
			{
				// get the parent entitie's parent node
				Node node = parent.ParentNode;

				Debug.Assert( node != null );

				return parent.ParentNode.DerivedOrientation;
			}
		}

		/// <summary>
		///
		/// </summary>
		public Vector3 WorldPosition
		{
			get
			{
				// get the parent entitie's parent node
				Node node = parent.ParentNode;

				Debug.Assert( node != null );

				return parent.ParentNode.DerivedPosition;
			}
		}

		/// <summary>
		///
		/// </summary>
		public LightList Lights
		{
			get
			{
				return parent.QueryLights();
			}
		}

		/// <summary>
		///		Returns whether or not hardware skinning is enabled.
		/// </summary>
		/// <remarks>
		///		Because fixed-function indexed vertex blending is rarely supported
		///		by existing graphics cards, hardware skinning can only be done if
		///		the vertex programs in the materials used to render an entity support
		///		it. Therefore, this method will only return true if all the materials
		///		assigned to this entity have vertex programs assigned, and all those
		///		vertex programs must support 'include_skeletal_animation true'.
		/// </remarks>
		public bool HardwareSkinningEnabled
		{
			get
			{
				return useVertexProgram && hardwareSkinningEnabled;
			}
			set
			{
				hardwareSkinningEnabled = value;
			}
		}

		public bool VertexProgramInUse
		{
			get
			{
				return useVertexProgram;
			}
			set
			{
				useVertexProgram = value;
			}
		}

		public Vector4 GetCustomParameter( int index )
		{
			if ( customParams[ index ] == null )
			{
				throw new Exception( "A parameter was not found at the given index" );
			}
			else
			{
				return (Vector4)customParams[ index ];
			}
		}

		public void SetCustomParameter( int index, Vector4 val )
		{
			while ( customParams.Count <= index )
				customParams.Add( Vector4.Zero );
			customParams[ index ] = val;
		}

		public void UpdateCustomGpuParameter( GpuProgramParameters.AutoConstantEntry entry, GpuProgramParameters gpuParams )
		{
			if ( entry.Type == GpuProgramParameters.AutoConstantType.AnimationParametric )
			{
				// Set up to 4 values, or up to limit of hardware animation entries
				// Pack into 4-element constants offset based on constant data index
				// If there are more than 4 entries, this will be called more than once
				Vector4 val = Vector4.Zero;

				int animIndex = entry.Data * 4;
				for ( int i = 0; i < 4 &&
					animIndex < hardwareVertexAnimVertexData.HWAnimationDataList.Count;
					++i, ++animIndex )
				{
					val[ i ] = hardwareVertexAnimVertexData.HWAnimationDataList[ animIndex ].Parametric;
				}
				// set the parametric morph value
				gpuParams.SetConstant( entry.PhysicalIndex, val );
			}
			else if ( customParams[ entry.Data ] != null )
			{
				gpuParams.SetConstant( entry.PhysicalIndex, (Vector4)customParams[ entry.Data ] );
			}
		}

		// TODO : Make these two methods a property
		public void MarkBuffersUnusedForAnimation()
		{
			vertexAnimationAppliedThisFrame = false;
		}

		public void MarkBuffersUsedForAnimation()
		{
			vertexAnimationAppliedThisFrame = true;
		}

		public void RestoreBuffersForUnusedAnimation( bool hardwareAnimation )
		{
			// Rebind original positions if:
			//  We didn't apply any animation and
			//    We're morph animated (hardware binds keyframe, software is missing)
			//    or we're pose animated and software (hardware is fine, still bound)
			if ( subMesh.VertexAnimationType != VertexAnimationType.None &&
				!subMesh.useSharedVertices &&
				!vertexAnimationAppliedThisFrame &&
				( !hardwareAnimation || subMesh.VertexAnimationType == VertexAnimationType.Morph ) )
			{
				VertexElement srcPosElem =
					subMesh.vertexData.vertexDeclaration.FindElementBySemantic( VertexElementSemantic.Position );
				HardwareVertexBuffer srcBuf =
					subMesh.vertexData.vertexBufferBinding.GetBuffer( srcPosElem.Source );

				// Bind to software
				VertexElement destPosElem =
					softwareVertexAnimVertexData.vertexDeclaration.FindElementBySemantic( VertexElementSemantic.Position );
				softwareVertexAnimVertexData.vertexBufferBinding.SetBinding( destPosElem.Source, srcBuf );
			}
		}

		#endregion IRenderable Members

		#region IDisposable Implementation

		/// <summary>
		/// Class level dispose method
		/// </summary>
		/// <remarks>
		/// When implementing this method in an inherited class the following template should be used;
		/// protected override void dispose( bool disposeManagedResources )
		/// {
		/// 	if ( !isDisposed )
		/// 	{
		/// 		if ( disposeManagedResources )
		/// 		{
		/// 			// Dispose managed resources.
		/// 		}
		///
		/// 		// There are no unmanaged resources to release, but
		/// 		// if we add them, they need to be released here.
		/// 	}
		///
		/// 	// If it is available, make the call to the
		/// 	// base class's Dispose(Boolean) method
		/// 	base.dispose( disposeManagedResources );
		/// }
		/// </remarks>
		/// <param name="disposeManagedResources">True if Unmanaged resources should be released.</param>
		protected override void dispose( bool disposeManagedResources )
		{
			if ( !this.IsDisposed )
			{
				if ( disposeManagedResources )
				{
					// Dispose managed resources.
					if ( this.renderOperation != null )
					{
						//if (!this.renderOperation.IsDisposed)
						//    this.renderOperation.Dispose();

						this.renderOperation = null;
					}

                    if (this.skelAnimVertexData != null)
                    {
                        if (!this.skelAnimVertexData.IsDisposed)
                            this.skelAnimVertexData.Dispose();

                        this.skelAnimVertexData = null;
                    }
   				}

				// There are no unmanaged resources to release, but
				// if we add them, they need to be released here.
			}

            base.dispose(disposeManagedResources);
		}

		#endregion IDisposable Implementation
	}
}