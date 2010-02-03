using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Axiom.Core;

using Gallio.Framework;
using MbUnit.Framework;
using MbUnit.Framework.ContractVerifiers;

namespace Axiom.UnitTests.Core
{
    [TestFixture]
    public class ResourceGroupManagerTests
    {
        [Test]
        public void CreateResource()
        {
            new Root( "AxiomTests.log" );
            ResourceGroupManager.Instance.AddResourceLocation( ".", "Folder" );
            Stream io = ResourceGroupManager.Instance.CreateResource( "CreateResource.Test", ResourceGroupManager.DefaultResourceGroupName, true, "." );
            Assert.IsNotNull( io );
            io.Close();
        }
    }
}
