#region MIT/X11 License

//Copyright © 2003-2011 Axiom 3D Rendering Engine Project
//
//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in
//all copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//THE SOFTWARE.

#endregion License

using System.Collections.Generic;
using Axiom.Core;
using Axiom.Graphics;
using Axiom.Math;
using SIS = SharpInputSystem;

namespace Axiom.Samples.Instancing
{
	/// <summary>
	/// </summary>
	public class InstancingSample : SdkSample
	{
		private static readonly string[] meshes =
			{
				"razor",
				"knot",
				"tudorhouse",
				"WoodPallet"
			};

		private enum CurrentGeomOpt
		{
			INSTANCE_OPT,
			STATIC_OPT,
			ENTITY_OPT
		};

		private static readonly int maxObjectsPerBatch = 80;
		private static readonly int numTypeMeshes = meshes.Length;

		private double mAvgFrameTime;
		private int mSelectedMesh;
		private int mNumMeshes;
		private int objectCount;
		private string mDebugText;
		private CurrentGeomOpt mCurrentGeomOpt;

		private int mNumRendered;

		private Timer mTimer;
		private double mLastTime, mBurnAmount;

		private List<InstancedGeometry> renderInstance;
		private List<StaticGeometry> renderStatic;
		private List<Entity> renderEntity;
		private List<SceneNode> nodes;
		private List<List<Vector3>> posMatrices;

		/// <summary>
		/// </summary>
		public InstancingSample()
		{
			Metadata[ "Title" ] = "Instancing";
			Metadata[ "Description" ] = "A demo of different methods to handle a large number of objects.";
			Metadata[ "Thumbnail" ] = "thumb_instancing.png";
			Metadata[ "Category" ] = "Geometry";
		}

		/// <summary>
		/// </summary>
		/// <param name="evt"> </param>
		/// <returns> </returns>
		public override bool FrameRenderingQueued( FrameEventArgs evt )
		{
			burnCPU();
			return base.FrameRenderingQueued( evt ); // don't forget the parent class updates!
		}

		private void burnCPU()
		{
			double mStartTime = this.mTimer.Microseconds/1000000.0f; //convert into seconds
			double mCurTime = mStartTime;
			double mStopTime = this.mLastTime + this.mBurnAmount;
			double mCPUUsage;

			while ( mCurTime < mStopTime )
			{
				mCurTime = this.mTimer.Microseconds/1000000.0f; //convert into seconds
			}

			if ( mCurTime - this.mLastTime > 0.00001f )
			{
				mCPUUsage = ( mCurTime - mStartTime )/( mCurTime - this.mLastTime )*100.0f;
			}
			else
			{
				mCPUUsage = float.MaxValue;
			}

			this.mLastTime = this.mTimer.Microseconds/1000000.0f; //convert into seconds
		}

		/// <summary>
		/// </summary>
		protected override void SetupContent()
		{
			// Set ambient light
			SceneManager.AmbientLight = new ColorEx( 0.2f, 0.2f, 0.2f );
			Light l = SceneManager.CreateLight( "MainLight" );
			//add a skybox
			SceneManager.SetSkyBox( true, "Examples/MorningSkyBox", 1000 );
			//setup the light
			l.Type = LightType.Directional;
			l.Direction = new Vector3( -0.5f, -0.5f, 0f );

			Camera.Position = new Vector3( 500f, 500f, 1500f );
			Camera.LookAt( new Vector3( 0f, 0f, 0f ) );
			DragLook = true;

			Plane plane;
			plane.Normal = Vector3.UnitY;
			plane.D = 100;
			MeshManager.Instance.CreatePlane( "Myplane", ResourceGroupManager.DefaultResourceGroupName, plane, 1500, 1500, 20, 20,
			                                  true, 1, 5, 5, Vector3.UnitZ );
			Entity pPlaneEnt = SceneManager.CreateEntity( "plane", "Myplane" );
			pPlaneEnt.MaterialName = "Examples/Rockwall";
			pPlaneEnt.CastShadows = false;
			SceneManager.RootSceneNode.CreateChildSceneNode().AttachObject( pPlaneEnt );

			CompositorManager.Instance.AddCompositor( Viewport, "Bloom" );

			SetupControls();

            ICollection<string> syntaxCodes = Root.Instance.RenderSystem.Capabilities.ShaderProfiles;
            foreach ( string syntax in syntaxCodes )
            {
            	LogManager.Instance.Write( "supported syntax : ", syntax );
            }

            this.mNumMeshes = 160;
			this.mNumRendered = 0;
			this.mSelectedMesh = 0;
			this.mBurnAmount = 0;
			this.mCurrentGeomOpt = CurrentGeomOpt.ENTITY_OPT;
			CreateCurrentGeomOpt();

			this.mTimer = new Timer();
			this.mLastTime = this.mTimer.Microseconds/1000000.0f;
		}

		private void CreateCurrentGeomOpt()
		{
			this.objectCount = this.mNumMeshes;
			this.mNumRendered = 1;

			while ( this.objectCount > maxObjectsPerBatch )
			{
				this.mNumRendered++;
				this.objectCount -= maxObjectsPerBatch;
			}

			System.Diagnostics.Debug.Assert( this.mSelectedMesh < numTypeMeshes );
			var m = (Mesh)MeshManager.Instance.GetByName( meshes[ this.mSelectedMesh ] + ".mesh" );
			if ( m == null )
			{
				m = MeshManager.Instance.Load( meshes[ this.mSelectedMesh ] + ".mesh",
				                               ResourceGroupManager.AutoDetectResourceGroupName );
			}
			Real radius = m.BoundingSphereRadius;

			// could/should print on screen mesh name, 
			//optimisation type, 
			//mesh vertices num, 
			//32 bit or not, 
			//etC..
			this.posMatrices = new List<List<Vector3>>( this.mNumRendered );


			var posMatCurr = new List<List<Vector3>>( this.mNumRendered );
			for ( int index = 0; index < this.mNumRendered; index++ )
			{
				this.posMatrices.Add( new List<Vector3>( this.mNumMeshes ) );
				posMatCurr.Add( this.posMatrices[ index ] );
			}

			int i = 0, j = 0;
			for ( int p = 0; p < this.mNumMeshes; p++ )
			{
				for ( int k = 0; k < this.mNumRendered; k++ )
				{
					posMatCurr[ k ].Add( new Vector3( radius*i, k*radius, radius*j ) );
				}
				if ( ++j == 10 )
				{
					j = 0;
					i++;
				}
			}
			posMatCurr.Clear();


			switch ( this.mCurrentGeomOpt )
			{
				case CurrentGeomOpt.INSTANCE_OPT:
					CreateInstanceGeom();
					break;
				case CurrentGeomOpt.STATIC_OPT:
					CreateStaticGeom();
					break;
				case CurrentGeomOpt.ENTITY_OPT:
					CreateEntityGeom();
					break;
			}
		}

		private void DestroyCurrentGeomOpt()
		{
			switch ( this.mCurrentGeomOpt )
			{
				case CurrentGeomOpt.INSTANCE_OPT:
					DestroyInstanceGeom();
					break;
				case CurrentGeomOpt.STATIC_OPT:
					DestroyStaticGeom();
					break;
				case CurrentGeomOpt.ENTITY_OPT:
					DestroyEntityGeom();
					break;
			}
		}

		private void SetupControls()
		{
			SelectMenu technique = TrayManager.CreateThickSelectMenu( TrayLocation.TopLeft, "TechniqueType",
			                                                          "Instancing Technique", 200, 3 );
			technique.AddItem( "Instancing" );
			technique.AddItem( "Static Geometry" );
			technique.AddItem( "Independent Entities" );

			SelectMenu objectType = TrayManager.CreateThickSelectMenu( TrayLocation.TopLeft, "ObjectType", "Object : ", 200, 4 );
			objectType.AddItem( "razor" );
			objectType.AddItem( "knot" );
			objectType.AddItem( "tudorhouse" );
			objectType.AddItem( "woodpallet" );

			TrayManager.CreateThickSlider( TrayLocation.TopLeft, "ObjectCountSlider", "Object count", 200, 50, 0, 1000, 101 ).
				SetValue( 160, false );

			TrayManager.CreateThickSlider( TrayLocation.TopLeft, "CPUOccupationSlider", "CPU Load (ms)", 200, 75, 0, 1000.0f/60,
			                               20 );

			TrayManager.CreateCheckBox( TrayLocation.TopLeft, "ShadowCheckBox", "Shadows", 200 );

			TrayManager.CreateCheckBox( TrayLocation.TopLeft, "PostEffectCheckBox", "Post Effect", 200 );

			TrayManager.ShowCursor();
		}

		private void CreateInstanceGeom()
		{
			if ( Root.Instance.RenderSystem.Capabilities.HasCapability( Capabilities.VertexPrograms ) == false )
			{
				throw new AxiomException( "Your video card doesn't support batching" );
			}

			Entity ent = SceneManager.CreateEntity( meshes[ this.mSelectedMesh ], meshes[ this.mSelectedMesh ] + ".mesh" );


			this.renderInstance = new List<InstancedGeometry>( this.mNumRendered );

            //Load a mesh to read data from.	
            var batch = new InstancedGeometry(SceneManager, meshes[this.mSelectedMesh] + "s")
            {
                CastShadows = true,
                Origin = Vector3.Zero,
                BatchInstanceDimensions = new Vector3(1000000f, 1000000f, 1000000f)
            };

            int batchSize = ( this.mNumMeshes > maxObjectsPerBatch ) ? maxObjectsPerBatch : this.mNumMeshes;
			SetupInstancedMaterialToEntity( ent );
			for ( int i = 0; i < batchSize; i++ )
			{
				batch.AddEntity( ent, Vector3.Zero );
			}

			batch.Build();


			int k;
			for ( k = 0; k < this.mNumRendered - 1; k++ )
			{
				batch.AddBatchInstance();
			}

            k = 0;
            foreach ( var batchInstance in batch.BatchInstances )
			{
				int j = 0;
				foreach ( var instancedObject in batchInstance.Objects )
				{
					instancedObject.Position = this.posMatrices[ k ][ j ];
					++j;
				}
				k++;
			}
			batch.IsVisible = true;
			this.renderInstance[ 0 ] = batch;

			SceneManager.RemoveEntity( ent );
		}

		private void SetupInstancedMaterialToEntity( Entity ent )
		{
			for ( int i = 0; i < ent.SubEntityCount; ++i )
			{
				SubEntity se = ent.GetSubEntity( i );
				string materialName = se.MaterialName;
				se.MaterialName = BuildInstancedMaterial( materialName );
			}
		}

		private string BuildInstancedMaterial( string originalMaterialName )
		{
			// already instanced ?
			if ( originalMaterialName.EndsWith( "/instanced" ) )
			{
				return originalMaterialName;
			}

			var originalMaterial = (Material)MaterialManager.Instance.GetByName( originalMaterialName );

			// if originalMat doesn't exists use "Instancing" material name
			string instancedMaterialName = ( null == originalMaterial ? "Instancing" : originalMaterialName + "/Instanced" );
			var instancedMaterial = (Material)MaterialManager.Instance.GetByName( instancedMaterialName );

			// already exists ?
			if ( null == instancedMaterial )
			{
				instancedMaterial = originalMaterial.Clone( instancedMaterialName );
				instancedMaterial.Load();
				Technique t = instancedMaterial.GetBestTechnique();
				for ( int pItr = 0; pItr < t.PassCount; pItr++ )
				{
					Pass p = t.GetPass( pItr );
					p.SetVertexProgram( "Instancing", false );
					p.SetShadowCasterVertexProgram( "InstancingShadowCaster" );
				}
			}
			instancedMaterial.Load();
			return instancedMaterialName;
		}

		private void DestroyInstanceGeom()
		{
		}

		private void CreateStaticGeom()
		{
		}

		private void DestroyStaticGeom()
		{
		}

		private void CreateEntityGeom()
		{
			int k = 0;
			int y = 0;
			this.renderEntity = new List<Entity>( this.mNumMeshes );
			this.nodes = new List<SceneNode>( this.mNumMeshes );

			for ( int i = 0; i < this.mNumMeshes; i++ )
			{
				if ( y == maxObjectsPerBatch )
				{
					y = 0;
					k++;
				}

				var node = SceneManager.RootSceneNode.CreateChildSceneNode( "node" + i.ToString() );
				var entity = SceneManager.CreateEntity( meshes[ this.mSelectedMesh ] + i.ToString(),
				                                        meshes[ this.mSelectedMesh ] + ".mesh" );
				node.AttachObject( entity );
				node.Position = this.posMatrices[ k ][ y ];

				this.nodes.Add( node );
				this.renderEntity.Add( entity );

				y++;
			}
		}

		private void DestroyEntityGeom()
		{
		}

		protected override void CleanupContent()
		{
			DestroyCurrentGeomOpt();
			this.mTimer = null;

			base.CleanupContent();
		}
	}
}