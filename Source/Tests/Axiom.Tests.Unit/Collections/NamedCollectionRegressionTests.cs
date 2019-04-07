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

namespace Axiom.UnitTests.Collections
{
    /// <summary>
    /// Regression tests for the <see cref="NamedCollection{T}"/> class.
    /// </summary>
    [ TestFixture ]
    public class NamedCollectionRegressionTests
    {
        /// <summary>
        /// Verifies that the collection does not throw an exception when told to remove an invalid item by key
        /// but instead simply returns false.
        /// </summary>
        [ Test ]
        public void TestRemoveInvalidKey()
        {
            StubCollection testedCollection = new StubCollection();

            bool foundAndRemoved = testedCollection.Remove( "InvalidKey" );

            Assert.IsFalse( foundAndRemoved, "The Remove(key) method returned true although an invalid key was passed." );
        }

        /// <summary>
        /// Verifies that the collection does not throw an exception when told to remove an invalid item by reference
        /// but instead simply returns false.
        /// </summary>
        [ Test ]
        public void TestRemoveInvalidItem()
        {
            StubCollection testedCollection = new StubCollection();

            bool foundAndRemoved = testedCollection.Remove( new TestObject() );

            Assert.IsFalse( foundAndRemoved, "The Remove(item) method returned true although an invalid item was passed." );
        }

        /// <summary>
        /// Verifies that the collection does throw an ArgumentOutOfRangeException when told to remove an item specifying an invalid index.
        /// </summary>
        [ Test ]
        public void TestRemoveInvalidIndex()
        {
            StubCollection testedCollection = new StubCollection();

            Assert.Throws<ArgumentOutOfRangeException>(() => testedCollection.RemoveAt( 0 ));

            Assert.Fail( "The RemoveAt(index) did not throw an Exception although an invalid index was passed." );
        }

        private class StubCollection : NamedCollection<TestObject>
        {
        }

        private class TestObject : INamable
        {
            public string Name
            {
                get { return Guid.NewGuid().ToString(); }
            }
        }
    }
}
