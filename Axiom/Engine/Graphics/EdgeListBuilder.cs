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

namespace Axiom.Graphics {
	/// <summary>
	///     General utility class for building edge lists for geometry.
	/// </summary>
	/// <remarks>
	///     You can add multiple sets of vertex and index data to build and edge list. 
	///     Edges will be built between the various sets as well as within sets; this allows 
	///     you to use a model which is built from multiple SubMeshes each using 
	///     separate index and (optionally) vertex data and still get the same connectivity 
	///     information. It's important to note that the indexes for the edge will be constrained
	///     to a single vertex buffer though (this is required in order to render the edge).
	/// </remarks>
	public class EdgeListBuilder {
		public EdgeListBuilder() {
		}

        #region Structs

        /// <summary>
        ///     A vertex can actually represent several vertices in the final model, because
        ///     vertices along texture seams etc will have been duplicated. In order to properly
        ///     evaluate the surface properties, a single common vertex is used for these duplicates,
        ///     and the faces hold the detail of the duplicated vertices.
        /// </summary>
        public struct CommonVertex {
            /// <summary>
            ///     Location of point in euclidean space.
            /// </summary>
            public Vector3 position;
            /// <summary>
            ///     Place of vertex in original vertex set.
            /// </summary>
            public int index;
            /// <summary>
            ///      The vertex set this came from.
            /// </summary>
            public int vertexSet;
        };

        #endregion Structs
	}
}
