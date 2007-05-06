' 
' Visual Basic.Net Compiler
' Copyright (C) 2004 - 2007 Rolf Bjarne Kvinge, RKvinge@novell.com
' 
' This library is free software; you can redistribute it and/or
' modify it under the terms of the GNU Lesser General Public
' License as published by the Free Software Foundation; either
' version 2.1 of the License, or (at your option) any later version.
' 
' This library is distributed in the hope that it will be useful,
' but WITHOUT ANY WARRANTY; without even the implied warranty of
' MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
' Lesser General Public License for more details.
' 
' You should have received a copy of the GNU Lesser General Public
' License along with this library; if not, write to the Free Software
' Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
' 

''' <summary>
''' The expression on the left side of the assignment operator must be classified as a variable or a property access, 
''' while the expression on the right side of the assignment operator must be classified as a value
''' The type of the expression must be implicitly convertible to the type of the variable or property access. 
''' </summary>
''' <remarks></remarks>
Public Class AssignmentStatement
    Inherits Statement

    Private m_LSide As Expression
    Private m_RSide As Expression

    Public Overrides Function ResolveTypeReferences() As Boolean
        Dim result As Boolean = True

        result = m_LSide.ResolveTypeReferences AndAlso result
        result = m_RSide.ResolveTypeReferences AndAlso result

        Return result
    End Function

    Sub New(ByVal Parent As ParsedObject)
        MyBase.new(Parent)
    End Sub

    Sub Init(ByVal LSide As Expression, ByVal RSide As Expression)
        m_LSide = LSide
        m_RSide = RSide
    End Sub

    ReadOnly Property LSide() As Expression
        Get
            Return m_LSide
        End Get
    End Property

    Property RSide() As Expression
        Get
            Return m_RSide
        End Get
        Set(ByVal value As Expression)
            m_RSide = value
        End Set
    End Property

    Friend Overrides Function GenerateCode(ByVal Info As EmitInfo) As Boolean
        Dim result As Boolean = True

        Dim lInfo As EmitInfo = Info.Clone(RSide)

        Helper.Assert(LSide.Classification.IsVariableClassification OrElse LSide.Classification.IsPropertyAccessClassification)
        result = LSide.Classification.GenerateCode(lInfo) AndAlso result

        Return result
    End Function

    Public Overrides Function ResolveStatement(ByVal Info As ResolveInfo) As Boolean
        Dim result As Boolean = True

        result = m_LSide.ResolveExpression(Info) AndAlso result
        result = m_RSide.ResolveExpression(Info) AndAlso result

        If result = False Then Return result

        If RSide.Classification.IsValueClassification Then
            'do nothing
        ElseIf RSide.Classification.IsMethodPointerClassification Then
            ''result = RSide.ResolveAddressOfExpression(m_LSide.ExpressionType) AndAlso result
            'If result Then
            m_RSide = m_RSide.ReclassifyMethodPointerToValueExpression(m_LSide.ExpressionType)
            result = m_RSide.ResolveExpression(Info) AndAlso result
            'End If
        ElseIf RSide.Classification.CanBeValueClassification Then
            RSide = RSide.ReclassifyToValueExpression()
            result = RSide.ResolveExpression(ResolveInfo.Default(Info.Compiler)) AndAlso result
            'RSide.Classification = RSide.Classification.ReclassifyToValue
        Else
            Helper.AddError("Right hand side expression must be classifiable as a value.")
        End If

        If LSide.Classification.IsVariableClassification OrElse LSide.Classification.IsPropertyAccessClassification Then
            'do nothing
        ElseIf LSide.Classification.CanBePropertyAccessClassification Then
            m_LSide = LSide.ReclassifyToPropertyAccessExpression
            result = LSide.ResolveExpression(ResolveInfo.Default(Info.Compiler)) AndAlso result
            If result = False Then
                Helper.AddError()
                Return result
            End If
        Else
            Helper.AddError("Left hand side expression must be classifiable as a variable or a property access.")
        End If

        If m_LSide.ExpressionType.IsGenericType AndAlso Helper.CompareType(Compiler.TypeCache.System_Nullable, m_LSide.ExpressionType.GetGenericTypeDefinition) Then
            Dim lTypeArg As Type()
            lTypeArg = m_LSide.ExpressionType.GetGenericArguments
            If lTypeArg.Length = 1 AndAlso Helper.CompareType(lTypeArg(0), m_RSide.ExpressionType) Then
                Dim objCreation As DelegateOrObjectCreationExpression
                objCreation = New DelegateOrObjectCreationExpression(Me)
                objCreation.Init(m_LSide.ExpressionType, New ArgumentList(objCreation, m_RSide))
                result = objCreation.ResolveExpression(ResolveInfo.Default(Info.Compiler)) AndAlso result
                m_RSide = objCreation
            End If
        End If

        result = CreateTypeConversion() AndAlso result

        Return result
    End Function

    Overridable Function CreateTypeConversion() As Boolean
        Dim result As Boolean = True

        m_RSide = Helper.CreateTypeConversion(Me, m_RSide, m_LSide.ExpressionType, result)

        Return result
    End Function

#If DEBUG Then
    Overridable ReadOnly Property AssignmentType() As KS
        Get
            Return KS.Equals
        End Get
    End Property
    Public Sub Dump(ByVal Dumper As IndentedTextWriter)
        m_LSide.Dump(Dumper)
        Dumper.Write(" " & Enums.GetKSStringAttribute(AssignmentType).FriendlyValue & " ")
        m_RSide.Dump(Dumper)
        Dumper.WriteLine("")
    End Sub
#End If

End Class
