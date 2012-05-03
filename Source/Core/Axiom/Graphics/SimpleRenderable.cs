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

using Axiom.Core;
using Axiom.Math;
using Axiom.Core.Collections;

#endregion Namespace Declarations

namespace Axiom.Graphics
{
	/// <summary>
	/// Summary description for SimpleRenderable.
	/// </summary>
	public abstract class SimpleRenderable : MovableObject, IRenderable
	{
		#region Fields

		protected RenderOperation renderOperation = new RenderOperation();
		protected Matrix4 worldTransform = Matrix4.Identity;
		protected AxisAlignedBox box;
		protected string materialName;
		protected Material material;
		protected SceneManager sceneMgr;
		protected Camera camera;
		protected static long nextAutoGenName;

		protected VertexData vertexData;
		protected IndexData indexData;

		/// <summary>
		///    Empty light list to use when there is no parent for this renderable.
		/// </summary>
		protected LightList dummyLightList = new LightList();

		protected List<Vector4> customParams = new List<Vector4>();

		#endregion Fields

		#region Constructor

		/// <summary>
		///		Default constructor.
		/// </summary>
		public SimpleRenderable()
			: this( "SimpleRenderable" + nextAutoGenName++ ) {}

		/// <summary>
		///		Default constructor.
		/// </summary>
		public SimpleRenderable( string name )
			: base( name )
		{
			materialName = "BaseWhite";
			material = (Material)MaterialManager.Instance[ "BaseWhite" ];
			name = "SimpleRenderable" + nextAutoGenName++;
			material.Load();
		}

		private void LoadDefaultMaterial()
		{
			this.materialName = "BaseWhite";
			this.material = (Material)MaterialManager.Instance[ "BaseWhite" ];
			this.material.Load();
		}

		#endregion Constructor

		#region Implementation of MovableObject

		/// <summary>
		///
		/// </summary>
		public override AxisAlignedBox BoundingBox
		{
			get
			{
				return (AxisAlignedBox)box.Clone();
			}
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="camera"></param>
		public override void NotifyCurrentCamera( Camera camera )
		{
			this.camera = camera;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="queue"></param>
		public override void UpdateRenderQueue( RenderQueue queue )
		{
			// add ourself to the render queue
			queue.AddRenderable( this, this.RenderQueueGroup );
		}

		#endregion Implementation of MovableObject

		#region IRenderable Members

		public bool CastsShadows
		{
			get
			{
				return CastShadows;
			}
		}

		/// <summary>
		///
		/// </summary>
		public virtual Material Material
		{
			get
			{
				return material;
			}
			set
			{
				material = value;
				if ( material != null )
				{
					materialName = material.Name;
				}
			}
		}

		public virtual Technique Technique
		{
			get
			{
				return material.GetBestTechnique();
			}
		}

		public virtual RenderOperation RenderOperation
		{
			get
			{
				return renderOperation;
			}
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="matrices"></param>
		public virtual void GetWorldTransforms( Matrix4[] matrices )
		{
			matrices[ 0 ] = worldTransform * parentNode.FullTransform;
		}

		public bool NormalizeNormals
		{
			get
			{
				return false;
			}
		}

		/// <summary>
		///
		/// </summary>
		public ushort NumWorldTransforms
		{
			get
			{
				return 1;
			}
		}

		/// <summary>
		///
		/// </summary>
		public virtual bool UseIdentityProjection
		{
			get
			{
				return false;
			}
		}

		/// <summary>
		///
		/// </summary>
		public virtual bool UseIdentityView
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
		public abstract Real GetSquaredViewDepth( Camera camera );

		/// <summary>
		///
		/// </summary>
		public virtual Quaternion WorldOrientation
		{
			get
			{
				return parentNode.DerivedOrientation;
			}
		}

		/// <summary>
		///
		/// </summary>
		public virtual Vector3 WorldPosition
		{
			get
			{
				return parentNode.DerivedPosition;
			}
		}

		public LightList Lights
		{
			get
			{
				return QueryLights();
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
			{
				customParams.Add( Vector4.Zero );
			}
			customParams[ index ] = val;
		}

		public void UpdateCustomGpuParameter( GpuProgramParameters.AutoConstantEntry entry, GpuProgramParameters gpuParams )
		{
			if ( customParams.Count > entry.Data && customParams[ entry.Data ] != null )
			{
				gpuParams.SetConstant( entry.PhysicalIndex, (Vector4)customParams[ entry.Data ] );
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
					if ( renderOperation != null )
					{
						if ( !this.renderOperation.IsDisposed )
						{
							this.renderOperation.Dispose();
						}

						renderOperation = null;
					}

					if ( indexData != null )
					{
						if ( !this.indexData.IsDisposed )
						{
							indexData.Dispose();
						}

						this.indexData = null;
					}

					if ( vertexData != null )
					{
						if ( !this.vertexData.IsDisposed )
						{
							vertexData.Dispose();
						}

						this.vertexData = null;
					}
				}

				// There are no unmanaged resources to release, but
				// if we add them, they need to be released here.
			}

			base.dispose( disposeManagedResources );
		}

		#endregion IDisposable Implementation
	}
}
