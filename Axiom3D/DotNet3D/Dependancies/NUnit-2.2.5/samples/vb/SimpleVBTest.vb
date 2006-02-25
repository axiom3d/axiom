'************************************************************************************
'
' Copyright © 2002 James W. Newkirk, Michael C. Two, Alexei A. Vorontsov
' Copyright © 2000-2002 Philip A. Craig
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
' Portions Copyright © 2002 James W. Newkirk, Michael C. Two, Alexei A. Vorontsov 
' or Copyright © 2000-2002 Philip A. Craig
'
' 2. Altered source versions must be plainly marked as such, and must not be 
' misrepresented as being the original software.
'
' 3. This notice may not be removed or altered from any source distribution.
'
'***********************************************************************************/

Option Explicit On 
Imports System
Imports NUnit.Framework

Namespace NUnit.Samples

    <TestFixture()> Public Class SimpleVBTest

        Private fValue1 As Integer
        Private fValue2 As Integer

        Public Sub New()
            MyBase.New()
        End Sub

        <SetUp()> Public Sub Init()
            fValue1 = 2
            fValue2 = 3
        End Sub

        <Test()> Public Sub Add()
            Dim result As Double

            result = fValue1 + fValue2
            Assert.AreEqual(6, result)
        End Sub

        <Test()> Public Sub DivideByZero()
            Dim zero As Integer
            Dim result As Integer

            zero = 0
            result = 8 / zero
        End Sub

        <Test()> Public Sub TestEquals()
            Assert.AreEqual(12, 12)
            Assert.AreEqual(CLng(12), CLng(12))

            Assert.AreEqual(12, 13, "Size")
            Assert.AreEqual(12, 11.99, 0, "Capacity")
        End Sub

        <Test(), ExpectedException(GetType(Exception))> Public Sub ExpectAnException()
            Throw New InvalidCastException()
        End Sub

        <Test(), Ignore("sample ignore")> Public Sub IgnoredTest()
            ' does not matter what we type the test is not run
            Throw New ArgumentException()
        End Sub

    End Class
End Namespace