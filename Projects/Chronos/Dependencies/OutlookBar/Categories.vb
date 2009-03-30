Imports System.ComponentModel.Design.Serialization

Public Enum CategoryLayoutType
    TextRight
    TextBelow
End Enum

Public Enum ButtonHighlightType
    ImageOnly
    ImageAndText
End Enum

<Designer(GetType(OutlookBarCategoryDesigner)), TypeConverter(GetType(OutlookBarCategoryConverter)), ToolboxItem(False), DesignTimeVisible(False)> _
Public Class OutlookBarCategory
    Inherits Component

    Private _OutlookBar As OutlookBar

    'Member variables
    Private _Text As String = "Label"
    Private _Buttons As OutlookBarButtonCollection
    Private _LayoutType As CategoryLayoutType = CategoryLayoutType.TextBelow
    Private _HighlightType As ButtonHighlightType = ButtonHighlightType.ImageOnly
    Private _ButtonSpacing As Integer = 10

    'Drawing and layout
    Friend HeaderBounds, ClientBounds As Rectangle
    Friend buttonTextFormat As StringFormat
    Friend idealHeight As Integer

    Public Sub New()
        MyBase.New()

        _Buttons = New OutlookBarButtonCollection(Me)
    End Sub

    <Browsable(False)> _
    Public ReadOnly Property Bounds() As Rectangle
        Get
            Return ClientBounds
        End Get
    End Property

    <DefaultValue(10)> _
    Public Property ButtonSpacing() As Integer
        Get
            Return _ButtonSpacing
        End Get
        Set(ByVal Value As Integer)
            If Value < 0 Or Value > 20 Then Throw New ArgumentException("Button spacing must be a value between 0 and 20.")

            _ButtonSpacing = Value
            If Not (_OutlookBar Is Nothing) Then _OutlookBar.InvalidateLayout()
        End Set
    End Property

    Friend Sub InitializeFormat()
        If Not buttonTextFormat Is Nothing Then buttonTextFormat.Dispose()

        buttonTextFormat = New StringFormat
        With buttonTextFormat
            If _LayoutType = CategoryLayoutType.TextBelow Then
                .Alignment = StringAlignment.Center
                .LineAlignment = StringAlignment.Near
            Else
                .Alignment = StringAlignment.Near
                .LineAlignment = StringAlignment.Center
                .FormatFlags = StringFormatFlags.NoWrap
            End If
            .Trimming = StringTrimming.EllipsisCharacter
        End With
    End Sub

    <Browsable(False), DesignerSerializationVisibility(DesignerSerializationVisibility.Content)> _
    Public ReadOnly Property Buttons() As OutlookBarButtonCollection
        Get
            Return _Buttons
        End Get
    End Property

    <DefaultValue(GetType(ButtonHighlightType), "ImageOnly")> _
    Public Property HighlightType() As ButtonHighlightType
        Get
            Return _HighlightType
        End Get
        Set(ByVal Value As ButtonHighlightType)
            _HighlightType = Value
            If Not (_OutlookBar Is Nothing) Then
                _OutlookBar.Invalidate()
            End If
        End Set
    End Property

    <DefaultValue(GetType(CategoryLayoutType), "TextBelow")> _
    Public Property LayoutType() As CategoryLayoutType
        Get
            Return _LayoutType
        End Get
        Set(ByVal Value As CategoryLayoutType)
            _LayoutType = Value
            InitializeFormat()
            If Not (_OutlookBar Is Nothing) Then _OutlookBar.InvalidateLayout()
        End Set
    End Property

    Friend Property OutlookBar() As OutlookBar
        Get
            Return _OutlookBar
        End Get
        Set(ByVal Value As OutlookBar)
            _OutlookBar = Value
            _Buttons.OutlookBar = Value
        End Set
    End Property

    <DefaultValue("Label"), Description("The text contained in the item."), Localizable(True)> _
    Public Property Text() As String
        Get
            Return _Text
        End Get
        Set(ByVal Value As String)
            _Text = Value
            If Not (OutlookBar Is Nothing) Then OutlookBar.Invalidate(HeaderBounds)
        End Set
    End Property

End Class

Friend Class OutlookBarCategoryConverter
    Inherits TypeConverter

    Public Overloads Overrides Function CanConvertTo(ByVal context As ITypeDescriptorContext, ByVal destType As Type) As Boolean
        If destType Is GetType(InstanceDescriptor) Then
            Return True
        End If

        Return MyBase.CanConvertTo(context, destType)
    End Function

    Public Overloads Overrides Function ConvertTo(ByVal context As ITypeDescriptorContext, ByVal culture As System.Globalization.CultureInfo, ByVal value As Object, ByVal destType As Type) As Object
        If destType Is GetType(InstanceDescriptor) Then
            Dim ci As System.Reflection.ConstructorInfo = GetType(OutlookBarCategory).GetConstructor(System.Type.EmptyTypes)

            Return New InstanceDescriptor(ci, Nothing, False)
        End If

        Return MyBase.ConvertTo(context, culture, value, destType)
    End Function
End Class

Public Class OutlookBarCategoryCollection
    Inherits CollectionBase

    Private OutlookBar As OutlookBar

    Friend Sub New(ByVal OutlookBar As OutlookBar)
        MyBase.New()

        Me.OutlookBar = OutlookBar
    End Sub

    Default Public ReadOnly Property Item(ByVal Index As Integer) As OutlookBarCategory
        Get
            Return DirectCast(list(Index), OutlookBarCategory)
        End Get
    End Property

    Public Function Add(ByVal Category As OutlookBarCategory) As Integer
        Insert(list.Count, Category)
        Return list.Count - 1
    End Function

    Public Sub Insert(ByVal Index As Integer, ByVal Category As OutlookBarCategory)
        list.Insert(Index, Category)
        Category.OutlookBar = OutlookBar

        If list.Count = 1 Then OutlookBar.SelectedCategory = Category

        OutlookBar.InvalidateLayout()
    End Sub

    Public Sub Remove(ByVal Category As OutlookBarCategory)
        list.Remove(Category)
        Category.OutlookBar = Nothing

        If Category Is OutlookBar.SelectedCategory Then
            If list.Count = 0 Then
                OutlookBar.NoSelectedCategory()
            Else
                OutlookBar.SelectedCategory = Item(0)
            End If
        End If

        OutlookBar.InvalidateLayout()
    End Sub

    Public Function Contains(ByVal Category As OutlookBarCategory) As Boolean
        Return list.Contains(Category)
    End Function

    Public Function IndexOf(ByVal Category As OutlookBarCategory) As Integer
        Return list.IndexOf(Category)
    End Function

End Class