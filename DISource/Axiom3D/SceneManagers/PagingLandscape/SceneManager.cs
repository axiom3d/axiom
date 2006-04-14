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
#endregion LGPL License

#region Namespace Declarations

using System;
using Axiom.MathLib;

using IntersectionSceneQuery = Axiom.IntersectionSceneQuery;
using RaySceneQuery = Axiom.RaySceneQuery;

using Axiom.SceneManagers.PagingLandscape.Data2D;
using Axiom.SceneManagers.PagingLandscape.Page;
using Axiom.SceneManagers.PagingLandscape.Renderable;
using Axiom.SceneManagers.PagingLandscape.Texture;
using Axiom.SceneManagers.PagingLandscape.Tile;

#endregion Using Directives

#region Versioning Information
/// File								Revision
/// ===============================================
/// OgrePagingLandScapeSceneManager.h		1.7
/// OgrePagingLandScapeSceneManager.cpp		1.9
/// 
#endregion

namespace Axiom.SceneManagers.PagingLandscape
{
    public enum Neighbor : int
    {
        North = 0,
        South = 1,
        East = 2,
        West = 3,
        Here = 4
    };

    /// <summary>
    ///		This is a basic SceneManager for organizing LandscapeRenderables into a total Landscape.
    ///		It loads a Landscape from a XML config file that specifices what textures/scale/virtual window/etc to use.
    /// </summary>
    public class SceneManager : Axiom.SceneManager
    {
        #region Fields

        /// <summary>
        ///  Landscape 2D Data manager.
        /// This class encapsulate the 2d data loading and unloading
        /// </summary>
        protected Data2DManager data2DManager;

        /// <summary>
        /// Landscape Texture manager.
        /// This class encapsulate the texture loading and unloading
        /// </summary>
        protected TextureManager textureManager;

        /// <summary>
        /// Landscape tiles manager to avoid creating and deleting terrain tiles.
        /// They are created at the plugin start and destroyed at the plugin unload.
        /// </summary>
        protected TileManager tileManager;

        /// <summary>
        /// Landscape Renderable manager to avoid creating and deleting renderables.
        /// They are created at the plugin start and destroyed at the plugin unload.
        /// </summary>
        protected RenderableManager renderableManager;

        /// <summary>
        /// Landscape pages for the terrain.
        /// </summary>
        protected PageManager pageManager;

        protected bool needOptionsUpdate;

        /// <summary>
        ///  flag to indicate if the world geometry was setup
        /// </summary>
        protected bool worldGeomIsSetup;

        protected TileInfo impactInfo;
        protected Vector3 impact;

        #endregion Fields

        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        public SceneManager()
        {
            tileManager = null;
            renderableManager = null;
            textureManager = null;
            data2DManager = null;
            pageManager = null;
            needOptionsUpdate = false;
            worldGeomIsSetup = false;
        }
        //~PagingLandScapeSceneManager( );
        #endregion Constructor

        #region Methods

        /// <summary>
        /// Creates a specialized Camera 
        /// </summary>
        /// <param name="name">Camera name</param>
        /// <returns>camera</returns>
        public override Axiom.Camera CreateCamera( string name )
        {
            Axiom.Camera c = new Camera( name, this );
            cameraList.Add( name, c );
            return c;
        }


        /// <summary>
        /// Loads the LandScape using parameters in the given config file.
        /// </summary>
        /// <param name="filename"></param>
        public override void LoadWorldGeometry( string filename )
        {
            if ( worldGeomIsSetup )
            {
                ClearScene();
            }
            // Load the configuration file
            optionList = PagingLandscape.Options.Instance;
            PagingLandscape.Options.Instance.Load( filename );

            // Create the Tile and Renderable and 2D Data Manager
            tileManager = TileManager.Instance;
            renderableManager = RenderableManager.Instance;
            textureManager = TextureManager.Instance;
            data2DManager = Data2DManager.Instance;
            pageManager = new PageManager( rootSceneNode );
            worldGeomIsSetup = true;
        }


        /// <summary>
        /// Empties the entire scene, inluding all SceneNodes, Cameras, Entities and Lights etc.
        /// </summary>
        public override void ClearScene()
        {
            if ( worldGeomIsSetup )
            {
                worldGeomIsSetup = false;

                // Delete the Managers
                if ( pageManager != null )
                {
                    pageManager.Dispose();
                    pageManager = null;
                }
                if ( tileManager != null )
                {
                    tileManager.Dispose();
                    tileManager = null;
                }
                if ( renderableManager != null )
                {
                    renderableManager.Dispose();
                    renderableManager = null;
                }
                if ( textureManager != null )
                {
                    textureManager.Dispose();
                    textureManager = null;
                }
                if ( data2DManager != null )
                {
                    data2DManager.Dispose();
                    data2DManager = null;
                }
            }
            //Call the default
            base.ClearScene();
        }


        /// <summary>
        /// Internal method for updating the scene graph ie the tree of SceneNode instances managed by this class.
        /// </summary>
        /// <param name="cam"></param>
        /// <remarks>
        ///	This must be done before issuing objects to the rendering pipeline, since derived transformations from
        ///	parent nodes are not updated until required. This SceneManager is a basic implementation which simply
        ///	updates all nodes from the root. This ensures the scene is up to date but requires all the nodes
        ///	to be updated even if they are not visible. Subclasses could trim this such that only potentially visible
        ///	nodes are updated.
        /// </remarks>
        protected override void UpdateSceneGraph( Axiom.Camera cam )
        {
            // entry into here could come before SetWorldGeometry 
            // got called which could be disasterous
            // so check for init

            if ( worldGeomIsSetup )
            {
                pageManager.Update( (Camera)cam );
                renderableManager.ResetVisibles();
            }

            // Call the default
            base.UpdateSceneGraph( cam );
        }


        /// <summary>
        /// Creates a RaySceneQuery for this scene manager.
        /// </summary>
        /// <param name="ray">Details of the ray which describes the region for this query.</param>
        /// <param name="mask">The query mask to apply to this query; can be used to filter out certain objects; see SceneQuery for details.</param>
        /// <returns>
        ///	The instance returned from this method must be destroyed by calling
        ///	SceneManager::destroyQuery when it is no longer required.
        /// </returns>
        /// <remarks>
        ///	This method creates a new instance of a query object for this scene manager, 
        ///	looking for objects which fall along a ray. See SceneQuery and RaySceneQuery 
        ///	for full details.
        /// </remarks>
        public override RaySceneQuery CreateRayQuery( Ray ray, ulong mask )
        {
            RaySceneQuery q = new Query.RaySceneQuery( this );
            q.Ray = ray;
            q.QueryMask = mask;
            return q;
        }


        /// <summary>
        /// Creates an IntersectionSceneQuery for this scene manager.
        /// </summary>
        /// <param name="mask">The query mask to apply to this query; can be used to filter out
        ///	certain objects; see SceneQuery for details.
        ///	</param>
        /// <returns>
        ///	The instance returned from this method must be destroyed by calling
        ///	SceneManager::destroyQuery when it is no longer required.
        /// </returns>
        /// <remarks>
        ///	This method creates a new instance of a query object for locating
        ///	intersecting objects. See SceneQuery and IntersectionSceneQuery
        ///	for full details.
        /// </remarks>
        public override IntersectionSceneQuery CreateIntersectionQuery( ulong mask )
        {
            IntersectionSceneQuery q = new Query.IntersectionSceneQuery( this );
            q.QueryMask = mask;
            return q;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="start">begining of the segment </param>
        /// <param name="end">where it ends</param>
        /// <param name="result">where it intersects with terrain</param>
        /// <returns></returns>
        /// <remarks>Intersect mainly with Landscape</remarks>
        public bool IntersectSegment( Vector3 start, Vector3 end, ref Vector3 result )
        {
            return IntersectSegment( start, end, ref result, false );
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="start">begining of the segment </param>
        /// <param name="end">where it ends</param>
        /// <param name="result">where it intersects with terrain</param>
        /// <param name="modif">If it does modify the terrain</param>
        /// <returns></returns>
        /// <remarks>Intersect mainly with Landscape</remarks>
        public bool IntersectSegment( Vector3 start, Vector3 end, ref Vector3 result, bool modif )
        {
            Vector3 begin = start;
            Vector3 dir = end - start;
            dir.Normalize();
            Tile.Tile t = pageManager.GetTile( start );
            if ( t != null )
            {
                // if you want to be able to intersect from a point outside the canvas
                int pageSize = (int)optionList["PageSize"] - 1;
                float W = (float)optionList["World_Width"] * 0.5f * pageSize * ( (Vector3)optionList["Scale"] ).x;
                float H = (float)optionList["World_Height"] * 0.5f * pageSize * ( (Vector3)optionList["Scale"] ).z;
                while ( start.y > 0.0f && start.y < 999999.0f &&
                    ( ( start.x < -W || start.x > W ) ||
                    ( start.z < -H || start.z > H ) ) )
                    start += dir;

                if ( start.y < 0.0f || start.y > 999999.0f )
                {
                    result = new Vector3( -1, -1, -1 );
                    return false;
                }
                t = pageManager.GetTile( start );

                // if you don't want to be able to intersect from a point outside the canvas
                //        *result = Vector3( -1, -1, -1 );
                //        return false;
            }

            bool impact = false;

            //special case...
            if ( dir.x == 0 && dir.z == 0 )
            {
                if ( start.y <= data2DManager.GetRealWorldHeight( start.x, start.z, t.Info ) )
                {
                    result = start;
                    impact = true;
                }
            }
            else
            {
                //    dir.x = dir.x * mOptions.scale.x;
                //    dir.y = dir.y * mOptions.scale.y;
                //    dir.z = dir.z * mOptions.scale.z;
                impact = t.IntersectSegment( start, dir, result );
            }


            // deformation
            if ( impact && modif )
            {

                int X = (int)( result.x / ( (Vector3)optionList["Scale"] ).x );
                int Z = (int)( result.z / ( (Vector3)optionList["Scale"] ).z );

                int pageSize = (int)optionList["PageSize"] - 1;

                int W = (int)( (float)optionList["World_Width"] * 0.5f * pageSize );
                int H = (int)( (float)optionList["World_Height"] * 0.5f * pageSize );

                if ( X < -W || X > W || Z < -H || Z > H )
                    return true;

                impact = ( result != Vector3.Zero );
                const int radius = 7;

                // Calculate the minimum X value 
                // make sure it is still on the height map
                int Xmin = -radius;
                if ( Xmin + X < -W )
                    Xmin = -X - W;

                // Calculate the maximum X value
                // make sure it is still on the height map
                int Xmax = radius;
                if ( Xmax + X > W )
                    Xmax = W - X;


                // Main loop to draw the circle on the height map 
                // (goes through each X value)

                for ( int Xcurr = Xmin; Xcurr <= radius; Xcurr++ )
                {
                    float Precalc = ( radius * radius ) - ( Xcurr * Xcurr );
                    if ( Precalc > 1.0f )
                    {
                        // Determine the minimum and maximum Z value for that 
                        // line in the circle (that X value)
                        int Zmax = (int)Math.Sqrt( Precalc );
                        int Zmin = -Zmax;

                        // Makes sure the values found are both on the height map
                        if ( Zmin + Z < -H )
                            Zmin = -Z - H;

                        if ( Zmax + Z > H )
                            Zmax = H - Z;

                        // For each of those Z values, calculate the new Y value
                        for ( int Zcurr = Zmin; Zcurr < Zmax; Zcurr++ )
                        {
                            // get results by page index ?
                            Vector3 currpoint = new Vector3( ( X + Xcurr ), 0.0f, ( Z + Zcurr ) );
                            Tile.Tile p = pageManager.GetTileUnscaled( currpoint );
                            if ( p != null && p.IsLoaded )
                            {
                                // Calculate the new theoretical height for the current point on the circle
                                float dY = (float)Math.Sqrt( Precalc - ( Zcurr * Zcurr ) ) * 10.0f;//* 0.01f

                                impactInfo = p.Info;
                                data2DManager.DeformHeight( currpoint, dY, p.Info );
                                p.Renderable.NeedUpdate();
                            }
                        }
                    }

                }

            }
            return true;
        }

        #endregion Methods

    }

}
