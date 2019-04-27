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

using NUnit.Framework;

#endregion

namespace Axiom.UnitTests.Core
{
    [TestFixture]
    public class ChainedEventTests
    {
        protected class TestEventArgs : EventArgs
        {
            public bool Continue;
        }

        protected ChainedEvent<TestEventArgs> eventUnderTest;

        [SetUp]
        public void TestSetup()
        {
            eventUnderTest = new ChainedEvent<TestEventArgs>();
        }

        [Test]
        public void FireEventWithZeroHandlersDoesNotThrowTest()
        {
            // Arrange

            // Act

            // Assert
            Assert.DoesNotThrow(() =>
                                {
                                    eventUnderTest.Fire(this, new TestEventArgs()
                                    {
                                        Continue = true
                                    }, (args) => args.Continue == true);
                                });
        }

        [Test]
        public void FireEventWithOneHandlerDoesNotThrowTest()
        {
            // Arrange
            eventUnderTest.EventSinks += (sender, args) =>
                                         {
                                             args.Continue = true;
                                         };
            // Act

            // Assert
            Assert.DoesNotThrow(() =>
                                {
                                    eventUnderTest.Fire(this, new TestEventArgs()
                                    {
                                        Continue = true
                                    }, (args) => args.Continue == true);
                                });
        }

        [Test]
        public void FireEventWithMultipleHandlersTest()
        {
            // Arrange
            eventUnderTest.EventSinks += (sender, args) =>
                                         {
                                             args.Continue = true;
                                         };
            eventUnderTest.EventSinks += (sender, args) =>
                                         {
                                             args.Continue = true;
                                         };
            eventUnderTest.EventSinks += (sender, args) =>
                                         {
                                             args.Continue = true;
                                         };

            var argsUnderTest = new TestEventArgs()
            {
                Continue = true
            };
            // Act
            eventUnderTest.Fire(this, argsUnderTest, (args) => args.Continue == true);

            // Assert
            Assert.IsTrue(argsUnderTest.Continue);
        }

        [Test]
        public void FireEventWithMultipleHandlersDoesNotContinueAfterFirstHandlerTest()
        {
            string lastEventHandler = String.Empty;
            // Arrange
            eventUnderTest.EventSinks += (sender, args) =>
                                         {
                                             lastEventHandler = "EventHandlerOne";
                                             args.Continue = false;
                                         };
            eventUnderTest.EventSinks += (sender, args) =>
                                         {
                                             lastEventHandler = "EventHandlerTwo";
                                             args.Continue = true;
                                         };
            eventUnderTest.EventSinks += (sender, args) =>
                                         {
                                             lastEventHandler = "EventHandlerThree";
                                             args.Continue = true;
                                         };

            var argsUnderTest = new TestEventArgs()
            {
                Continue = true
            };
            // Act
            eventUnderTest.Fire(this, argsUnderTest, (args) => args.Continue == true);

            // Assert
            Assert.IsTrue(argsUnderTest.Continue == false && lastEventHandler == "EventHandlerOne", "{0} returned {1}", lastEventHandler, argsUnderTest.Continue);
        }

        [Test]
        public void FireEventWithMultipleHandlersDoesNotContinueAfterSecondHandlerTest()
        {
            string lastEventHandler = String.Empty;
            // Arrange
            eventUnderTest.EventSinks += (sender, args) =>
                                         {
                                             lastEventHandler = "EventHandlerOne";
                                             args.Continue = true;
                                         };
            eventUnderTest.EventSinks += (sender, args) =>
                                         {
                                             lastEventHandler = "EventHandlerTwo";
                                             args.Continue = false;
                                         };
            eventUnderTest.EventSinks += (sender, args) =>
                                         {
                                             lastEventHandler = "EventHandlerThree";
                                             args.Continue = true;
                                         };

            var argsUnderTest = new TestEventArgs()
            {
                Continue = true
            };
            // Act
            eventUnderTest.Fire(this, argsUnderTest, (args) => args.Continue == true);

            // Assert
            Assert.IsTrue(argsUnderTest.Continue == false && lastEventHandler == "EventHandlerTwo", "{0} returned {1}", lastEventHandler, argsUnderTest.Continue);
        }

        [Test]
        public void FireEventWithMultipleHandlersDoesNotContinueAfterThirdHandlerTest()
        {
            string lastEventHandler = String.Empty;
            // Arrange
            eventUnderTest.EventSinks += (sender, args) =>
                                         {
                                             lastEventHandler = "EventHandlerOne";
                                             args.Continue = true;
                                         };
            eventUnderTest.EventSinks += (sender, args) =>
                                         {
                                             lastEventHandler = "EventHandlerTwo";
                                             args.Continue = true;
                                         };
            eventUnderTest.EventSinks += (sender, args) =>
                                         {
                                             lastEventHandler = "EventHandlerThree";
                                             args.Continue = false;
                                         };
            eventUnderTest.EventSinks += (sender, args) =>
                                         {
                                             lastEventHandler = "EventHandlerFour";
                                             args.Continue = true;
                                         };

            var argsUnderTest = new TestEventArgs()
            {
                Continue = true
            };
            // Act
            eventUnderTest.Fire(this, argsUnderTest, (args) => args.Continue == true);

            // Assert
            Assert.IsTrue(argsUnderTest.Continue == false && lastEventHandler == "EventHandlerThree", "{0} returned {1}", lastEventHandler, argsUnderTest.Continue);
        }
    }
}
