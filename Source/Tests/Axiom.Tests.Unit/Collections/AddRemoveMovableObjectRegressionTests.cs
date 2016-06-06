using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Axiom.Core;

using Gallio.Framework;
using MbUnit.Framework;
using MbUnit.Framework.ContractVerifiers;

using TypeMock.ArrangeActAssert;

namespace Axiom.UnitTests.Collections
{
    [TestFixture]
    public class AddRemoveMovableObjectRegressionTests
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
        public void SceneNode_AddThenRemoveEntity_ShouldNotThrowException()
        {

            sceneManager.RootSceneNode.AttachObject( entity );

            sceneManager.RootSceneNode.DetachObject( entity ); // detach old object

            Assert.IsTrue( sceneManager.RootSceneNode.ObjectCount == 0 );
        }

    }
}
