Imports System.Windows.Forms.Design

Friend Class OutlookBarCategoryDesigner
    Inherits ComponentDesigner

    Private Category As OutlookBarCategory

    Public Overrides Sub Initialize(ByVal component As System.ComponentModel.IComponent)
        MyBase.Initialize(component)

        'Record toolbar
        Category = DirectCast(component, OutlookBarCategory)
    End Sub

    Public Overrides ReadOnly Property Verbs() As System.ComponentModel.Design.DesignerVerbCollection
        Get
            Dim v As New DesignerVerbCollection

            'Commands to insert and add buttons
            v.Add(New DesignerVerb("&Add Button", AddressOf OnAddButton))
            'v.Add(New DesignerVerb("&Remove All Buttons", AddressOf OnRemoveAllButtons))
            v.Add(New DesignerVerb("Move &Up", AddressOf OnMoveUp))
            v.Add(New DesignerVerb("Move &Down", AddressOf OnMoveDown))

            Return v
        End Get
    End Property

    Private Sub OnMoveUp(ByVal sender As Object, ByVal e As System.EventArgs)
        MoveCategory(False)
    End Sub

    Private Sub OnMoveDown(ByVal sender As Object, ByVal e As System.EventArgs)
        MoveCategory(True)
    End Sub

    Private Sub MoveCategory(ByVal bDown As Boolean)
        Dim c As IComponentChangeService = DirectCast(GetService(GetType(IComponentChangeService)), IComponentChangeService)
        Dim h As IDesignerHost = DirectCast(GetService(GetType(IDesignerHost)), IDesignerHost)
        Dim t As DesignerTransaction
        Dim i As Integer
        Dim OutlookBar As OutlookBar

        i = Category.OutlookBar.Categories.IndexOf(Category)
        If bDown Then
            If i = Category.OutlookBar.Categories.Count - 1 Then Exit Sub
        Else
            If i = 0 Then Exit Sub
        End If
        OutlookBar = Category.OutlookBar

        'Do move
        t = h.CreateTransaction("Move Category")
        c.OnComponentChanging(Category.OutlookBar, Nothing)
        OutlookBar.Categories.Remove(Category)
        If bDown Then
            OutlookBar.Categories.Insert(i + 1, Category)
        Else
            OutlookBar.Categories.Insert(i - 1, Category)
        End If
        c.OnComponentChanged(Category.OutlookBar, Nothing, Nothing, Nothing)
        t.Commit()

        OutlookBar.SelectedCategory = Category
    End Sub

    'Private Sub OnRemoveAllButtons(ByVal sender As Object, ByVal e As EventArgs)
    '    Dim h As IDesignerHost = DirectCast(GetService(GetType(IDesignerHost)), IDesignerHost)
    '    Dim dt As DesignerTransaction
    '    Dim c As IComponentChangeService = DirectCast(getservice(GetType(IComponentChangeService)), IComponentChangeService)
    '    Dim i As Integer

    '    'Clear collection and remove all buttons from design surface
    '    dt = h.CreateTransaction("Remove All Buttons")
    '    c.OnComponentChanging(Category, Nothing)
    '    For i = Category.Buttons.Count - 1 To 0 Step -1
    '        h.DestroyComponent(Category.Buttons(i))
    '    Next
    '    Category.Buttons.Clear()
    '    c.OnComponentChanged(Category, Nothing, Nothing, Nothing)
    '    dt.Commit()

    '    'Update bar
    '    Category.OutlookBar.Invalidate()
    'End Sub

    Private Sub OnAddButton(ByVal sender As Object, ByVal e As EventArgs)
        Dim b As OutlookBarButton
        Dim h As IDesignerHost = DirectCast(GetService(GetType(IDesignerHost)), IDesignerHost)
        Dim dt As DesignerTransaction
        Dim c As IComponentChangeService = DirectCast(getservice(GetType(IComponentChangeService)), IComponentChangeService)

        'Add a new button to the collection
        dt = h.CreateTransaction("Add Button")
        c.OnComponentChanging(Category, Nothing)
        b = DirectCast(h.CreateComponent(GetType(OutlookBarButton)), OutlookBarButton)
        Category.Buttons.Add(b)
        c.OnComponentChanged(Category, Nothing, Nothing, Nothing)
        dt.Commit()
    End Sub

    Public Overrides ReadOnly Property AssociatedComponents() As System.Collections.ICollection
        Get
            Return Category.Buttons
        End Get
    End Property

End Class
