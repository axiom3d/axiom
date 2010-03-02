using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Axiom.Core;
using Axiom.Core.Collections;

using Gallio.Framework;
using MbUnit.Framework;
using MbUnit.Framework.ContractVerifiers;

using TypeMock;
using TypeMock.ArrangeActAssert;

namespace Axiom.UnitTests.Collections
{
    [TestFixture]
    public class RenameMovableObjectRegressionTests
    {
        private SceneManager sceneManager;
        private Entity entity;

        /// <summary>
        /// Sets up each test.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            this.sceneManager = Isolate.Fake.Instance<SceneManager>();
            Isolate.WhenCalled( () => this.sceneManager.RootSceneNode ).WillReturn( new SceneNode( this.sceneManager, "Root" ) );
            this.entity = Isolate.Fake.Instance<Entity>();
        }


        [Test]
        public void SceneNode_AddRenameEntity_ShouldNotThrowException()
        {

            sceneManager.RootSceneNode.AttachObject( entity );
            entity.Name = "newName";
            Assert.IsTrue( sceneManager.RootSceneNode.GetObject( entity.Name ) != null );

            sceneManager.RootSceneNode.DetachAllObjects();
        }

        [Test]
        public void SceneManager_AddRenameEntity_ShouldNotThrowException()
        {

            sceneManager.RootSceneNode.AttachObject( entity );
            entity.Name = "newName";
            Assert.IsTrue( sceneManager.GetMovableObject( entity.Name, "Entity" ) != null );

            sceneManager.RootSceneNode.DetachAllObjects();
        }
    }
}
