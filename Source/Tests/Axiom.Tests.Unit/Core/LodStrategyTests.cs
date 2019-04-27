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
using Axiom.Core.Collections;
using Axiom.Math;
using NUnit.Framework;

#endregion

namespace Axiom.UnitTests.Core
{
    [TestFixture]
    public class LodStrategyTests
    {
        private class LodStategyAscending : LodStrategy
        {

            public LodStategyAscending()
                : base("LodStrategyAscending")
            {
            }

            public override Axiom.Math.Real BaseValue
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public override Axiom.Math.Real TransformBias(Axiom.Math.Real factor)
            {
                throw new NotImplementedException();
            }

            public override ushort GetIndex(Axiom.Math.Real value, MeshLodUsageList meshLodUsageList)
            {
                throw new NotImplementedException();
            }

            public override ushort GetIndex(Axiom.Math.Real value, LodValueList materialLodValueList)
            {
                return GetIndexAscending(value, materialLodValueList);
            }

            public override void Sort(MeshLodUsageList meshLodUsageList)
            {
                throw new NotImplementedException();
            }

            public override bool IsSorted(LodValueList values)
            {
                throw new NotImplementedException();
            }

            protected override Axiom.Math.Real getValue(MovableObject movableObject, Camera camera)
            {
                throw new NotImplementedException();
            }
        }

        private class LodStategyDescending : LodStrategy
        {

            public LodStategyDescending()
                : base("LodStategyDescending")
            {
            }

            public override Axiom.Math.Real BaseValue
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public override Axiom.Math.Real TransformBias(Axiom.Math.Real factor)
            {
                throw new NotImplementedException();
            }

            public override ushort GetIndex(Axiom.Math.Real value, MeshLodUsageList meshLodUsageList)
            {
                throw new NotImplementedException();
            }

            public override ushort GetIndex(Axiom.Math.Real value, LodValueList materialLodValueList)
            {
                return GetIndexDescending(value, materialLodValueList);
            }

            public override void Sort(MeshLodUsageList meshLodUsageList)
            {
                throw new NotImplementedException();
            }

            public override bool IsSorted(LodValueList values)
            {
                throw new NotImplementedException();
            }

            protected override Axiom.Math.Real getValue(MovableObject movableObject, Camera camera)
            {
                throw new NotImplementedException();
            }
        }

        [Test]
        public void GetIndexAscendingTest()
        {
            var materialLodUsageList = new LodValueList()
                                       {
                                           new Real( 0.0f ), new Real( 1.0f ), new Real( 2.0f ), new Real( 3.0f )
                                       };
            var lodStrategy = new LodStategyAscending();

            for (int expected = 0; expected < materialLodUsageList.Count; expected++)
            {
                int actual = lodStrategy.GetIndex(0.5 + expected, materialLodUsageList);
                Assert.AreEqual(expected, actual);
            }
        }

        [Test]
        public void GetIndexDescendingTest()
        {
            var materialLodUsageList = new LodValueList()
                                       {
                                           new Real( 3.0f ), new Real( 2.0f ), new Real( 1.0f ), new Real( 0.0f )
                                       };
            var lodStrategy = new LodStategyDescending();

            for (int expected = 0; expected < materialLodUsageList.Count; expected++)
            {
                int actual = lodStrategy.GetIndex(3.0f - expected, materialLodUsageList);
                Assert.AreEqual(expected, actual);
            }
        }

    }
}
