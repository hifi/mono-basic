' ByteTypeTest.cs - NUnit Test Cases for Microsoft.VisualBasic.CompilerServices.ByteType
'
' Rolf Bjarne Kvinge (RKvinge@novell.com)
'
' 
' Copyright (C) 2008 Novell, Inc (http:www.novell.com)
'
' Permission is hereby granted, free of charge, to any person obtaining
' a copy of this software and associated documentation files (the
' "Software"), to deal in the Software without restriction, including
' without limitation the rights to use, copy, modify, merge, publish,
' distribute, sublicense, and/or sell copies of the Software, and to
' permit persons to whom the Software is furnished to do so, subject to
' the following conditions:
' 
' The above copyright notice and this permission notice shall be
' included in all copies or substantial portions of the Software.
' 
' THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
' EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
' MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
' NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
' LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
' OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
' WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
'
Imports NUnit.Framework
Imports System
Imports System.IO
Imports Microsoft.VisualBasic


Namespace CompilerServices
    <TestFixture()> _
    Public Class ByteTypeTest
        Sub New()
            Helper.SetThreadCulture()
        End Sub
        <Test()> _
        Public Sub FromString()
            Try
                Microsoft.VisualBasic.CompilerServices.ByteType.FromString("#ERROR")
                Assert.Fail("Expected InvalidCastException", "#A1")
            Catch ex As InvalidCastException
#If NET_VER >= 2.0 Then
                Assert.AreEqual("Conversion from string ""#ERROR"" to type 'Byte' is not valid.", ex.Message, "#A2")
#Else
                Assert.AreEqual("Cast from string ""#ERROR"" to type 'Byte' is not valid.", ex.Message, "#A2")
#End If
                Assert.IsNotNull(ex.InnerException, "#A3")
                Assert.AreSame(GetType(FormatException), ex.InnerException.GetType, "#A4")
            End Try

            Assert.AreEqual(0, Microsoft.VisualBasic.CompilerServices.ByteType.FromString(Nothing), "#B0")
        End Sub
    End Class
End Namespace