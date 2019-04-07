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
using Axiom.Math;
using NUnit.Framework;

#endregion

namespace Axiom.Engine.Tests.Math
{
    [TestFixture]
    public class AxisAlignedBoxTests
    {

        [Test]
        public void TestMergePoint()
        {
            AxisAlignedBox actual = new AxisAlignedBox( new Vector3(0,0,0), new Vector3(50,50,50));
            AxisAlignedBox expected = new AxisAlignedBox(new Vector3(0, 0, 0), new Vector3(150, 150, 150));

            Vector3 point = new Vector3(150, 150, 150);

            actual.Merge(point);

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void TestMergeAABB()
        {
            AxisAlignedBox[] boxA = {
                                        new AxisAlignedBox( new Vector3( -500.00000f, 0.00000000f, 500.00000f ),
                                                            new Vector3( -499.00000f, 1.0000000f, 501.00000f ) ),
                                    };
            AxisAlignedBox[] boxB = {
                                        new AxisAlignedBox( new Vector3( -0.50000000f, -0.50000000f, -0.50000000f ),
                                                            new Vector3( 0.50000000f, 0.50000000f, 0.50000000f ) ),
                                    };

            AxisAlignedBox[] expected = {
                                            new AxisAlignedBox( new Vector3( -500.00000f, -0.50000000f, -0.50000000f ),
                                                                new Vector3( 0.50000000f, 1.0000000f, 501.00000f ) ),
                                        };

            AxisAlignedBox actual;

            for ( int index = 0; index < boxA.Length; index++ )
            {
                actual = boxA[ index ];
                actual.Merge( boxB[ index ] );

                Assert.AreEqual( expected[ index ], actual );
            }
        }

    }
}
