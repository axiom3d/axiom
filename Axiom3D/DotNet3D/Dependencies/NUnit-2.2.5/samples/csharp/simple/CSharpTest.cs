#region Copyright (c) 2002, James W. Newkirk, Michael C. Two, Alexei A. Vorontsov, Philip A. Craig
/************************************************************************************
'
' Copyright  2002 James W. Newkirk, Michael C. Two, Alexei A. Vorontsov
' Copyright  2000-2002 Philip A. Craig
'
' This software is provided 'as-is', without any express or implied warranty. In no 
' event will the authors be held liable for any damages arising from the use of this 
' software.
' 
' Permission is granted to anyone to use this software for any purpose, including 
' commercial applications, and to alter it and redistribute it freely, subject to the 
' following restrictions:
'
' 1. The origin of this software must not be misrepresented; you must not claim that 
' you wrote the original software. If you use this software in a product, an 
' acknowledgment (see the following) in the product documentation is required.
'
' Portions Copyright  2002 James W. Newkirk, Michael C. Two, Alexei A. Vorontsov 
' or Copyright  2000-2002 Philip A. Craig
'
' 2. Altered source versions must be plainly marked as such, and must not be 
' misrepresented as being the original software.
'
' 3. This notice may not be removed or altered from any source distribution.
'
'***********************************************************************************/
#endregion

namespace NUnit.Samples 
{
	using System;
	using NUnit.Framework;

	/// <summary>Some simple Tests.</summary>
	/// 
	[TestFixture] 
	public class SimpleCSharpTest
	{
		/// <summary>
		/// 
		/// </summary>
		protected int fValue1;
		/// <summary>
		/// 
		/// </summary>
		protected int fValue2;
		
		/// <summary>
		/// 
		/// </summary>
		[SetUp] public void Init() 
		{
			fValue1= 2;
			fValue2= 3;
		}

		/// <summary>
		/// 
		/// </summary>
		///
		[Test] public void Add() 
		{
			double result= fValue1 + fValue2;
			// forced failure result == 5
			Assert.AreEqual(6, result, "Expected Failure.");
		}

		/// <summary>
		/// 
		/// </summary>
		/// 
		[Test] public void DivideByZero() 
		{
			int zero= 0;
			int result= 8/zero;
		}

		/// <summary>
		/// 
		/// </summary>
		/// 
		[Test] public void Equals() 
		{
			Assert.AreEqual(12, 12, "Integer");
			Assert.AreEqual(12L, 12L, "Long");
			Assert.AreEqual('a', 'a', "Char");
			Assert.AreEqual((object)12, (object)12, "Integer Object Cast");
            
			Assert.AreEqual(12, 13, "Expected Failure (Integer)");
			Assert.AreEqual(12.0, 11.99, 0.0, "Expected Failure (Double).");
		}

		[Test]
		[ExpectedException(typeof(InvalidOperationException))]
		public void ExpectAnException()
		{
			throw new InvalidCastException();
		}

		[Test]
		[Ignore("ignored test")]
		public void IgnoredTest()
		{
			throw new Exception();
		}
	}
}