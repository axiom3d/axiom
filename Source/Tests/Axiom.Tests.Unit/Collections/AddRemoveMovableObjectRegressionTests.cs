﻿#region LGPL License

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

using Axiom.Core;
using FakeItEasy;
using NUnit.Framework;

#endregion

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
            var resourceMnager = A.Fake<ResourceManager>();
            var iManualResourceLoader = A.Fake<IManualResourceLoader>();

            var mesh = A.Fake<Mesh>();

            entity = A.Fake<Entity>();

            sceneManager = A.Fake<SceneManager>();
            A.CallTo(() => sceneManager.CreateEntity(A<string>.That.IsNotNull(), A<string>.That.IsNotNull())).Returns(entity);
        }


        [Test]
        public void SceneNode_AddThenRemoveEntity_ShouldNotThrowException()
        {
            var sceneNode = new SceneNode(this.sceneManager, "Root");

            var newEntity = sceneManager.CreateEntity("", "");

            sceneNode.AttachObject(newEntity);

            sceneNode.DetachObject(newEntity); // detach old object

            Assert.IsTrue(sceneNode.ObjectCount == 0);
        }

    }
}
