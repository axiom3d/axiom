using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Axiom.Core;

using Gallio.Framework;
using MbUnit.Framework;
using MbUnit.Framework.ContractVerifiers;

namespace Axiom.UnitTests.Core
{
    [TestFixture]
    public class ChainedEventTests
    {
        protected class TestEventArgs: EventArgs
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
            Assert.DoesNotThrow( () =>
                                 {
                                     eventUnderTest.Fire( this, new TestEventArgs()
                                                                {
                                                                    Continue = true
                                                                }, ( args ) => args.Continue == true );
                                 } );
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
            Assert.DoesNotThrow( () =>
                                 {
                                     eventUnderTest.Fire( this, new TestEventArgs()
                                                                {
                                                                    Continue = true
                                                                }, ( args ) => args.Continue == true );
                                 } );
        }

        [Test]
        public void FireEventWithMultipleHandlersTest()
        {
            // Arrange
            eventUnderTest.EventSinks += ( sender, args ) =>
                                         {
                                             args.Continue = true;
                                         };
            eventUnderTest.EventSinks += ( sender, args ) =>
                                         {
                                             args.Continue = true;
                                         };
            eventUnderTest.EventSinks += ( sender, args ) =>
                                         {
                                             args.Continue = true;
                                         };

            var argsUnderTest = new TestEventArgs()
                                {
                                    Continue = true
                                };
            // Act
            eventUnderTest.Fire( this, argsUnderTest, ( args ) => args.Continue == true );

            // Assert
            Assert.IsTrue( argsUnderTest.Continue );
        }

        [Test]
        public void FireEventWithMultipleHandlersDoesNotContinueAfterFirstHandlerTest()
        {
            string lastEventHandler = String.Empty;
            // Arrange
            eventUnderTest.EventSinks += ( sender, args ) =>
                                         {
                                             lastEventHandler = "EventHandlerOne";
                                             args.Continue = false;
                                         };
            eventUnderTest.EventSinks += ( sender, args ) =>
                                         {
                                             lastEventHandler = "EventHandlerTwo";
                                             args.Continue = true;
                                         };
            eventUnderTest.EventSinks += ( sender, args ) =>
                                         {
                                             lastEventHandler = "EventHandlerThree";
                                             args.Continue = true;
                                         };

            var argsUnderTest = new TestEventArgs()
            {
                Continue = true
            };
            // Act
            eventUnderTest.Fire( this, argsUnderTest, ( args ) => args.Continue == true );

            // Assert
            Assert.IsTrue( argsUnderTest.Continue == false && lastEventHandler == "EventHandlerOne", "{0} returned {1}", lastEventHandler, argsUnderTest.Continue );
        }

        [Test]
        public void FireEventWithMultipleHandlersDoesNotContinueAfterSecondHandlerTest()
        {
            string lastEventHandler = String.Empty;
            // Arrange
            eventUnderTest.EventSinks += ( sender, args ) =>
                                         {
                                             lastEventHandler = "EventHandlerOne";
                                             args.Continue = true;
                                         };
            eventUnderTest.EventSinks += ( sender, args ) =>
                                         {
                                             lastEventHandler = "EventHandlerTwo";
                                             args.Continue = false;
                                         };
            eventUnderTest.EventSinks += ( sender, args ) =>
                                         {
                                             lastEventHandler = "EventHandlerThree";
                                             args.Continue = true;
                                         };

            var argsUnderTest = new TestEventArgs()
            {
                Continue = true
            };
            // Act
            eventUnderTest.Fire( this, argsUnderTest, ( args ) => args.Continue == true );

            // Assert
            Assert.IsTrue( argsUnderTest.Continue == false && lastEventHandler == "EventHandlerTwo", "{0} returned {1}", lastEventHandler, argsUnderTest.Continue );
        }

        [Test]
        public void FireEventWithMultipleHandlersDoesNotContinueAfterThirdHandlerTest()
        {
            string lastEventHandler = String.Empty;
            // Arrange
            eventUnderTest.EventSinks += ( sender, args ) =>
                                         {
                                             lastEventHandler = "EventHandlerOne";
                                             args.Continue = true;
                                         };
            eventUnderTest.EventSinks += ( sender, args ) =>
                                         {
                                             lastEventHandler = "EventHandlerTwo";
                                             args.Continue = true;
                                         };
            eventUnderTest.EventSinks += ( sender, args ) =>
                                         {
                                             lastEventHandler = "EventHandlerThree";
                                             args.Continue = false;
                                         };
            eventUnderTest.EventSinks += ( sender, args ) =>
                                         {
                                             lastEventHandler = "EventHandlerFour";
                                             args.Continue = true;
                                         };

            var argsUnderTest = new TestEventArgs()
                                {
                                    Continue = true
                                };
            // Act
            eventUnderTest.Fire( this, argsUnderTest, ( args ) => args.Continue == true );

            // Assert
            Assert.IsTrue( argsUnderTest.Continue == false && lastEventHandler == "EventHandlerThree", "{0} returned {1}", lastEventHandler, argsUnderTest.Continue );
        }
    }
}
