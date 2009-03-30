Imports System.ComponentModel.Design.Serialization
Imports System.Drawing.Design

<TypeConverter(GetType(OutlookBarButtonConverter)), ToolboxItem(False), DesignTimeVisible(False)> _
Public Class OutlookBarButton
    Inherits Component

    Public Event ButtonClicked(ByVal sender As Object, ByVal tag As Object)
    Public Event ButtonDoubleClicked(ByVal sender As Object, ByVal tag As Object)

    Private _OutlookBar As OutlookBar
    Private _Category As OutlookBarCategory

    'Member variables
    Private _Text As String = "Button"
    Private _ImageIndex As Integer = -1
    Private _Image As Image = Nothing
    Private _Pushed As Boolean = False
    Private _Tag As Object
    Private _enabled As Boolean = True

    Friend OuterBounds, ImageBounds, TextBounds, SelectionBounds As Rectangle

    Public Sub ButtonClick()
        RaiseEvent ButtonClicked(Me, Me.Tag)
    End Sub

    Public Sub ButtonDoubleClick()
        RaiseEvent ButtonDoubleClicked(Me, Me.Tag)
    End Sub

    <Browsable(False)> _
    Public Property Tag() As Object
        Get
            Return _Tag
        End Get
        Set(ByVal Value As Object)
            _Tag = Value
        End Set
    End Property

    <Description("Indicates whether the button is enabled."), DefaultValue(True)> _
    Public Property Enabled() As Boolean
        Get
            Return _enabled
        End Get
        Set(ByVal Value As Boolean)
            _enabled = Value
            If Not (_OutlookBar Is Nothing) Then _OutlookBar.Invalidate(OuterBounds)
        End Set
    End Property

    <DefaultValue(False), Description("Indicates whether the button appears in a toggled state.")> _
    Public Property Pushed() As Boolean
        Get
            Return _Pushed
        End Get
        Set(ByVal Value As Boolean)
            _Pushed = Value
            If Not (_OutlookBar Is Nothing) Then _OutlookBar.InvalidateButton(Me)
        End Set
    End Property

    Friend Function GetHeight(ByVal g As Graphics) As Integer
        Dim imageWidth, imageHeight As Integer
        Dim height As Integer
        Dim textSize As SizeF

        'OutlookBar must be something, since we're added to it

        'Get text size
        textSize = g.MeasureString(_Text, _OutlookBar.Font, New SizeF(_OutlookBar.ClientRectangle.Width, 100), _Category.buttonTextFormat)
        height = Convert.ToInt32(Math.Ceiling(textSize.Height))

        If _Category.LayoutType = CategoryLayoutType.TextBelow Then
            'Increase by image size
            If Not _OutlookBar.ImageList Is Nothing Then
                height += 3
                height += _OutlookBar.ImageList.ImageSize.Height
                height += 4
            End If
        Else
            'Increase by image size
            If Not _OutlookBar.ImageList Is Nothing Then
                If _OutlookBar.ImageList.ImageSize.Height > height Then height = _OutlookBar.ImageList.ImageSize.Height
            End If

            height += 4
        End If

        Return height
    End Function

    Friend Property OutlookBar() As OutlookBar
        Get
            Return _OutlookBar
        End Get
        Set(ByVal Value As OutlookBar)
            _OutlookBar = Value
        End Set
    End Property

    <Browsable(False)> _
    Public ReadOnly Property Category() As OutlookBarCategory
        Get
            Return _Category
        End Get
    End Property

    Friend Sub SetCategory(ByVal category As OutlookBarCategory)
        _Category = category
    End Sub

    <DefaultValue("Button"), Description("The text contained in the item."), Localizable(True)> _
    Public Property Text() As String
        Get
            Return _Text
        End Get
        Set(ByVal Value As String)
            _Text = Value
            If Not (OutlookBar Is Nothing) Then OutlookBar.InvalidateLayout()
        End Set
    End Property

    <Browsable(False)> _
    Public ReadOnly Property ImageList() As ImageList
        Get
            If Not _OutlookBar Is Nothing Then Return _OutlookBar.ImageList
        End Get
    End Property

    Public Property Image() As Image
        Get
            Return _Image
        End Get
        Set(ByVal Value As Image)
            _Image = Value
            If Not (_OutlookBar Is Nothing) Then OutlookBar.InvalidateLayout()
        End Set
    End Property

    <DefaultValue(-1), TypeConverter(GetType(ImageIndexConverter)), Editor("OutlookBar.ButtonIconEditor", GetType(UITypeEditor))> _
    Public Property ImageIndex() As Integer
        Get
            Return _ImageIndex
        End Get
        Set(ByVal Value As Integer)
            _ImageIndex = Value
            If Not (_OutlookBar Is Nothing) Then OutlookBar.InvalidateLayout()
        End Set
    End Property

End Class

Friend Class OutlookBarButtonConverter
    Inherits TypeConverter

    Public Overloads Overrides Function CanConvertTo(ByVal context As ITypeDescriptorContext, ByVal destType As Type) As Boolean
        If destType Is GetType(InstanceDescriptor) Then
            Return True
        End If

        Return MyBase.CanConvertTo(context, destType)
    End Function

    Public Overloads Overrides Function ConvertTo(ByVal context As ITypeDescriptorContext, ByVal culture As System.Globalization.CultureInfo, ByVal value As Object, ByVal destType As Type) As Object
        If destType Is GetType(InstanceDescriptor) Then
            Dim ci As System.Reflection.ConstructorInfo = GetType(OutlookBarButton).GetConstructor(System.Type.EmptyTypes)

            Return New InstanceDescriptor(ci, Nothing, False)
        End If

        Return MyBase.ConvertTo(context, culture, value, destType)
    End Function
End Class

Public Class OutlookBarButtonCollection
    Inherits CollectionBase

    Private OutlookBarCategory As OutlookBarCategory
    Private _OutlookBar As OutlookBar

    Friend Sub New(ByVal OutlookBarCategory As OutlookBarCategory)
        MyBase.New()

        Me.OutlookBarCategory = OutlookBarCategory
    End Sub

    Default Public ReadOnly Property Item(ByVal Index As Integer) As OutlookBarButton
        Get
            Return DirectCast(list(Index), OutlookBarButton)
        End Get
    End Property

    Friend Property OutlookBar() As OutlookBar
        Get
            Return OutlookBar
        End Get
        Set(ByVal Value As OutlookBar)
            Dim b As OutlookBarButton

            _OutlookBar = Value
            For Each b In list
                b.OutlookBar = Value
            Next
        End Set
    End Property

    Public Function Add(ByVal Button As OutlookBarButton) As Integer
        Add = list.Add(Button)
        Button.OutlookBar = OutlookBarCategory.OutlookBar
        Button.SetCategory(OutlookBarCategory)

        If Not (OutlookBarCategory.OutlookBar Is Nothing) Then
            If OutlookBarCategory.OutlookBar.SelectedCategory Is OutlookBarCategory Then OutlookBarCategory.OutlookBar.InvalidateLayout()
        End If
    End Function

    Public Sub Insert(ByVal Index As Integer, ByVal Button As OutlookBarButton)
        list.Insert(Index, Button)
        Button.OutlookBar = OutlookBarCategory.OutlookBar
        Button.SetCategory(OutlookBarCategory)

        If Not (OutlookBarCategory.OutlookBar Is Nothing) Then
            If OutlookBarCategory.OutlookBar.SelectedCategory Is OutlookBarCategory Then OutlookBarCategory.OutlookBar.InvalidateLayout()
        End If
    End Sub

    Public Shadows Sub Clear()
        MyBase.Clear()

        If Not (OutlookBarCategory.OutlookBar Is Nothing) Then
            If OutlookBarCategory.OutlookBar.SelectedCategory Is OutlookBarCategory Then OutlookBarCategory.OutlookBar.InvalidateLayout()
        End If
    End Sub

    Public Sub Remove(ByVal Button As OutlookBarButton)
        list.Remove(Button)
        Button.OutlookBar = Nothing
        Button.SetCategory(Nothing)

        If Not (OutlookBarCategory.OutlookBar Is Nothing) Then
            If OutlookBarCategory.OutlookBar.SelectedCategory Is OutlookBarCategory Then OutlookBarCategory.OutlookBar.InvalidateLayout()
        End If
    End Sub

    Public Function Contains(ByVal Button As OutlookBarButton) As Boolean
        Return list.Contains(Button)
    End Function

    Public Function IndexOf(ByVal Button As OutlookBarButton) As Integer
        Return list.IndexOf(Button)
    End Function

End Class