using System;
using System.Collections.Generic;
using System.Text;

using MbUnit.Framework;

using Axiom.Math;

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
