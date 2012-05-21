using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Axiom.Core;
using Axiom.Math;

using Gallio.Framework;
using MbUnit.Framework;
using MbUnit.Framework.ContractVerifiers;
using Axiom.Core.Collections;

namespace Axiom.UnitTests.Core
{
    [TestFixture]
    public class LodStrategyTests
    {
        private class LodStategyAscending : LodStrategy
        {

            public LodStategyAscending()
                : base( "LodStrategyAscending" )
            {
            }

            public override Axiom.Math.Real BaseValue
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public override Axiom.Math.Real TransformBias( Axiom.Math.Real factor )
            {
                throw new NotImplementedException();
            }

            public override ushort GetIndex( Axiom.Math.Real value, MeshLodUsageList meshLodUsageList )
            {
                throw new NotImplementedException();
            }

            public override ushort GetIndex( Axiom.Math.Real value, LodValueList materialLodValueList )
            {
                return GetIndexAscending( value, materialLodValueList );
            }

            public override void Sort( MeshLodUsageList meshLodUsageList )
            {
                throw new NotImplementedException();
            }

            public override bool IsSorted( LodValueList values )
            {
                throw new NotImplementedException();
            }

            protected override Axiom.Math.Real getValue( MovableObject movableObject, Camera camera )
            {
                throw new NotImplementedException();
            }
        }

        private class LodStategyDescending : LodStrategy
        {

            public LodStategyDescending()
                : base( "LodStategyDescending" )
            {
            }

            public override Axiom.Math.Real BaseValue
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public override Axiom.Math.Real TransformBias( Axiom.Math.Real factor )
            {
                throw new NotImplementedException();
            }

            public override ushort GetIndex( Axiom.Math.Real value, MeshLodUsageList meshLodUsageList )
            {
                throw new NotImplementedException();
            }

            public override ushort GetIndex( Axiom.Math.Real value, LodValueList materialLodValueList )
            {
                return GetIndexDescending( value, materialLodValueList );
            }

            public override void Sort( MeshLodUsageList meshLodUsageList )
            {
                throw new NotImplementedException();
            }

            public override bool IsSorted( LodValueList values )
            {
                throw new NotImplementedException();
            }

            protected override Axiom.Math.Real getValue( MovableObject movableObject, Camera camera )
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

            for ( int expected = 0; expected < materialLodUsageList.Count; expected++)
            {
                int actual = lodStrategy.GetIndex( 0.5 + expected, materialLodUsageList );
                Assert.AreEqual( expected, actual );
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

            for ( int expected = 0; expected < materialLodUsageList.Count; expected++ )
            {
                int actual = lodStrategy.GetIndex( 3.0f - expected, materialLodUsageList );
                Assert.AreEqual( expected, actual );
            }
        }
    
    }
}
