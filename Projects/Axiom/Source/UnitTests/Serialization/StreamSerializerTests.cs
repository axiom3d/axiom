#define MbUnit

using System;
using System.Collections.Generic;
using System.Text;

#if MbUnit
using Gallio.Framework;
using MbUnit.Framework;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;

using TestFixture = Microsoft.VisualStudio.TestTools.UnitTesting.TestClassAttribute;
using Test = Microsoft.VisualStudio.TestTools.UnitTesting.TestMethodAttribute;
#endif

using Axiom.Math;

namespace Axiom.Engine.Tests.Serialization
{
    [TestFixture]
    public class StreamSerializerTests
    {

#if !MbUnit
        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }
#endif

        [Test]
        public void TestReadData()
        {
            AxisAlignedBox actual = new AxisAlignedBox(new Vector3(0, 0, 0), new Vector3(50, 50, 50));
            AxisAlignedBox expected = new AxisAlignedBox(new Vector3(0, 0, 0), new Vector3(150, 150, 150));

            Vector3 point = new Vector3(150, 150, 150);

            actual.Merge(point);

            Assert.AreEqual(expected, actual);
        }
    }
}
