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
using Axiom.MathLib;
using Axiom.MathLib.Collections;

namespace Axiom.Core
{
    /// <summary>
    /// 
    /// </summary>
    public delegate bool RaySceneQueryResultEventHandler(object source, RayQueryResultEventArgs e);

	/// <summary>
	/// 	A class for performing queries on a scene.
	/// </summary>
	/// <remarks>
	/// 	This is an abstract class for performing a query on a scene, i.e. to retrieve
	/// 	a list of objects and/or world geometry sections which are potentially intersecting a
	/// 	given region. Note the use of the word 'potentially': the results of a scene query
	/// 	are generated based on bounding volumes, and as such are not correct at a triangle
	/// 	level; the user of the SceneQuery is expected to filter the results further if
	/// 	greater accuracy is required.
	/// 	<p/>
	/// 	Different SceneManagers will implement these queries in different ways to
	/// 	exploit their particular scene organization, and thus will provide their own
	/// 	concrete subclasses. In fact, these subclasses will be derived from subclasses
	/// 	of this class rather than directly because there will be region-type classes
	/// 	in between.
	/// 	<p/>
	/// 	These queries could have just been implemented as methods on the SceneManager,
	/// 	however, they are wrapped up as objects to allow 'compilation' of queries
	/// 	if deemed appropriate by the implementation; i.e. each concrete subclass may
	/// 	precalculate information (such as fixed scene partitions involved in the query)
	/// 	to speed up the repeated use of the query.
	/// 	<p/>
	/// 	You should never try to create a SceneQuery object yourself, they should be created
	/// 	using the SceneManager interfaces for the type of query required, e.g.
	/// 	SceneManager.CreateRaySceneQuery.
	/// </remarks>
	public abstract class SceneQuery
	{
        #region Fields

        protected SceneManager creator;
        protected ulong queryMask;

        #endregion
		
		#region Constructors
		
        /// <summary>
        ///    
        /// </summary>
        /// <param name="creator"></param>
		internal SceneQuery(SceneManager creator) {
            this.creator = creator;
		}
		
		#endregion
		
		#region Methods

        /// <summary>
        /// 
        /// </summary>
        public abstract void Execute();
		
		#endregion
		
		#region Properties
		
        /// <summary>
        ///    Sets the mask for results of this query.
        /// </summary>
        /// <remarks>
        ///    This method allows you to set a 'mask' to limit the results of this
        ///    query to certain types of result. The actual meaning of this value is
        ///    up to the application; basically SceneObject instances will only be returned
        ///    from this query if a bitwise AND operation between this mask value and the
        ///    SceneObject.QueryFlags value is non-zero. The application will
        ///    have to decide what each of the bits means.
        /// </remarks>
        public ulong QueryMask {
            get {
                return queryMask;
            }
            set {
                queryMask = value;
            }
        }

		#endregion
	}

    /// <summary>
    /// 
    /// </summary>
    public abstract class RaySceneQuery : SceneQuery {

        public event RaySceneQueryResultEventHandler QueryResult;
        protected Ray ray;

        public RaySceneQuery(SceneManager creator, Ray ray) : base(creator) { 
            this.ray = ray;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        internal void OnQueryResult(object source, RayQueryResultEventArgs e) {
            if(QueryResult != null) {
                QueryResult(source, e);
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class QueryResultEventArgs : System.EventArgs {
        protected SceneObject hitObject;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hitObject"></param>
        protected internal QueryResultEventArgs(SceneObject hitObject) {
            this.hitObject = hitObject;
        }

        /// <summary>
        /// 
        /// </summary>
        public SceneObject HitObject {
            get {
                return hitObject;
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class RayQueryResultEventArgs : QueryResultEventArgs {
        protected float distance;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hitObject"></param>
        /// <param name="distance"></param>
        protected internal RayQueryResultEventArgs(SceneObject hitObject, float distance) : base(hitObject) {
            this.distance = distance;
        }

        /// <summary>
        /// 
        /// </summary>
        public float Distance {
            get {
                return distance;
            }
        }
    }
}
