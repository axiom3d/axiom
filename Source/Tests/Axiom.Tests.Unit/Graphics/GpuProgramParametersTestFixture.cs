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
using Axiom.Graphics;
using Axiom.Math;
using NUnit.Framework;

#endregion

namespace Axiom.UnitTests.Graphics
{
    [TestFixture]
    public class GpuProgramParametersTestFixture
    {

        [Test]
        public void SetConstantFloat()
        {
            float[] expected = new[] { (float)Utility.SymmetricRandom(), 0f, 0f, 0f };
            float actual;

            GpuProgramParameters parameters = new GpuProgramParameters();
            //var floatLogical = new GpuLogicalBufferStruct();
            //parameters._setLogicalIndexes( floatLogical, new GpuLogicalBufferStruct() );
            parameters.SetConstant(0, expected[0]);

            Assert.IsTrue(parameters.GetFloatConstant(0) != 0);
            actual = parameters.GetFloatConstant(0);
            Assert.AreEqual(expected[0], actual);
        }

        [Test]
        public void SetConstantFloat4()
        {
            float[] expected = new[] { (float)Utility.SymmetricRandom(), (float)Utility.SymmetricRandom(), (float)Utility.SymmetricRandom(), (float)Utility.SymmetricRandom() };
            float[] actual = new float[4];

            GpuProgramParameters parameters = new GpuProgramParameters();
            //var floatLogical = new GpuLogicalBufferStruct();
            //parameters._setLogicalIndexes( floatLogical, new GpuLogicalBufferStruct() );
            parameters.SetConstant(0, expected[0], expected[1], expected[2], expected[3]);

            for (int i = 0; i < 4; i++)
            {
                float fcEntry = parameters.GetFloatConstant(i);
                Assert.IsTrue(fcEntry != 0);

                actual[i * 4] = fcEntry;
            }

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void SetConstantFloatArray()
        {
            float[] expected = new[] { (float)Utility.SymmetricRandom(), (float)Utility.SymmetricRandom(), (float)Utility.SymmetricRandom(), (float)Utility.SymmetricRandom() };
            float[] actual = new float[4];

            GpuProgramParameters parameters = new GpuProgramParameters();
            //var floatLogical = new GpuLogicalBufferStruct();
            //parameters._setLogicalIndexes( floatLogical, new GpuLogicalBufferStruct() );
            parameters.SetConstant(0, expected);

            for (int i = 0; i < 4; i++)
            {
                float fcEntry = parameters.GetFloatConstant(i);
                Assert.IsTrue(fcEntry != 0);

                actual[i * 4] = fcEntry;
            }

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void SetConstantVector3()
        {
            float[] expected = new[] { (float)Utility.SymmetricRandom(), (float)Utility.SymmetricRandom(), (float)Utility.SymmetricRandom(), 1.0f };

            float[] actual = new float[3];

            GpuProgramParameters parameters = new GpuProgramParameters();
            //var floatLogical = new GpuLogicalBufferStruct();
            //parameters._setLogicalIndexes( floatLogical, new GpuLogicalBufferStruct() );
            parameters.SetConstant(0, new Vector3(expected[0], expected[1], expected[2]));

            for (int i = 0; i < 3; i++)
            {
                float fcEntry = parameters.GetFloatConstant(i);
                Assert.IsTrue(fcEntry != 0);

                actual[i * 4] = fcEntry;
            }

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void SetConstantVector4()
        {
            float[] expected = new[] { (float)Utility.SymmetricRandom(), (float)Utility.SymmetricRandom(), (float)Utility.SymmetricRandom(), (float)Utility.SymmetricRandom() };
            float[] actual = new float[4];

            GpuProgramParameters parameters = new GpuProgramParameters();
            //var floatLogical = new GpuLogicalBufferStruct();
            //parameters._setLogicalIndexes( floatLogical, new GpuLogicalBufferStruct() );
            parameters.SetConstant(0, new Vector4(expected[0], expected[1], expected[2], expected[3]));

            for (int i = 0; i < 4; i++)
            {
                float fcEntry = parameters.GetFloatConstant(i);
                Assert.IsTrue(fcEntry != 0);

                actual[i * 4] = fcEntry;
            }

            Assert.AreEqual(expected, actual);
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
            parameters.SetConstant(0, new Matrix4(expected[0], expected[1], expected[2], expected[3],
                                                    expected[4], expected[5], expected[6], expected[7],
                                                    expected[8], expected[9], expected[10], expected[11],
                                                    expected[12], expected[13], expected[14], expected[15]));

            for (int i = 0; i < 4; i++)
            {
                float fcEntry = parameters.GetFloatConstant(i);
                Assert.IsTrue(fcEntry != 0);

                actual[i * 4] = fcEntry;
            }

            Assert.AreEqual(expected, actual);
        }
    }
}
