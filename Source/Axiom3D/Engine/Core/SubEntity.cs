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

using Axiom.MathLib;

namespace Axiom
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
    public class SubEntity : IRenderable
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
        /// <summary>
        ///    Detail to be used for rendering this sub entity.
        /// </summary>
        protected SceneDetailLevel renderDetail;
        /// <summary>
        ///		Current LOD index to use.
        /// </summary>
        internal int materialLodIndex;
        /// <summary>
        ///		Flag indicating whether this sub entity should be rendered or not.
        /// </summary>
        protected bool isVisible;
        /// <summary>
        ///		Blend buffer details for dedicated geometry.
        /// </summary>
        protected internal VertexData blendedVertexData;
        /// <summary>
        ///		Quick lookup of buffers.
        /// </summary>
        protected internal TempBlendedBufferInfo tempBlendedBuffer = new TempBlendedBufferInfo();

        protected Hashtable customParams = new Hashtable();

        #endregion Fields

        #region Constructor

        /// <summary>
        ///		Internal constructor, only allows creation of SubEntities within the engine core.
        /// </summary>
        internal SubEntity()
        {
            material = MaterialManager.Instance.GetByName( "BaseWhite" );
            renderDetail = SceneDetailLevel.Solid;

            isVisible = true;
        }

        #endregion

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
                    throw new AxiomException( "Cannot set the subentity material to be null" );
                materialName = value;

                // load the material from the material manager (it should already exist
                material = MaterialManager.Instance.GetByName( materialName );

                if ( material == null )
                {
                    LogManager.Instance.Write(
                        "Cannot assign material '{0}' to SubEntity '{1}' because the material doesn't exist.", materialName, parent.Name );

                    // give it base white so we can continue
                    material = MaterialManager.Instance.GetByName( "BaseWhite" );
                }

                // ensure the material is loaded.  It will skip it if it already is
                material.Load();

                // since the material has changed, re-evaulate its support of skeletal animation
                if ( parent.Mesh.HasSkeleton )
                {
                    parent.ReevaluateVertexProcessing();
                }
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

        #endregion

        #region Methods

        /// <summary>
        ///		Internal method for preparing this sub entity for use in animation.
        /// </summary>
        protected internal void PrepareTempBlendBuffers()
        {
            blendedVertexData = parent.CloneVertexDataRemoveBlendInfo( subMesh.vertexData );
            parent.ExtractTempBufferInfo( blendedVertexData, tempBlendedBuffer );
        }

        #endregion Methods

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
            }
        }

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
                return material.GetBestTechnique( materialLodIndex );
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="op"></param>
        public void GetRenderOperation( RenderOperation op )
        {
            // use LOD
            subMesh.GetRenderOperation( op, parent.MeshLodIndex );

            // Do we need to use software blended vertex data?
            if ( parent.HasSkeleton && !parent.IsHardwareSkinningEnabled )
            {
                op.vertexData = subMesh.useSharedVertices ?
                    parent.sharedBlendedVertexData : blendedVertexData;
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
            if ( parent.numBoneMatrices == 0 )
            {
                matrices[0] = parent.ParentFullTransform;
            }
            else
            {
                // use cached bone matrices of the parent entity
                for ( int i = 0; i < parent.numBoneMatrices; i++ )
                {
                    matrices[i] = parent.boneMatrices[i];
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

        /// <summary>
        /// 
        /// </summary>
        public SceneDetailLevel RenderDetail
        {
            get
            {
                return renderDetail;
            }
            set
            {
                renderDetail = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="camera"></param>
        /// <returns></returns>
        public float GetSquaredViewDepth( Camera camera )
        {
            // get the parent entitie's parent node
            Node node = parent.ParentNode;

            Debug.Assert( node != null );

            return node.GetSquaredViewDepth( camera );
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
                // get the parent entitie's parent node
                Node node = parent.ParentNode;

                Debug.Assert( node != null );

                return parent.ParentNode.Lights;
            }
        }

        public Vector4 GetCustomParameter( int index )
        {
            if ( customParams[index] == null )
            {
                throw new Exception( "A parameter was not found at the given index" );
            }
            else
            {
                return (Vector4)customParams[index];
            }
        }

        public void SetCustomParameter( int index, Vector4 val )
        {
            customParams[index] = val;
        }

        public void UpdateCustomGpuParameter( GpuProgramParameters.AutoConstantEntry entry, GpuProgramParameters gpuParams )
        {
            if ( customParams[entry.data] != null )
            {
                gpuParams.SetConstant( entry.index, (Vector4)customParams[entry.data] );
            }
        }

        #endregion
    }
}
