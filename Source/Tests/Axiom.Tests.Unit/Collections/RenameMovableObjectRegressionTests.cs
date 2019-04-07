#region LGPL License

// Axiom Graphics Engine Library
// Copyright (C) 2003-2009 Axiom Project Team
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

using System;

using Axiom.Collections;
using Axiom.Core;
using Moq;
using NUnit.Framework;

#endregion

namespace Axiom.UnitTests.Collections
{
    [TestFixture]
    public class RenameMovableObjectRegressionTests
    {
        private Mock<SceneManager> sceneManagerMock;
        private Mock<Entity> entityMock;
        private SceneManager sceneManager;
        private Entity entity;

        /// <summary>
        /// Sets up each test.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            this.sceneManagerMock = new Mock<SceneManager>();
            sceneManagerMock.Setup( x => x.RootSceneNode )
                .Returns( new SceneNode( this.sceneManagerMock.Object, "Root" ) );
            this.entityMock = new Mock<Entity>();
            this.sceneManager = sceneManagerMock.Object;
            this.entity = entityMock.Object;
        }


        [Test]
        public void SceneNode_AddRenameEntity_ShouldNotThrowException()
        {
            sceneManager.RootSceneNode.AttachObject( entity );
            entity.Name = "newName_" + "SceneNode_AddRenameEntity";
            Assert.IsTrue( sceneManager.RootSceneNode.GetObject( entity.Name ) != null );

            sceneManager.RootSceneNode.DetachAllObjects();
        }

        [Test]
        public void SceneManager_AddRenameEntity_ShouldNotThrowException()
        {
            sceneManager.RootSceneNode.AttachObject( entity );
            entity.Name = "newName_" + "SceneManager_AddRenameEntity";
            Assert.IsTrue( sceneManager.GetMovableObject( entity.Name, "Entity" ) != null );

            sceneManager.RootSceneNode.DetachAllObjects();
        }

        [Test]
        public void SceneManager_AttachDetachRenameEntity_ShouldNotThrowException()
        {
            sceneManager.RootSceneNode.AttachObject( entity );
            sceneManager.RootSceneNode.DetachAllObjects();
            entity.Name = "newName_" + "SceneManager_AddRenameEntity";
            Assert.IsTrue( sceneManager.GetMovableObject( entity.Name, "Entity" ) != null );
        }

        [Test]
        public void MovableObject_RenameUnattachedEntity_ShouldNotThrowException()
        {
            entity.Name = "newName_" + "MovableObject_RenameUnattachedEntity";
            Assert.IsTrue( sceneManager.GetMovableObject( entity.Name, "Entity" ) != null );
        }
    }
}
