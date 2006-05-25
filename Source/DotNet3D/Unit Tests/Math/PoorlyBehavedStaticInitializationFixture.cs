#region LGPL License
/*
 DotNet3D Library
 Copyright (C) 2006 DotNet3D Project Team
 
 The overall design, and a majority of the core engine and rendering code
 contained within this library is a derivative of the open source Object Oriented
 Graphics Engine OGRE, which can be found at http://ogre.sourceforge.net.
 Many thanks to the OGRE team for maintaining such a high quality project.
 
 This library is free software; you can redistribute it and/or
 modify it under the terms of the GNU Lesser General Public
 License as published by the Free Software Foundation; either
 version 2.1 of the License, or (at your option) any later version.
 
 This library is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
 Lesser General Public License for more details.
 
 You should have received a copy of the GNU Lesser General Public
 License along with this library; if not, write to the Free Software
 Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
 */
#endregion

#region Namespace Declarations

#if !NUNIT && !MBUNIT
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif

#if NUNIT
using NUnit.Framework;
using TestClass = NUnit.Framework.TestFixtureAttribute;
using TestInitialize = NUnit.Framework.SetUpAttribute;
using TestCleanup = NUnit.Framework.TearDownAttribute;
using TestMethod = NUnit.Framework.TestAttribute;
#endif

#if MBUNIT
using MbUnit.Framework;
using TestClass = MbUnit.Framework.TestFixtureAttribute;
using TestInitialize = MbUnit.Framework.SetUpAttribute;
using TestCleanup = MbUnit.Framework.TearDownAttribute;
using TestMethod = MbUnit.Framework.TestAttribute;
#endif

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

using DotNet3D.Math;

#endregion Namespace Declarations

namespace DotNet3D.Math.Tests
{
    /// <summary>
    /// This tests for the rather esoteric failure of certain DotNet3D.Math
	/// classes to initialize properly due to some wierdness with how static
	/// members are initialized.
	/// 
	/// Currently only Radian fails this test, and only when a static
	/// member of Utility (PI in this case) is accessed directly at some
	/// point.  It does not seem to matter *where* the Utility.PI statement
	/// is placed in the code, as long as it is present.
	/// Location in the code does seem to have an effect - look below for
	/// some examples.
	/// Once the error condition is triggered, it doesn't seem to matter how
	/// many places Utility.PI is accessed, which is not unexpected given
	/// we are dealing with static variables.
    ///</summary>
    [TestFixture]
    public class PoorlyBehavedStaticInitializationFixture
    {
		// Uncomment only this version of the statement to cause the test(s) to fail.
		Real pi = Utility.PI;
		
		/// <summary>
		/// _radiansToDegrees initializes as {Infinity} due to an error
		/// in how Utility.PI is initialized (all of Utility, actually).
		/// Utility.PI ends up being 0, which obviously causes problems in
		/// the following line of code:
		/// private static readonly Real _radiansToDegrees = (180.0f / Utility.PI);
		/// 
		/// This test should fail.
		/// </summary>
		[TestMethod]
		public void PoorlyBehavedRadianInitialization()
		{
			Radian r = new Radian(new Real(0.2342f));
			Real expectedInDegrees = new Real(13.41867);
			Real actualInDegrees = r.InDegrees;

			// Uncomment only this version of the statement to cause the test(s) to fail.
			//Real pi = Utility.PI;
			
			Assert.AreEqual(expectedInDegrees, actualInDegrees, "Radian.InDegrees did not return the expected value");
		}
		
		/// <summary>
		/// This is just left here to show that once the error condition occurs,
		/// *all* instances of Radian are broken, since we are dealing with a 
		/// static field.
		/// 
		/// This test should also fail
		/// </summary>
		[TestMethod]
		public void SonOfPoorlyBehavedRadianInitialization()
		{
			Radian r = new Radian(new Real(0.6746f));
			Real expectedInDegrees = new Real(38.65173);
			Real actualInDegrees = r.InDegrees;
			
			// Uncommenting only this copy of this statement does *not*
			// trigger the error condition
			//Real pi = Utility.PI;

			Assert.AreEqual(expectedInDegrees, actualInDegrees, "Radian.InDegrees did not return the expected value");
		}
				
		/// <summary>
		/// For reasons that are not clear, Degree is unaffected by the error condition.
		/// 
		/// This test should pass.
		/// </summary>
		[TestMethod]
		public void WellBehavedDegreeInitialization()
		{
			// Uncommenting only this copy of this statement does *not*
			// trigger the error condition
			//Real pi = Utility.PI;

			Degree d = new Degree(new Real(45.0f));
			Real expectedInRadians = new Real(0.7853982f);
			Real actualInRadians = d.InRadians;
			
			Assert.AreEqual(expectedInRadians, actualInRadians, "Degree.InRadians did not return the expected value");
		}
		
	}
}
