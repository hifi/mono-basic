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

#Const DEBUGPARSERESULT = True
#Const EXTENDEDDEBUG = 0

Public Class CodeBlock
    Inherits ParsedObject

    ''' <summary>
    ''' A list of all the variables in this code block.
    ''' This is just a cache, all the variables are also in m_Statements.
    ''' </summary>
    ''' <remarks></remarks>
    Private m_Variables As New Nameables(Of VariableDeclaration)(Me)
    Private m_StaticVariables As Generic.List(Of VariableDeclaration)

    ''' <summary>
    ''' A list of all the statements (expressions) in this code block.
    ''' </summary>
    ''' <remarks></remarks>
    Private m_Statements As New BaseObjects(Of Statement)(Compiler)
    Private m_BlockStatements As Generic.List(Of BlockStatement)
    Private m_Sequence As New BaseObjects(Of BaseObject)(Me)

    ''' <summary>
    ''' A list of all the labels in this code block.
    ''' </summary>
    ''' <remarks></remarks>
    Private m_Labels As New Generic.List(Of LabelDeclarationStatement)

    Private m_FirstStatement As Statement

    Private m_HasStructuredExceptionHandling As Boolean

    Private m_HasUnstructuredExceptionHandling As Boolean
    Private m_HasResume As Boolean

    Public EndUnstructuredExceptionHandler As Label
    ''' <summary>
    ''' This is the variable informing which handler should handle an exception.
    ''' </summary>
    ''' <remarks></remarks>
    Public UnstructuredExceptionHandlerVariable As LocalBuilder
    ''' <summary>
    ''' A value is stored here to check if the running code is in the unstructured handler
    ''' 0: not in the handler
    ''' -1: in the handler.
    ''' </summary>
    ''' <remarks></remarks>
    Public IsInUnstructuredHandler As LocalBuilder
    Public UnstructuredExceptionHandler As Label
    Public UnstructuredSwitchHandler As Label
    ''' <summary>
    ''' The end of the switch. The code here jumps to the end of the method.
    ''' </summary>
    ''' <remarks></remarks>
    Public UnstructuredSwitchHandlerEnd As Label

    ''' <summary>
    ''' The resume next exception handler. Index 1 of the switch table
    ''' </summary>
    ''' <remarks></remarks>
    Public ResumeNextExceptionHandler As Label
    ''' <summary>
    ''' The index into the jump table of the current instruction.
    ''' </summary>
    ''' <remarks></remarks>
    Public CurrentInstruction As LocalBuilder
    Public UnstructuredExceptionLabels As Generic.List(Of Label)
    Public EndMethodLabel As Label

    ''' <summary>
    ''' The location of the code that throws an internal exception.
    ''' </summary>
    ''' <remarks></remarks>
    Private m_InternalExceptionLocation As Label

    Private m_EndOfMethodLabel As Nullable(Of Label)

    ReadOnly Property BlockStatements() As Generic.List(Of BlockStatement)
        Get
            If m_BlockStatements Is Nothing Then
                m_BlockStatements = New Generic.List(Of BlockStatement)
                For Each stmt As Statement In m_Statements
                    Dim blockStmt As BlockStatement
                    blockStmt = TryCast(stmt, BlockStatement)
                    If blockStmt IsNot Nothing Then m_BlockStatements.Add(blockStmt)
                Next
            End If
            Return m_BlockStatements
        End Get
    End Property

    Sub FindStaticVariables(ByVal list As Generic.List(Of VariableDeclaration))
        If m_StaticVariables IsNot Nothing Then
            list.AddRange(m_StaticVariables)
            Return
        End If

        For Each var As VariableDeclaration In m_Variables
            If var.Modifiers.ContainsAny(KS.Static) Then
                list.Add(var)
            End If
        Next
        For Each stmt As BlockStatement In BlockStatements
            stmt.CodeBlock.FindStaticVariables(list)
        Next

        m_StaticVariables = list
    End Sub

    Sub New(ByVal Parent As ParsedObject)
        MyBase.New(Parent)
    End Sub

    Sub AddStatement(ByVal Statement As Statement)
        m_Statements.Add(Statement)
        m_Sequence.Add(Statement)
    End Sub

    Sub AddLabel(ByVal lbl As LabelDeclarationStatement)
        m_Labels.Add(lbl)
        Dim cb As CodeBlock = Me.FindFirstParent(Of CodeBlock)()
        If cb IsNot Nothing Then cb.AddLabel(lbl)
    End Sub

    Sub AddVariable(ByVal var As VariableDeclaration)
        m_Variables.Add(var)
        m_Sequence.Add(var)
    End Sub

    Sub AddVariables(ByVal list As Generic.ICollection(Of VariableDeclaration))
        For Each var As VariableDeclaration In list
            AddVariable(var)
        Next
    End Sub

    ''' <summary>
    ''' A label to just before the last ret instruction of the method.
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    ReadOnly Property EndOfMethodLabel() As Label
        Get
            If m_EndOfMethodLabel.HasValue Then
                Return m_EndOfMethodLabel.Value
            Else
                Helper.Assert(Me IsNot UpmostBlock)
                Return Me.UpmostBlock.EndOfMethodLabel
            End If
        End Get
    End Property

    Property FirstStatement() As Statement
        Get
            Return m_FirstStatement
        End Get
        Set(ByVal value As Statement)
            m_FirstStatement = value
        End Set
    End Property

    ReadOnly Property Statements() As Generic.List(Of Statement)
        Get
            Return m_Statements
        End Get
    End Property

    Sub RemoveStatement(ByVal Statement As Statement)
        m_Statements.Remove(Statement)
        m_Sequence.Remove(Statement)
    End Sub

    Property HasResume() As Boolean
        Get
            Return m_HasResume
        End Get
        Set(ByVal value As Boolean)
            Helper.Assert(value = True)
            m_HasResume = value
            Dim parent As CodeBlock = Me.FindFirstParent(Of CodeBlock)()
            If parent IsNot Nothing Then parent.HasResume = value
        End Set
    End Property

    Property HasUnstructuredExceptionHandling() As Boolean
        Get
            Return m_HasUnstructuredExceptionHandling
        End Get
        Set(ByVal value As Boolean)
            m_HasUnstructuredExceptionHandling = True
            Dim parent As CodeBlock = Me.FindFirstParent(Of CodeBlock)()
            If parent IsNot Nothing Then parent.HasUnstructuredExceptionHandling = value
        End Set
    End Property

    Property HasStructuredExceptionHandling() As Boolean
        Get
            Return m_HasStructuredExceptionHandling
        End Get
        Set(ByVal value As Boolean)
            m_HasStructuredExceptionHandling = True
            Dim parent As CodeBlock = Me.FindFirstParent(Of CodeBlock)()
            parent.HasStructuredExceptionHandling = value
        End Set
    End Property

    ReadOnly Property Labels() As Generic.List(Of LabelDeclarationStatement)
        Get
            Return m_Labels
        End Get
    End Property

    Function FindLabel(ByVal Name As Token) As LabelDeclarationStatement
        Dim cb As CodeBlock = Me.FindFirstParent(Of CodeBlock)()
        If cb IsNot Nothing Then
            Return cb.FindLabel(Name)
        Else
            For Each l As LabelDeclarationStatement In m_Labels
                If l.Label.Equals(Name) Then
                    Return l
                End If
            Next
        End If
        Return Nothing
    End Function

    Private Function GenerateUnstructuredStart(ByVal Info As EmitInfo) As Boolean
        Dim result As Boolean = True

        EndMethodLabel = Info.ILGen.DefineLabel
        m_InternalExceptionLocation = Info.ILGen.DefineLabel
        UnstructuredExceptionHandler = Info.ILGen.DefineLabel
        UnstructuredSwitchHandler = Info.ILGen.DefineLabel
        UnstructuredSwitchHandlerEnd = Info.ILGen.DefineLabel

        UnstructuredExceptionHandlerVariable = Info.ILGen.DeclareLocal(Compiler.TypeCache.Integer)
        IsInUnstructuredHandler = Info.ILGen.DeclareLocal(Compiler.TypeCache.Integer)

        EndUnstructuredExceptionHandler = Info.ILGen.BeginExceptionBlock()
        UnstructuredExceptionLabels = New Generic.List(Of Label)

        'At entry to the method, the exception-handler location and the exception are both set to Nothing. 
        Emitter.EmitCall(Info, Compiler.TypeCache.MS_VB_CS_PD_ClearProjectError)

        UnstructuredExceptionLabels.Add(UnstructuredSwitchHandlerEnd) 'index 0
        If Me.HasResume Then
            Me.CurrentInstruction = Info.ILGen.DeclareLocal(Compiler.TypeCache.Integer)
            ResumeNextExceptionHandler = Info.ILGen.DefineLabel

            UnstructuredExceptionLabels.Add(ResumeNextExceptionHandler) 'index 1
        Else
            UnstructuredExceptionLabels.Add(UnstructuredSwitchHandlerEnd) 'index 1
        End If

        Return result
    End Function

    Private Function GenerateUnstructuredEnd(ByVal Method As IMethod, ByVal Info As EmitInfo) As Boolean
        Dim result As Boolean = True
        Dim retvar As LocalBuilder = Method.DefaultReturnVariable

        'Add a label to the end of the code as the last item in the switch.

        If retvar IsNot Nothing Then
            Emitter.EmitLeave(Info, Me.EndMethodLabel)
        Else
            Emitter.EmitLeave(Info, Me.EndMethodLabel)
        End If

        Me.UnstructuredExceptionLabels.Add(UnstructuredSwitchHandlerEnd)

        Dim tmpVar As LocalBuilder = Info.ILGen.DeclareLocal(Compiler.TypeCache.Integer)
        If Me.HasResume Then
            'Increment the instruction pointer index with one, then jump to the switch
            Info.ILGen.MarkLabel(ResumeNextExceptionHandler)
            Emitter.EmitLoadI4Value(Info, -1)
            Emitter.EmitStoreVariable(Info, IsInUnstructuredHandler)
            Emitter.EmitLoadVariable(Info, CurrentInstruction)
            Emitter.EmitLoadI4Value(Info, 1)
            Emitter.EmitAdd(Info, Compiler.TypeCache.Integer)
            Emitter.EmitStoreVariable(Info, tmpVar)
            Emitter.EmitLeave(Info, UnstructuredSwitchHandler)
        End If

        'Emit the actual handler 
        Info.ILGen.MarkLabel(UnstructuredExceptionHandler)
        Emitter.EmitLoadI4Value(Info, -1)
        Emitter.EmitStoreVariable(Info, IsInUnstructuredHandler)
        Emitter.EmitLoadVariable(Info, UnstructuredExceptionHandlerVariable)
        Emitter.EmitStoreVariable(Info, tmpVar)
        Info.ILGen.MarkLabel(UnstructuredSwitchHandler)
        Emitter.EmitLoadVariable(Info, tmpVar)
        Emitter.EmitSwitch(Info, UnstructuredExceptionLabels.ToArray)

        Info.ILGen.MarkLabel(UnstructuredSwitchHandlerEnd)
        Emitter.EmitLeave(Info, EndMethodLabel)

        'Catch the exception

        'create a filter, only handle the exception if it is of type Exception, 
        'if it was not raised when in the unstructured handler and if there actually
        'is a registered exception handler.
        Info.ILGen.BeginExceptFilterBlock()
        Info.Stack.Push(Compiler.TypeCache.Object)
        Emitter.EmitIsInst(Info, Compiler.TypeCache.Object, Compiler.TypeCache.Exception)
        Emitter.EmitLoadNull(Info.Clone(True, False, Compiler.TypeCache.Exception))
        Emitter.EmitGT_Un(Info, Compiler.TypeCache.Exception) 'TypeOf ... Is System.Exception

        Emitter.EmitLoadVariable(Info, Me.UnstructuredExceptionHandlerVariable)
        Emitter.EmitLoadI4Value(Info, 0)
        Emitter.EmitGT(Info, Compiler.TypeCache.Integer) 'if a handler is registered.
        Emitter.EmitAnd(Info, Compiler.TypeCache.Boolean)

        Emitter.EmitLoadVariable(Info, Me.IsInUnstructuredHandler)
        Emitter.EmitLoadI4Value(Info, 0)
        Emitter.EmitGT(Info, Compiler.TypeCache.Integer) 'if code is in a unstructured handler or not
        Emitter.EmitAnd(Info, Compiler.TypeCache.Boolean)

        Info.Stack.Pop(Compiler.TypeCache.Boolean)

        'create the catch block
        Info.ILGen.BeginCatchBlock(Nothing)
        Info.Stack.Push(Compiler.TypeCache.Object)
        Emitter.EmitCastClass(Info, Compiler.TypeCache.Object, Compiler.TypeCache.Exception)
        Emitter.EmitCall(Info, Compiler.TypeCache.MS_VB_CS_PD_SetProjectError__Exception)
        Emitter.EmitLeave(Info, UnstructuredExceptionHandler)

        Info.ILGen.EndExceptionBlock()

        'Create an internal exception if the code gets here.
        Info.ILGen.MarkLabel(m_InternalExceptionLocation)
        Emitter.EmitLoadI4Value(Info, -2146828237)
        Emitter.EmitCall(Info, Compiler.TypeCache.MS_VB_CS_PD_CreateProjectError__Integer)
        Emitter.EmitThrow(Info)

        Info.ILGen.MarkLabel(EndMethodLabel)

        Dim veryMethodEnd As Label = Info.ILGen.DefineLabel
        Emitter.EmitLoadVariable(Info.Clone(True, False, Compiler.TypeCache.Boolean), IsInUnstructuredHandler)
        Info.Stack.SwitchHead(Compiler.TypeCache.Integer, Compiler.TypeCache.Boolean)
        Emitter.EmitBranchIfFalse(Info, veryMethodEnd)
        Emitter.EmitCall(Info, Compiler.TypeCache.MS_VB_CS_PD_ClearProjectError)
        Info.ILGen.MarkLabel(veryMethodEnd)

        If retvar IsNot Nothing Then
            Emitter.MarkLabel(Info, m_EndOfMethodLabel.Value)
            Emitter.EmitLoadVariable(Info, retvar)
            Info.ILGen.Emit(OpCodes.Ret)
        Else
            Emitter.MarkLabel(Info, m_EndOfMethodLabel.Value)
            Info.ILGen.Emit(OpCodes.Ret)
        End If

        Return result
    End Function

    ReadOnly Property UpmostBlock() As CodeBlock
        Get
            Dim result As CodeBlock = Nothing
            Dim tmp As CodeBlock

            tmp = Me
            Do Until tmp Is Nothing
                result = tmp
                tmp = tmp.FindFirstParent(Of CodeBlock)()
            Loop

            Return result
        End Get
    End Property

    ''' <summary>
    ''' Call this function from a method beeing emitted.
    ''' </summary>
    ''' <param name="Method"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Friend Overloads Function GenerateCode(ByVal Method As IMethod) As Boolean
        Dim result As Boolean = True

#If EXTENDEDDEBUG Then
        Compiler.Report.WriteLine(vbnc.Report.ReportLevels.Debug, "Emitting method: " & Method.FullName)
#End If

        Dim info As New EmitInfo(Method)

        m_EndOfMethodLabel = Emitter.DefineLabel(info)
        If Me.HasUnstructuredExceptionHandling Then
            result = GenerateUnstructuredStart(info) AndAlso result
        End If

#If DEBUG Then
        If Method.MemberDescriptor.MemberType = MemberTypes.Constructor = False Then
            info.ILGen.Emit(OpCodes.Nop)
        End If
#End If

        result = GenerateCode(info) AndAlso result

        If Me.HasUnstructuredExceptionHandling = False Then
            Emitter.MarkLabel(info, m_EndOfMethodLabel.Value)
        End If

        Dim retvar As LocalBuilder = Method.DefaultReturnVariable
        If retvar IsNot Nothing Then
            Emitter.EmitLoadVariable(info, retvar)
        Else
            Helper.Assert(Method.HasReturnValue = False)
        End If

        If Me.HasUnstructuredExceptionHandling Then
            result = GenerateUnstructuredEnd(Method, info) AndAlso result
        Else
            Emitter.EmitRet(info)
#If DEBUGREFLECTION Then
            Dim obj As Object
            If TypeOf info.Method Is ConstructorDeclaration Then
                obj = CType(info.Method, ConstructorDeclaration).ConstructorBuilder
            Else
                obj = info.Method.MethodBuilder
            End If
            Helper.DebugReflection_AppendLine("{0} = {1}.GetILGenerator", info.ILGen, obj)
            Helper.DebugReflection_AppendLine("{0}.Emit (System.Reflection.Emit.Opcodes.Ret)", info.ILGen)
#End If
        End If

#If EXTENDEDDEBUG Then
        Compiler.Report.WriteLine(vbnc.Report.ReportLevels.Debug, "Method " & Method.FullName & " emitted (ID: " & Method.ObjectID.ToString & ")")
#End If
#If EXTENDEDDEBUG Then
        If info.Stack.Count <> 0 Then
            Throw New InternalException("End of method " & Method.FullName & " reached, but stack is not empty.")
        End If
#End If
        Return result
    End Function

    Private Function CreateLabelForCurrentInstruction(ByVal Info As EmitInfo) As Boolean
        Dim result As Boolean = True
        If UpmostBlock.HasResume Then
            Dim index As Integer
            Dim lbl As Label = Info.ILGen.DefineLabel
            UpmostBlock.UnstructuredExceptionLabels.Add(lbl)
            index = UpmostBlock.UnstructuredExceptionLabels.IndexOf(lbl)
            Info.ILGen.MarkLabel(lbl)
            Emitter.EmitLoadI4Value(Info, index)
            Emitter.EmitStoreVariable(Info, UpmostBlock.CurrentInstruction)
        End If
        Return result
    End Function

    Friend Overrides Function GenerateCode(ByVal Info As EmitInfo) As Boolean
        Dim result As Boolean = True

#If DEBUG Then
        Info.Stack.CheckStackEmpty("Start of block " & Me.GetType.Name & " - " & Location.ToString & ") in " & Info.Method.FullName & " reached, but stack is not empty.")
#End If

        For i As Integer = 0 To m_Variables.Count - 1
            Dim var As VariableDeclaration = m_Variables(i)
            result = CreateLabelForCurrentInstruction(Info) AndAlso result
            result = var.DefineLocalVariable(Info) AndAlso result
        Next

        For i As Integer = 0 To m_Sequence.Count - 1
            Dim stmt As BaseObject = m_Sequence.Item(i)

#If DEBUG Then
            Info.Stack.CheckStackEmpty("Start of statement #" & (i + 1).ToString & " (" & stmt.GetType.Name & " - " & stmt.Location.ToString & ") in " & Info.Method.FullName & " reached, but stack is not empty.")
#End If

            Emitter.MarkSequencePoint(Info, stmt.Location)

            result = CreateLabelForCurrentInstruction(Info) AndAlso result
            result = stmt.GenerateCode(Info) AndAlso result
#If DEBUG Then
            Info.Stack.CheckStackEmpty("End of statement #" & (i + 1).ToString & " (" & stmt.GetType.Name & " - " & stmt.Location.ToString & ") in " & Info.Method.FullName & " reached, but stack is not empty.")
#End If
        Next

        Return result
    End Function

    ReadOnly Property Variables() As Nameables(Of VariableDeclaration)
        Get
            Return m_Variables
        End Get
    End Property

    Overridable ReadOnly Property IsOneLiner() As Boolean
        Get
            If TypeOf Me.Parent Is CodeBlock Then
                Return DirectCast(Me.Parent, CodeBlock).IsOneLiner
            ElseIf TypeOf Me.Parent Is Statement Then
                Return DirectCast(Me.Parent, Statement).IsOneLiner
            Else
                Return False
            End If
        End Get
    End Property

    Public Overrides Function ResolveTypeReferences() As Boolean
        Dim result As Boolean = True

        For i As Integer = 0 To m_Variables.Count - 1
            result = m_Variables(i).ResolveTypeReferences AndAlso result
            'vbnc.Helper.Assert(result = (Report.Errors = 0))
        Next

        For Each obj As Statement In m_Statements
            result = obj.ResolveTypeReferences AndAlso result
            'vbnc.Helper.Assert(result = (Report.Errors = 0))
        Next

        Return result
    End Function

    Public Overrides Function ResolveCode(ByVal Info As ResolveInfo) As Boolean
        Dim result As Boolean = True

        For i As Integer = 0 To m_Variables.Count - 1
            result = m_Variables(i).ResolveMember(Info) AndAlso result
            result = m_Variables(i).ResolveCode(Info) AndAlso result
            'Helper.Assert(result = (Compiler.Report.Errors = 0))
        Next

        For Each obj As Statement In m_Statements
            result = obj.ResolveStatement(Info) AndAlso result
            'Helper.Assert(result = (Compiler.Report.Errors = 0))
        Next

        Return result
    End Function

    Function FindVariable(ByVal Name As String) As IAttributableNamedDeclaration
        Dim found As Generic.List(Of INameable)
        found = m_Variables.Index.Item(Name)
        If found Is Nothing Then
            Return Nothing
        ElseIf found.Count = 1 Then
            Return DirectCast(found(0), IAttributableNamedDeclaration)
        ElseIf found.Count > 1 Then
            Throw New InternalException(Me)
        Else
            Return Nothing
        End If
    End Function
End Class
