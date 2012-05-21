#region LGPL License

// Axiom Graphics Engine Library
// Copyright (C) 2003-2010 Axiom Project Team
// 
// The overall design, and a majority of the core engine and rendering code 
// contained within this library is a derivative of the open source Object Oriented 
// Graphics Engine OGRE, which can be found at http://ogre.sourceforge.net.  
// Many thanks to the OGRE team for maintaining such a high quality project.
// 
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
// 
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
// Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA

#endregion

#region Namespace Declarations

using Axiom.Core;
using Axiom.SceneManagers.Octree;

using MbUnit.Framework;

#endregion

namespace Axiom.UnitTests.SceneManagers.Octree
{
    /// <summary>
    /// Regression tests for the <see cref="OctreeSceneManager"/> class.
    /// </summary>
    /// <remarks>
    /// The tests were originally written to resolve Ticket #69,
    /// https://sourceforge.net/apps/trac/axiomengine/ticket/69,
    /// a problem with node removal functionality.
    /// </remarks>
    [ TestFixture ]
    public class OctreeSceneManagerRegressionTests
    {
        /// <summary>
        /// Verifies that the destruction of a scene node via the interface of its parent does in fact also
        /// remove that child node from the <see cref="OctreeSceneManager"/> scene graph.
        /// </summary>
        [ Test ]
        public void TestChildSceneNodeDestruction()
        {
            SceneManager sceneManager = new OctreeSceneManager( "Manager under test" );
            SceneNode node = sceneManager.CreateSceneNode( "testNode" );
            SceneNode childNode = node.CreateChildSceneNode( "childNode" );

            Assert.IsTrue( ManagerContainsNode( sceneManager, childNode ), "A child node was created but not added to the scene graph." );

            node.RemoveAndDestroyChild( childNode.Name );

            Assert.IsFalse( ManagerContainsNode( sceneManager, childNode ), "A child node was destroryed but not removed from the scene graph." );
        }

        /// <summary>
        /// Verifies that the simple removal of a scene node from its parent does NOT
        /// remove that child node from the <see cref="OctreeSceneManager"/> scene graph.
        /// </summary>
        [ Test ]
        public void TestChildSceneNodeRemoval()
        {
            SceneManager sceneManager = new OctreeSceneManager( "Manager under test" );
            SceneNode node = sceneManager.CreateSceneNode( "testNode" );
            SceneNode childNode = node.CreateChildSceneNode( "childNode" );

            Assert.IsTrue( ManagerContainsNode( sceneManager, childNode ), "A child node was created but not added to the scene graph." );

            node.RemoveChild( childNode.Name );

            Assert.IsTrue( ManagerContainsNode( sceneManager, childNode ), "A child node was removed from its parent but also incorrectly removed from the scene graph." );
        }

        private static bool ManagerContainsNode( SceneManager sceneManager, SceneNode childNode )
        {
            bool managerContainsChild = false;

            foreach ( SceneNode sceneNode in sceneManager.SceneNodes )
            {
                if ( sceneNode.Equals( childNode ) )
                {
                    managerContainsChild = true;
                }
            }
            return managerContainsChild;
        }
    }
}
