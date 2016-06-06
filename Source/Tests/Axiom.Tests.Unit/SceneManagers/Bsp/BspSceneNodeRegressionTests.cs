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
using Axiom.SceneManagers.Bsp;

using MbUnit.Framework;

using TypeMock.ArrangeActAssert;

#endregion

namespace Axiom.UnitTests.SceneManagers.Bsp
{
    /// <summary>
    /// Regression tests for the <see cref="SceneNode"/> class.
    /// </summary>
    /// <remarks>
    /// The tests were originally written to resolve Ticket #69,
    /// https://sourceforge.net/apps/trac/axiomengine/ticket/69,
    /// a problem with node removal functionality.
    /// </remarks>
    [ TestFixture ]
    public class OctreeNodeRegressionTests
    {
        private SceneManager fakeSceneManager;
        private const string Name = "testName";

        /// <summary>
        /// Sets up each test.
        /// </summary>
        [ SetUp ]
        public void SetUp()
        {
            this.fakeSceneManager = Isolate.Fake.Instance<BspSceneManager>();
            Isolate.WhenCalled( () => this.fakeSceneManager.CreateSceneNode( Name ) ).WillReturn( new BspSceneNode( this.fakeSceneManager, Name ) );
        }

        /// <summary>
        /// Tears down each test.
        /// </summary>
        [ TearDown ]
        public void TearDown()
        {
            Isolate.CleanUp();
        }

        /// <summary>
        /// Verifies that a new child node can be created after a node with the same name has been removed by reference.
        /// </summary>
        [ Test ]
        public void TestRecreationOfChildNodeAfterRemovalByReference()
        {
            Node node = new BspSceneNode( this.fakeSceneManager );
            Node childNode = node.CreateChild( Name );

            node.RemoveChild( childNode );
            node.CreateChild( Name );
        }

        /// <summary>
        /// Verifies that a new child node can be created after a node with the same name has been removed by name.
        /// </summary>
        [ Test ]
        public void TestRecreationOfChildNodeAfterRemovalByName()
        {
            Node node = new BspSceneNode( this.fakeSceneManager );
            node.CreateChild( Name );

            node.RemoveChild( Name );
            node.CreateChild( Name );
        }

        /// <summary>
        /// Verifies that a new child node can be added after a node with the same name has been removed by reference.
        /// </summary>
        [ Test ]
        public void TestReaddingOfChildNodeAfterRemovalByReference()
        {
            Node node = new BspSceneNode( this.fakeSceneManager );
            Node childNode = node.CreateChild( Name );

            node.RemoveChild( childNode );
            node.AddChild( childNode );
        }

        /// <summary>
        /// Verifies that a new child node can be added after a node with the same name has been removed by name.
        /// </summary>
        [ Test ]
        public void TestReaddingOfChildNodeAfterRemovalByName()
        {
            Node node = new BspSceneNode( this.fakeSceneManager );
            Node childNode = node.CreateChild( Name );

            node.RemoveChild( Name );
            node.AddChild( childNode );
        }
    }
}