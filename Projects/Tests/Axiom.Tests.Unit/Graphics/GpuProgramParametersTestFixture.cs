using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Axiom.Graphics;
using Axiom.Math;

using Gallio.Framework;
using MbUnit.Framework;
using MbUnit.Framework.ContractVerifiers;

namespace Axiom.UnitTests.Graphics
{
    [TestFixture]
    public class GpuProgramParametersTestFixture
    {

        [Test]
        public void SetConstantFloat()
        {
            float[] expected = new[] { (float)Utility.SymmetricRandom(), 0f, 0f, 0f };
            float[] actual;

            GpuProgramParameters parameters = new GpuProgramParameters();
            //var floatLogical = new GpuLogicalBufferStruct();
            //parameters._setLogicalIndexes( floatLogical, new GpuLogicalBufferStruct() );
            parameters.SetConstant( 0, expected[ 0 ] );

            Assert.IsTrue( parameters.GetFloatConstant( 0 ).isSet );
            actual = parameters.GetFloatConstant( 0 ).val;
            Assert.AreEqual( expected, actual );
        }

        [Test]
        public void SetConstantFloat4()
        {
            float[] expected = new[] { (float)Utility.SymmetricRandom(), (float)Utility.SymmetricRandom(), (float)Utility.SymmetricRandom(), (float)Utility.SymmetricRandom() };
            float[] actual;

            GpuProgramParameters parameters = new GpuProgramParameters();
            //var floatLogical = new GpuLogicalBufferStruct();
            //parameters._setLogicalIndexes( floatLogical, new GpuLogicalBufferStruct() );
            parameters.SetConstant( 0, expected[ 0 ], expected[ 1 ], expected[ 2 ], expected[ 3 ] );

            Assert.IsTrue( parameters.GetFloatConstant( 0 ).isSet );
            actual = parameters.GetFloatConstant( 0 ).val;
            Assert.AreEqual( expected, actual );
        }

        [Test]
        public void SetConstantFloatArray()
        {
            float[] expected = new[] { (float)Utility.SymmetricRandom(), (float)Utility.SymmetricRandom(), (float)Utility.SymmetricRandom(), (float)Utility.SymmetricRandom() };
            float[] actual;

            GpuProgramParameters parameters = new GpuProgramParameters();
            //var floatLogical = new GpuLogicalBufferStruct();
            //parameters._setLogicalIndexes( floatLogical, new GpuLogicalBufferStruct() );
            parameters.SetConstant( 0, expected );

            Assert.IsTrue( parameters.GetFloatConstant( 0 ).isSet );
            actual = parameters.GetFloatConstant( 0 ).val;
            Assert.AreEqual( expected, actual );
        }

        [Test]
        public void SetConstantVector3()
        {
            float[] expected = new[] { (float)Utility.SymmetricRandom(), (float)Utility.SymmetricRandom(), (float)Utility.SymmetricRandom(), 1.0f };

            float[] actual;

            GpuProgramParameters parameters = new GpuProgramParameters();
            //var floatLogical = new GpuLogicalBufferStruct();
            //parameters._setLogicalIndexes( floatLogical, new GpuLogicalBufferStruct() );
            parameters.SetConstant( 0, new Vector3( expected[ 0 ], expected[ 1 ], expected[ 2 ] ) );

            Assert.IsTrue( parameters.GetFloatConstant( 0 ).isSet );
            actual = parameters.GetFloatConstant( 0 ).val;
            Assert.AreEqual( expected, actual );
        }

        [Test]
        public void SetConstantVector4()
        {
            float[] expected = new[] { (float)Utility.SymmetricRandom(), (float)Utility.SymmetricRandom(), (float)Utility.SymmetricRandom(), (float)Utility.SymmetricRandom() };
            float[] actual;

            GpuProgramParameters parameters = new GpuProgramParameters();
            //var floatLogical = new GpuLogicalBufferStruct();
            //parameters._setLogicalIndexes( floatLogical, new GpuLogicalBufferStruct() );
            parameters.SetConstant( 0, new Vector4( expected[ 0 ], expected[ 1 ], expected[ 2 ], expected[ 3 ] ) );

            Assert.IsTrue( parameters.GetFloatConstant( 0 ).isSet );
            actual = parameters.GetFloatConstant( 0 ).val;
            Assert.AreEqual( expected, actual );
        }

        [Test]
        public void SetConstantMatrix4()
        {
            float[] expected = new[] {  (float)Utility.SymmetricRandom(), (float)Utility.SymmetricRandom(), (float)Utility.SymmetricRandom(), (float)Utility.SymmetricRandom(),
                                        (float)Utility.SymmetricRandom(), (float)Utility.SymmetricRandom(), (float)Utility.SymmetricRandom(), (float)Utility.SymmetricRandom(),
                                        (float)Utility.SymmetricRandom(), (float)Utility.SymmetricRandom(), (float)Utility.SymmetricRandom(), (float)Utility.SymmetricRandom(),
                                        (float)Utility.SymmetricRandom(), (float)Utility.SymmetricRandom(), (float)Utility.SymmetricRandom(), (float)Utility.SymmetricRandom() };

            float[] actual = new float[16];

            GpuProgramParameters parameters = new GpuProgramParameters();
            //var floatLogical = new GpuLogicalBufferStruct();
            //parameters._setLogicalIndexes( floatLogical, new GpuLogicalBufferStruct() );
            parameters.SetConstant( 0, new Matrix4( expected[ 0 ],  expected[ 1 ],  expected[ 2 ],  expected[ 3 ],
                                                    expected[ 4 ],  expected[ 5 ],  expected[ 6 ],  expected[ 7 ],
                                                    expected[ 8 ],  expected[ 9 ],  expected[ 10 ], expected[ 11 ],
                                                    expected[ 12 ], expected[ 13 ], expected[ 14 ], expected[ 15 ] ) );

            GpuProgramParameters.FloatConstantEntry fcEntry;
            for (int i = 0; i < 4; i++)
            {
                fcEntry = parameters.GetFloatConstant(i);
                Assert.IsTrue(fcEntry.isSet);

                fcEntry.val.CopyTo(actual, i * 4);
            }

            Assert.AreEqual( expected, actual );
        }
    }
}
