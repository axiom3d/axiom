Imports System.Windows.Forms.Design

Friend Class OutlookBarDesigner
    Inherits ControlDesigner

    Private OutlookBar As OutlookBar

    Public Overrides Sub Initialize(ByVal component As System.ComponentModel.IComponent)
        MyBase.Initialize(component)

        'Record toolbar
        OutlookBar = DirectCast(component, OutlookBar)

        'Hook up SelectionChanged event
        Dim s As ISelectionService = DirectCast(GetService(GetType(ISelectionService)), ISelectionService)
        Dim c As IComponentChangeService = DirectCast(GetService(GetType(IComponentChangeService)), IComponentChangeService)
        AddHandler s.SelectionChanged, AddressOf OnSelectionChanged
        AddHandler c.ComponentRemoving, AddressOf OnComponentRemoving
        'AddHandler c.ComponentChanged, AddressOf OnComponentChanged
    End Sub

    Private Sub OnSelectionChanged(ByVal sender As Object, ByVal e As System.EventArgs)
        OutlookBar.Invalidate() 'OnSelectionChanged(DirectCast(sender, ISelectionService))
    End Sub

    'Private Sub OnComponentChanged(ByVal sender As Object, ByVal e As ComponentChangedEventArgs)
    '    'If e.Component Is OutlookBar Then
    '    '    OutlookBar.CalculateLayout()
    '    '    OutlookBar.Invalidate()
    '    'End If

    '    'If TypeOf e.Component Is OutlookBarCategory Then
    '    '    If OutlookBar.Categories.Contains(DirectCast(e.Component, OutlookBarCategory)) Then
    '    '        OutlookBar.CalculateLayout()
    '    '        OutlookBar.Invalidate()
    '    '    End If
    '    'End If
    'End Sub

    Private Sub OnComponentRemoving(ByVal sender As Object, ByVal e As ComponentEventArgs)
        Dim c As IComponentChangeService = DirectCast(GetService(GetType(IComponentChangeService)), IComponentChangeService)
        Dim cat As OutlookBarCategory, b As OutlookBarButton
        Dim h As IDesignerHost = DirectCast(GetService(GetType(IDesignerHost)), IDesignerHost)
        Dim i, j As Integer

        'If the user is removing a button
        If TypeOf e.Component Is OutlookBarButton Then
            b = DirectCast(e.Component, OutlookBarButton)
            For Each cat In OutlookBar.Categories
                If cat.Buttons.Contains(b) Then
                    c.OnComponentChanging(cat, Nothing)
                    cat.Buttons.Remove(b)
                    c.OnComponentChanged(cat, Nothing, Nothing, Nothing)
                    Return
                End If
            Next
        End If

        'See if the user is removing a category
        If TypeOf e.Component Is OutlookBarCategory Then
            cat = DirectCast(e.Component, OutlookBarCategory)
            If OutlookBar.Categories.Contains(cat) Then
                For i = cat.Buttons.Count - 1 To 0 Step -1
                    b = cat.Buttons(i)
                    c.OnComponentChanging(cat, Nothing)
                    h.DestroyComponent(b)
                    'cat.Buttons.Remove(b)
                    c.OnComponentChanged(cat, Nothing, Nothing, Nothing)
                Next
                c.OnComponentChanging(OutlookBar, Nothing)
                OutlookBar.Categories.Remove(cat)
                c.OnComponentChanged(OutlookBar, Nothing, Nothing, Nothing)
                Return
            End If
        End If

        'If user is removing the control itself
        If e.Component Is OutlookBar Then
            For i = OutlookBar.Categories.Count - 1 To 0 Step -1
                cat = OutlookBar.Categories(i)
                For j = cat.Buttons.Count - 1 To 0 Step -1
                    b = cat.Buttons(j)
                    c.OnComponentChanging(cat, Nothing)
                    h.DestroyComponent(b)
                    c.OnComponentChanged(cat, Nothing, Nothing, Nothing)
                Next
                c.OnComponentChanging(OutlookBar, Nothing)
                h.DestroyComponent(cat)
                c.OnComponentChanged(OutlookBar, Nothing, Nothing, Nothing)
            Next
            Return
        End If

    End Sub

    Protected Overloads Overrides Sub Dispose(ByVal disposing As Boolean)
        Dim s As ISelectionService = DirectCast(GetService(GetType(ISelectionService)), ISelectionService)
        Dim c As IComponentChangeService = DirectCast(GetService(GetType(IComponentChangeService)), IComponentChangeService)

        'Unhook events
        RemoveHandler s.SelectionChanged, AddressOf OnSelectionChanged
        RemoveHandler c.ComponentRemoving, AddressOf OnComponentRemoving
        'RemoveHandler c.ComponentChanged, AddressOf OnComponentChanged

        MyBase.Dispose(disposing)
    End Sub

    Public Overrides ReadOnly Property Verbs() As System.ComponentModel.Design.DesignerVerbCollection
        Get
            Dim v As New DesignerVerbCollection

            'Commands to insert and add buttons
            v.Add(New DesignerVerb("&Add Category", AddressOf OnAddCategory))

            Return v
        End Get
    End Property

    Private Sub OnAddCategory(ByVal sender As Object, ByVal e As EventArgs)
        Dim cat As OutlookBarCategory
        Dim h As IDesignerHost = DirectCast(GetService(GetType(IDesignerHost)), IDesignerHost)
        Dim dt As DesignerTransaction
        Dim c As IComponentChangeService = DirectCast(getservice(GetType(IComponentChangeService)), IComponentChangeService)

        'Add a new button to the collection
        dt = h.CreateTransaction("Add Item")
        c.OnComponentChanging(OutlookBar, Nothing)
        cat = DirectCast(h.CreateComponent(GetType(OutlookBarCategory)), OutlookBarCategory)
        OutlookBar.Categories.Add(cat)
        c.OnComponentChanged(OutlookBar, Nothing, Nothing, Nothing)
        dt.Commit()
    End Sub

    Public Overrides ReadOnly Property AssociatedComponents() As System.Collections.ICollection
        Get
            Return OutlookBar.Categories
        End Get
    End Property

    Protected Overrides Function GetHitTest(ByVal point As System.Drawing.Point) As Boolean
        Dim cat As OutlookBarCategory
        Dim button As OutlookBarButton
        Dim wrct As Rectangle

        point = OutlookBar.PointToClient(point)

        'See if we're on a category
        For Each cat In OutlookBar.Categories
            wrct = cat.HeaderBounds
            If wrct.Contains(point) Then Return True
        Next

        'See if we're on a button
        If Not OutlookBar.SelectedCategory Is Nothing Then
            For Each button In OutlookBar.SelectedCategory.Buttons
                wrct = button.TextBounds
                If wrct.Contains(point) Then Return True
                wrct = button.ImageBounds
                If wrct.Height <> 0 Then wrct.Height += 3
                If wrct.Contains(point) Then Return True
            Next
        End If

        'See if we're on a scroll button
        If OutlookBar.bShowScroll Then
            wrct = OutlookBar.scrollUpBounds
            If wrct.Contains(point) Then Return True
            wrct = OutlookBar.scrollDownBounds
            If wrct.Contains(point) Then Return True
        End If
    End Function

    Protected Overrides Sub WndProc(ByRef m As System.Windows.Forms.Message)
        Dim p, ps As Point
        Dim i As Integer

        Const MK_LBUTTON As Integer = &H1
        Const WM_MOUSEMOVE As Integer = &H200

        Select Case m.Msg
            Case WM_MOUSEMOVE
                i = m.LParam.ToInt32()
                p = New Point(i Mod 65536, i \ 65536)
                ps = OutlookBar.PointToScreen(p)
                If GetHitTest(ps) Then
                    If m.WParam.ToInt32() = MK_LBUTTON Then
                        OutlookBar.DoMouseMove(New MouseEventArgs(MouseButtons.Left, 1, p.X, p.Y, 0))
                    Else
                        OutlookBar.DoMouseMove(New MouseEventArgs(MouseButtons.None, 1, p.X, p.Y, 0))
                    End If
                Else
                    MyBase.WndProc(m)
                End If
            Case Else
                MyBase.WndProc(m)
        End Select
    End Sub
End Class
