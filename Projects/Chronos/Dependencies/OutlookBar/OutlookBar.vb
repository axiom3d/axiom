Public Enum AnimationType
    FastSlow
    SlowFast
    None
End Enum

<DefaultEvent("ButtonClick"), Designer(GetType(OutlookBarDesigner))> _
Public Class OutlookBar
    Inherits System.Windows.Forms.UserControl

#Region " Windows Form Designer generated code "

    Public Sub New()
        MyBase.New()

        'This call is required by the Windows Form Designer.
        InitializeComponent()

        'Add any initialization after the InitializeComponent() call
        Initialize()
    End Sub

    'UserControl overrides dispose to clean up the component list.
    Protected Overloads Overrides Sub Dispose(ByVal disposing As Boolean)
        If disposing Then
            _colours.Dispose()

            If Not (components Is Nothing) Then
                components.Dispose()
            End If
        End If
        MyBase.Dispose(disposing)
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()> Private Sub InitializeComponent()
        components = New System.ComponentModel.Container()
    End Sub

#End Region

    'Important collections and references
    Private _Categories As OutlookBarCategoryCollection
    Private _ImageList As ImageList
    Private _AnimationType As AnimationType = AnimationType.FastSlow
    Private _AnimationSpeed As Integer = 100

    'Drawing
    Private _categoryHeight As Integer = 22
    Private Const SCROLLAMOUNT As Integer = 15
    Private knownImageSize As Size
    Private _colours As DotNetColourManager
    Private categoryTextFormat As StringFormat
    Private ignoreSelectionEvents As Boolean
    Private bPressed As Boolean

    'Layout
    Private layoutInvalid As Boolean = False
    Private _hideCategoryHeadings As Boolean = False

    'Scrolling
    Friend bShowScroll As Boolean
    Friend scrollUpBounds, scrollDownBounds As Rectangle
    Private scrollUpHover, scrollDownHover As Boolean
    Private scrollOffset As Integer
    Private WithEvents tmrScroll As Windows.Forms.Timer

    'Animating
    Private bAnimating As Boolean
    Private animatePosition As Integer
    Private shiftButtonsCount As Integer

    'Category context
    Private _selectedCategory As OutlookBarCategory
    Private hoverCategory As OutlookBarCategory

    'Button context
    Private hoverButton As OutlookBarButton
    Private internalButtonSelected As Integer = -1
    Private movingTransaction As DesignerTransaction

    'Events
    Public Event ButtonClick(ByVal sender As Object, ByVal e As OutlookBarButtonClickEventArgs)
    Public Event SelectedCategoryChanged()
    Public Event ItemDrag(ByVal sender As Object, ByVal e As ItemDragEventArgs)

    Private Sub Initialize()
        'Initialize scroll timer
        tmrScroll = New Windows.Forms.Timer()

        'Set control drawing styles
        SetStyle(ControlStyles.AllPaintingInWmPaint, True)
        SetStyle(ControlStyles.DoubleBuffer, True)
        SetStyle(ControlStyles.Selectable, False)

        'Category text formatting
        categoryTextFormat = New StringFormat()
        With categoryTextFormat
            .Alignment = StringAlignment.Center
            .LineAlignment = StringAlignment.Center
            .Trimming = StringTrimming.EllipsisCharacter
            .FormatFlags = StringFormatFlags.NoWrap
        End With

        'Initialize categories collection
        _Categories = New OutlookBarCategoryCollection(Me)

        'Initialize drawing objects
        _colours = New DotNetColourManager
    End Sub

    Friend Sub InvalidateLayout()
        layoutInvalid = True
        Invalidate()
    End Sub

    <Category("Layout"), Description("The height, in pixels, of the category heading buttons."), DefaultValue(22)> _
    Public Property CategoryHeight() As Integer
        Get
            Return _categoryHeight
        End Get
        Set(ByVal Value As Integer)
            If Value < 10 Or Value > 50 Then
                Throw New ArgumentException("Value must be between 10 and 50.")
            End If
            _categoryHeight = Value
            InvalidateLayout()
        End Set
    End Property

    <Category("Appearance"), Description("Indicates whether to hide the category heading when only one category is present."), DefaultValue(False)> _
    Public Property HideCategoryHeadings() As Boolean
        Get
            Return _hideCategoryHeadings
        End Get
        Set(ByVal Value As Boolean)
            _hideCategoryHeadings = Value
            If Categories.Count = 1 Then InvalidateLayout()
        End Set
    End Property

    <DefaultValue(GetType(AnimationType), "FastSlow"), Category("Behavior"), Description("Indicates the type of animation performed when the user changes categories.")> _
    Public Property AnimationType() As AnimationType
        Get
            Return _AnimationType
        End Get
        Set(ByVal Value As AnimationType)
            _AnimationType = Value
        End Set
    End Property

    <DefaultValue(100), Category("Behavior"), Description("The speed, in milliseconds, it takes for the selected category to change.")> _
    Public Property AnimationSpeed() As Integer
        Get
            Return _AnimationSpeed
        End Get
        Set(ByVal Value As Integer)
            If Value < 10 Or Value > 1000 Then Throw New ArgumentException("Value must be between 10 and 1000.")
            _AnimationSpeed = Value
        End Set
    End Property

    <Browsable(False), DesignerSerializationVisibility(DesignerSerializationVisibility.Content)> _
    Public ReadOnly Property Categories() As OutlookBarCategoryCollection
        Get
            Return _Categories
        End Get
    End Property

    <Category("Behavior"), Description("The ImageList control used by the OutlookBar for images.")> _
    Public Property ImageList() As ImageList
        Get
            Return _ImageList
        End Get
        Set(ByVal Value As ImageList)
            'Remove event handler
            If Not _ImageList Is Nothing Then
                RemoveHandler _ImageList.RecreateHandle, AddressOf OnImageListHandleRecreated
            End If

            _ImageList = Value

            'Add event handler
            If Not _ImageList Is Nothing Then
                AddHandler _ImageList.RecreateHandle, AddressOf OnImageListHandleRecreated
            End If

            InvalidateLayout()
        End Set
    End Property

    Private Sub OnImageListHandleRecreated(ByVal sender As Object, ByVal e As System.EventArgs)
        InvalidateLayout()
    End Sub

    Public Function GetButtonAt(ByVal x As Integer, ByVal y As Integer) As OutlookBarButton
        Return GetButtonAt(New Point(x, y))
    End Function

    Public Function GetButtonAt(ByVal p As Point) As OutlookBarButton
        Dim r As Rectangle
        Dim b As OutlookBarButton

        If _selectedCategory Is Nothing Then Return Nothing

        For Each b In _selectedCategory.Buttons
            r = b.OuterBounds
            If r.Contains(p) Then Return b
        Next

        Return Nothing
    End Function

    <Browsable(False)> _
    Public Property SelectedCategory() As OutlookBarCategory
        Get
            Return _selectedCategory
        End Get
        Set(ByVal Value As OutlookBarCategory)
            If Categories.Count = 0 Then Return
            If Value Is Nothing Then Throw New ArgumentNullException
            If Not (Categories.Contains(Value)) Then Throw New ArgumentException("Category is not present in collection.")
            _selectedCategory = Value

            InvalidateLayout()

            RaiseEvent SelectedCategoryChanged()
        End Set
    End Property

    Protected Overrides Sub OnPaint(ByVal e As System.Windows.Forms.PaintEventArgs)
        Dim i As Integer
        Dim cat As OutlookBarCategory
        Dim buttonsToShift As Integer
        Dim textBrush As Brush

        Dim s As ISelectionService
        Dim selectedComponents As ArrayList

        textBrush = New SolidBrush(ForeColor)

        'If we need to recalculate the layout
        If (layoutInvalid) Then
            CalculateLayout(e.Graphics)
            layoutInvalid = False
        End If
        If Not _ImageList Is Nothing Then
            If Not Size.op_Equality(_ImageList.ImageSize, knownImageSize) Then
                CalculateLayout(e.Graphics)
            End If
        End If

        'Get selection service if necessary
        If DesignMode Then
            s = DirectCast(GetService(GetType(ISelectionService)), ISelectionService)
            selectedComponents = New ArrayList(s.GetSelectedComponents())
        End If

        'Draw category headings
        For i = 0 To Categories.Count - 1
            cat = Categories(i)

            'Draw category header in correct state
            If Not (_hideCategoryHeadings And Categories.Count = 1) Then
                e.Graphics.FillRectangle(SystemBrushes.Control, cat.HeaderBounds)

                If (hoverCategory Is cat) Then
                    If bPressed Then
                        ControlPaint.DrawBorder3D(e.Graphics, cat.HeaderBounds, Border3DStyle.SunkenInner)
                    Else
                        ControlPaint.DrawBorder3D(e.Graphics, cat.HeaderBounds, Border3DStyle.Raised)
                    End If
                Else
                    ControlPaint.DrawBorder3D(e.Graphics, cat.HeaderBounds, Border3DStyle.RaisedInner)
                End If
                e.Graphics.DrawString(cat.Text, Font, SystemBrushes.ControlText, RectangleF.op_Implicit(cat.HeaderBounds), categoryTextFormat)
            End If

            'If this is the selected category, draw buttons too
            If _selectedCategory Is cat Then
                DrawCategoryButtons(e.Graphics, textBrush, cat, selectedComponents)
                buttonsToShift = shiftButtonsCount
            ElseIf bAnimating = True And buttonsToShift <> 0 Then
                buttonsToShift -= 1
                If buttonsToShift = 0 Then DrawCategoryButtons(e.Graphics, textBrush, cat, selectedComponents)
            End If
        Next

        'Draw border
        ControlPaint.DrawBorder3D(e.Graphics, ClientRectangle, Border3DStyle.SunkenOuter)

        textBrush.Dispose()
    End Sub

    Private Function HasImage(ByVal button As OutlookBarButton) As Boolean
        If Not button.Image Is Nothing Then Return True
        If _ImageList Is Nothing Then Return False
        If button.ImageIndex < 0 Then Return False
        If button.ImageIndex >= _ImageList.Images.Count Then Return False

        Return True
    End Function

    Private Sub DrawCategoryButtons(ByVal g As Graphics, ByVal textBrush As Brush, ByVal cat As OutlookBarCategory, ByVal selectedComponents As ArrayList)
        Dim i As Integer
        Dim button As OutlookBarButton
        Dim wrct As Rectangle
        Dim highlightDesign As Boolean = False

        g.Clip = New Region(cat.ClientBounds)

        'Draw buttons
        For i = 0 To cat.Buttons.Count - 1
            'Get current button
            button = cat.Buttons(i)

            'Determine whether to highlight button anyway because it's selected
            If DesignMode Then
                If selectedComponents.Contains(button) Then highlightDesign = True Else highlightDesign = False
            Else
                highlightDesign = False
            End If

            'Draw selection if necessary
            If (hoverButton Is button And button.Enabled) Or highlightDesign Then
                If bPressed Then
                    _colours.DrawButtonHighlight(g, button.SelectionBounds, False, DotNetColourManager.HighlightMode.Pushed)
                Else
                    _colours.DrawButtonHighlight(g, button.SelectionBounds, False, DotNetColourManager.HighlightMode.Hot)
                End If
            ElseIf button.Pushed Then
                _colours.DrawButtonHighlight(g, button.SelectionBounds, False, DotNetColourManager.HighlightMode.Checked)
            End If

            'Process image
            If HasImage(button) Then
                'Draw image
                wrct = button.ImageBounds
                If Not Rectangle.Equals(wrct, Rectangle.Empty) Then
                    If (button.Enabled) Then
                        If Not button.Image Is Nothing Then
                            g.DrawImage(button.Image, wrct)
                        ElseIf button.ImageIndex <> -1 Then
                            g.DrawImage(_ImageList.Images(button.ImageIndex), wrct)
                        End If

                    Else
                        If Not button.Image Is Nothing Then
                            _colours.DrawImageDisabled(g, button.Image, wrct, BackColor)
                        Else
                            _colours.DrawImageDisabled(g, _ImageList.Images(button.ImageIndex), wrct, BackColor)
                        End If

                    End If
                Else
                        wrct = button.ImageBounds
                End If
            Else
                'If cat.HighlightType = ButtonHighlightType.ImageAndText Then
                '    wrct = button.OuterBounds
                '    wrct.X = 2
                '    wrct.Width = ClientRectangle.Width - 4
                '    If cat.LayoutType = CategoryLayoutType.TextBelow Then
                '        wrct.Height = button.GetHeight(g)
                '    Else
                '        wrct.Y -= 2
                '        wrct.Height = button.GetHeight(g) + 4
                '    End If

                '    If hoverButton Is button Then
                '        If bPressed Then
                '            _colours.DrawButtonHighlight(g, wrct, False, DotNetColourManager.HighlightMode.Pushed)
                '        Else
                '            _colours.DrawButtonHighlight(g, wrct, False, DotNetColourManager.HighlightMode.Hot)
                '        End If
                '    ElseIf button.Pushed Then
                '        _colours.DrawButtonHighlight(g, wrct, False, DotNetColourManager.HighlightMode.Checked)
                '    End If
                'End If
            End If

            'Draw text
            If (button.Enabled) Then
                g.DrawString(button.Text, Font, textBrush, RectangleF.op_Implicit(button.TextBounds), cat.buttonTextFormat)
            Else
                g.DrawString(button.Text, Font, SystemBrushes.ControlDark, RectangleF.op_Implicit(button.TextBounds), cat.buttonTextFormat)
            End If
        Next

        'Draw scroll buttons if necessary
        If bShowScroll Then
            'Draw up button
            If scrollOffset = 0 Then
                ControlPaint.DrawScrollButton(g, scrollUpBounds, ScrollButton.Up, ButtonState.Flat Or ButtonState.Inactive)
            ElseIf scrollUpHover Then
                If bPressed Then
                    ControlPaint.DrawScrollButton(g, scrollUpBounds, ScrollButton.Up, ButtonState.Pushed)
                Else
                    ControlPaint.DrawScrollButton(g, scrollUpBounds, ScrollButton.Up, ButtonState.Normal)
                End If
            Else
                ControlPaint.DrawScrollButton(g, scrollUpBounds, ScrollButton.Up, ButtonState.Flat)
            End If

            'Draw down button
            wrct = _selectedCategory.ClientBounds
            If scrollOffset = _selectedCategory.idealHeight - wrct.Height Then
                ControlPaint.DrawScrollButton(g, scrollDownBounds, ScrollButton.Down, ButtonState.Flat Or ButtonState.Inactive)
            ElseIf scrollDownHover Then
                If bPressed Then
                    ControlPaint.DrawScrollButton(g, scrollDownBounds, ScrollButton.Down, ButtonState.Pushed)
                Else
                    ControlPaint.DrawScrollButton(g, scrollDownBounds, ScrollButton.Down, ButtonState.Normal)
                End If
            Else
                ControlPaint.DrawScrollButton(g, scrollDownBounds, ScrollButton.Down, ButtonState.Flat)
            End If
        End If

        g.Clip = New Region(ClientRectangle)
    End Sub

    Private Sub CalculateLayout(ByVal g As Graphics)
        Dim i, y, h As Integer
        Dim wrct As Rectangle
        Dim buttonsToShift As Integer = 0

        If Not _ImageList Is Nothing Then knownImageSize = _ImageList.ImageSize

        If Categories.Count <> 0 Then
            y = 1
            For i = 0 To Categories.Count - 1
                'Calculate header rectangle
                wrct = New Rectangle(1, y, ClientRectangle.Width - 2, CategoryHeight)
                Categories(i).HeaderBounds = wrct

                'Move vertical pointer
                If _selectedCategory Is Categories(i) Then
                    h = ClientRectangle.Height - ((Categories.Count - i - 1) * CategoryHeight) - 1
                    If bAnimating Then
                        'Fix client bounds of current category
                        h -= wrct.Bottom
                        Categories(i).ClientBounds = New Rectangle(1, wrct.Bottom, ClientRectangle.Width - 2, Convert.ToInt32(h * (animatePosition / 100)))
                        y += Convert.ToInt32(h * (animatePosition / 100)) + CategoryHeight
                        buttonsToShift = shiftButtonsCount
                    Else
                        'Set client bounds of category and calculate buttons layout
                        If _hideCategoryHeadings And Categories.Count = 1 Then
                            Categories(i).ClientBounds = New Rectangle(1, 1, ClientRectangle.Width - 2, ClientRectangle.Height - 2)
                        Else
                            y = h
                            Categories(i).ClientBounds = New Rectangle(1, wrct.Bottom, ClientRectangle.Width - 2, y - wrct.Bottom)
                        End If
                    End If
                    CalculateButtonsLayout(Categories(i), g)
                ElseIf buttonsToShift <> 0 Then
                    y += CategoryHeight
                    buttonsToShift -= 1
                    If buttonsToShift = 0 Then
                        h = h - Convert.ToInt32(h * (animatePosition / 100))
                        Categories(i).ClientBounds = New Rectangle(1, wrct.Bottom, ClientRectangle.Width - 2, ClientRectangle.Height - wrct.Bottom)
                        CalculateButtonsLayout(Categories(i), g)
                        y += h
                    End If
                Else
                    y += CategoryHeight
                End If
            Next
        End If
    End Sub

    Private Sub CalculateButtonsLayout(ByVal cat As OutlookBarCategory, ByVal g As Graphics)
        Dim i, h As Integer
        Dim y As Integer
        Dim wrct As Rectangle
        Dim button As OutlookBarButton
        Dim tb As SizeF

        wrct = cat.ClientBounds

        'Calculate total needed height
        bShowScroll = False
        y = (cat.ButtonSpacing \ 2)
        If y < 5 Then y = 5
        For Each button In cat.Buttons
            y += button.GetHeight(g)
            y += cat.ButtonSpacing
        Next
        If cat.Buttons.Count <> 0 Then y -= cat.ButtonSpacing
        If y > wrct.Height Then
            'Up button
            wrct = cat.ClientBounds
            wrct.X = wrct.Right - SystemInformation.VerticalScrollBarWidth - 3
            wrct.Width = SystemInformation.VerticalScrollBarWidth
            wrct.Y += 3
            wrct.Height = SystemInformation.VerticalScrollBarThumbHeight
            scrollUpBounds = wrct

            'Draw down button
            wrct = cat.ClientBounds
            wrct.X = wrct.Right - SystemInformation.VerticalScrollBarWidth - 3
            wrct.Width = SystemInformation.VerticalScrollBarWidth
            wrct.Y = wrct.Bottom - SystemInformation.VerticalScrollBarThumbHeight - 3
            wrct.Height = SystemInformation.VerticalScrollBarThumbHeight
            scrollDownBounds = wrct

            bShowScroll = True
            cat.idealHeight = y
            wrct = cat.ClientBounds
            If scrollOffset > cat.idealHeight - wrct.Height Then scrollOffset = cat.idealHeight - wrct.Height
        Else
            scrollOffset = 0
        End If

        y = wrct.Top + (cat.ButtonSpacing \ 2)
        If y < wrct.Top + 5 Then y = wrct.Top + 5
        y -= scrollOffset
        For i = 0 To cat.Buttons.Count - 1
            button = cat.Buttons(i)

            'Get button height and calculate outer bounds
            h = button.GetHeight(g)
            wrct = New Rectangle(1, y, ClientRectangle.Width - 3, h)
            cat.Buttons(i).OuterBounds = wrct

            If cat.LayoutType = CategoryLayoutType.TextBelow Then
                'Calculate image bounds
                If Not _ImageList Is Nothing Then
                    'Construct image bounds
                    wrct = button.OuterBounds
                    wrct.X = (ClientRectangle.Width \ 2) - (_ImageList.ImageSize.Width \ 2)
                    wrct.Y += 2
                    wrct.Width = _ImageList.ImageSize.Width
                    wrct.Height = _ImageList.ImageSize.Height
                    button.ImageBounds = wrct

                    'Modify rectangle for text
                    wrct = button.OuterBounds
                    wrct.Y += _ImageList.ImageSize.Height + 7
                    wrct.Height -= (_ImageList.ImageSize.Height + 7)
                ElseIf Not button.Image Is Nothing Then
                    wrct = button.OuterBounds
                    wrct.X = (ClientRectangle.Width \ 2) - (button.Image.Width \ 2)
                    wrct.Y += 2
                    wrct.Width = button.Image.Width
                    wrct.Height = button.Image.Height
                    button.ImageBounds = wrct

                    'Modify rectangle for text
                    wrct = button.OuterBounds
                    wrct.Y += button.Image.Height + 7
                    wrct.Height -= (button.Image.Height + 7)
                End If

                'Calculate text bounds
                tb = g.MeasureString(button.Text, Font, New SizeF(ClientRectangle.Width - 2, 100), cat.buttonTextFormat)
                wrct.X = (ClientRectangle.Width \ 2) - (Convert.ToInt32(tb.Width) \ 2) + 1
                wrct.Width = Convert.ToInt32(tb.Width) + 2
                wrct.Height = Convert.ToInt32(tb.Height) + 1
                button.TextBounds = wrct
            Else
                'Calculate image bounds
                If HasImage(button) And Not _ImageList Is Nothing Then
                    'Construct image bounds
                    'wrct = button.OuterBounds
                    wrct.X = 5
                    wrct.Y += (wrct.Height \ 2) - (_ImageList.ImageSize.Height \ 2) '(wrct.Y + (wrct.Height \ 2)) - (_ImageList.ImageSize.Height \ 2) - 2
                    wrct.Width = _ImageList.ImageSize.Width
                    wrct.Height = _ImageList.ImageSize.Height
                    button.ImageBounds = wrct

                    'Modify rectangle for text
                    wrct = button.OuterBounds
                    wrct.X += _ImageList.ImageSize.Width + 7
                    wrct.Width -= (_ImageList.ImageSize.Height + 7)
                ElseIf Not button.Image Is Nothing Then
                    'Construct image bounds
                    'wrct = button.OuterBounds
                    wrct.X = 5
                    wrct.Y += (wrct.Height \ 2) - (button.Image.Height \ 2) '(wrct.Y + (wrct.Height \ 2)) - (_ImageList.ImageSize.Height \ 2) - 2
                    wrct.Width = button.Image.Width
                    wrct.Height = button.Image.Height
                    button.ImageBounds = wrct

                    'Modify rectangle for text
                    wrct = button.OuterBounds
                    wrct.X += button.Image.Width + 7
                    wrct.Width -= (button.Image.Height + 7)
                End If

                'Calculate text bounds
                button.TextBounds = wrct
            End If

            'Calculate selection bounds
            If cat.HighlightType = ButtonHighlightType.ImageAndText Then
                button.SelectionBounds = button.OuterBounds
            Else
                wrct = button.ImageBounds
                wrct.Inflate(2, 2)
                button.SelectionBounds = wrct
            End If

            y += h + cat.ButtonSpacing + 1
        Next
    End Sub

    Friend Sub NoSelectedCategory()
        _selectedCategory = Nothing
        RaiseEvent SelectedCategoryChanged()
    End Sub

    'Friend Sub OnSelectionChanged(ByVal SelectionService As ISelectionService)
    '    Dim cat As OutlookBarCategory
    '    Dim button As OutlookBarButton

    '    Dim newHoverButton As OutlookBarButton

    '    If ignoreSelectionEvents = True Then Exit Sub
    '    'If internalButtonSelected = -1 Then Exit Sub

    '    'The selection has changed
    '    For Each cat In _Categories
    '        For Each button In cat.Buttons
    '            If SelectionService.PrimarySelection Is button Then
    '                If Not _selectedCategory Is cat Then _selectedCategory = cat
    '                newHoverButton = button
    '                Exit For
    '            End If
    '        Next
    '    Next

    '    'If something has changed that we care about
    '    If Not newHoverButton Is hoverButton Then
    '        hoverButton = newHoverButton
    '        InvalidateLayout()
    '    End If
    'End Sub

    Protected Overrides Sub OnMouseMove(ByVal e As System.Windows.Forms.MouseEventArgs)
        MyBase.OnMouseMove(e)

        DoMouseMove(e)
    End Sub

    Friend Sub DoMouseMove(ByVal e As System.Windows.Forms.MouseEventArgs)
        If bAnimating Then Return

        If DesignMode Then
            If e.Button = MouseButtons.Left Then
                Dim h As IDesignerHost = DirectCast(GetService(GetType(IDesignerHost)), IDesignerHost)
                Dim c As IComponentChangeService = DirectCast(GetService(GetType(IComponentChangeService)), IComponentChangeService)
                Dim dt As DesignerTransaction
                Dim b As OutlookBarButton
                Dim i, iDragPos As Integer
                Dim wrct As Rectangle

                If _selectedCategory Is Nothing Then Return
                If internalButtonSelected = -1 Then Return

                'Find out position the user is dragging in
                iDragPos = -1
                For i = 0 To _selectedCategory.Buttons.Count - 1
                    b = _selectedCategory.Buttons(i)
                    wrct = b.OuterBounds
                    If wrct.Contains(e.X, e.Y) Then
                        If i = internalButtonSelected Then
                            'internalDragPosition = -1
                            Exit Sub
                        ElseIf i < internalButtonSelected Then
                            If e.Y < wrct.Y + (wrct.Height \ 2) Then
                                iDragPos = i
                            Else
                                iDragPos = i + 1
                            End If
                        ElseIf i > internalButtonSelected Then
                            If e.Y > wrct.Y + (wrct.Height \ 2) Then
                                iDragPos = i
                            Else
                                iDragPos = i - 1
                            End If
                        End If
                        Exit For
                    End If
                Next

                'Move button indicator
                If iDragPos = -1 Then
                    'internalDragPosition = -1
                    Invalidate()
                ElseIf iDragPos = internalButtonSelected Then
                    'internalDragPosition = -1
                    Invalidate()
                Else
                    'Create transaction if we're not already in one
                    If movingTransaction Is Nothing Then
                        movingTransaction = h.CreateTransaction("Move Button")
                        c.OnComponentChanging(_selectedCategory, Nothing)
                    End If
                    b = _selectedCategory.Buttons(internalButtonSelected)
                    _selectedCategory.Buttons.Remove(b)
                    _selectedCategory.Buttons.Insert(iDragPos, b)
                    internalButtonSelected = iDragPos
                End If
            End If
        Else
            Dim cat As OutlookBarCategory
            Dim button As OutlookBarButton
            Dim wrct As Rectangle

            Dim newHoverCategory As OutlookBarCategory = Nothing
            Dim bFoundCategory As Boolean = False
            Dim newHoverButton As OutlookBarButton = Nothing
            Dim bFoundButton As Boolean = False

            'See if the mouse is over a scroll button
            If bShowScroll Then
                If scrollUpBounds.Contains(e.X, e.Y) Then
                    scrollUpHover = True
                    hoverCategory = Nothing
                    hoverButton = Nothing
                    Invalidate()
                    Return
                End If
                If scrollDownBounds.Contains(e.X, e.Y) Then
                    scrollDownHover = True
                    hoverCategory = Nothing
                    hoverButton = Nothing
                    Invalidate()
                    Return
                End If
                If scrollUpHover Or scrollDownHover Then
                    scrollUpHover = False
                    scrollDownHover = False
                    Invalidate()
                End If
            End If

            'See if the mouse is over a category heading
            If Not (_hideCategoryHeadings And Categories.Count = 1) Then
                For Each cat In _Categories
                    wrct = cat.HeaderBounds
                    If wrct.Contains(e.X, e.Y) Then
                        newHoverCategory = cat
                        bFoundCategory = True
                        Exit For
                    End If
                Next
            End If

            'See if the mouse is over a button
            If Not bFoundCategory And Not _selectedCategory Is Nothing Then
                For Each button In _selectedCategory.Buttons
                    wrct = button.OuterBounds
                    'If wrct.Height <> 0 Then wrct.Height += 3
                    If wrct.Contains(e.X, e.Y) Then
                        newHoverButton = button
                        bFoundButton = True
                        Exit For
                    End If
                    'wrct = button.TextBounds
                    'If wrct.Contains(e.X, e.Y) Then
                    '    newHoverButton = button
                    '    bFoundButton = True
                    '    Exit For
                    'End If
                Next
            End If
            If Not newHoverButton Is Nothing And e.Button = MouseButtons.Left Then
                RaiseEvent ItemDrag(Me, New ItemDragEventArgs(e.Button, newHoverButton))
                Return
            End If

            'Determine whether to redraw
            If Not newHoverCategory Is hoverCategory Then
                hoverCategory = newHoverCategory
                Invalidate()
            End If
            If Not newHoverButton Is hoverButton Then
                If Not hoverButton Is Nothing Then InvalidateButton(hoverButton)
                hoverButton = newHoverButton
                If Not hoverButton Is Nothing Then InvalidateButton(hoverButton)
            End If
        End If
    End Sub

    Friend Sub InvalidateButton(ByVal button As OutlookBarButton)
        Dim wrct As Rectangle = button.OuterBounds
        wrct.Inflate(2, 2)
        Invalidate(wrct)
    End Sub

    Friend Sub InvalidateCategory(ByVal category As OutlookBarCategory)
        Dim wrct As Rectangle = category.HeaderBounds
        wrct.Width += 1
        wrct.Height += 1
        Invalidate(wrct)
    End Sub

    Protected Overrides Sub OnMouseDown(ByVal e As System.Windows.Forms.MouseEventArgs)
        MyBase.OnMouseDown(e)

        If bAnimating Then Return

        If DesignMode Then
            Dim cat As OutlookBarCategory
            Dim button As OutlookBarButton
            Dim wrct As Rectangle
            Dim s As ISelectionService, a As ArrayList

            'Select the category to edit
            For Each cat In _Categories
                wrct = cat.HeaderBounds
                If wrct.Contains(e.X, e.Y) Then
                    s = DirectCast(GetService(GetType(ISelectionService)), ISelectionService)
                    a = New ArrayList
                    a.Add(cat)
                    ignoreSelectionEvents = True
                    s.SetSelectedComponents(a)
                    ignoreSelectionEvents = False

                    If Not hoverButton Is Nothing Then
                        hoverButton = Nothing
                        Invalidate()
                    End If
                    SelectedCategory = cat
                    'Return True
                End If
            Next

            'Select a button
            If Not _selectedCategory Is Nothing Then
                For Each button In _selectedCategory.Buttons
                    wrct = button.OuterBounds
                    If wrct.Contains(e.X, e.Y) Then
                        s = DirectCast(GetService(GetType(ISelectionService)), ISelectionService)
                        a = New ArrayList
                        a.Add(button)
                        s.SetSelectedComponents(a)

                        internalButtonSelected = _selectedCategory.Buttons.IndexOf(button)
                    End If
                    'wrct = button.ImageBounds
                    'If wrct.Contains(e.X, e.Y) Then
                    '    s = DirectCast(GetService(GetType(ISelectionService)), ISelectionService)
                    '    a = New ArrayList
                    '    a.Add(button)
                    '    s.SetSelectedComponents(a)

                    '    internalButtonSelected = _selectedCategory.Buttons.IndexOf(button)
                    'End If
                Next
            End If
        Else
            'If they're hovering on a scroll button
            If scrollUpHover Or scrollDownHover Then
                bPressed = True
                tmrScroll.Interval = 300
                tmrScroll.Enabled = True
                PerformScroll()
            End If

            'If they're hovering on a category
            If Not hoverCategory Is Nothing Then
                bPressed = True
                InvalidateCategory(hoverCategory)
            End If

            'If they're hovering on a button
            If Not hoverButton Is Nothing Then
                bPressed = True
                InvalidateButton(hoverButton)
                If e.Clicks = 2 Then
                    hoverButton.ButtonDoubleClick()
                End If
            End If
        End If
    End Sub

    Protected Overrides Sub OnMouseUp(ByVal e As System.Windows.Forms.MouseEventArgs)
        MyBase.OnMouseUp(e)

        If bAnimating Then Return

        If DesignMode Then
            Dim c As IComponentChangeService = DirectCast(GetService(GetType(IComponentChangeService)), IComponentChangeService)

            'Commit move
            If Not (movingTransaction Is Nothing) Then
                c.OnComponentChanged(_selectedCategory, Nothing, Nothing, Nothing)
                movingTransaction.Commit()
                movingTransaction = Nothing
            End If

            internalButtonSelected = -1
        Else
            If bPressed = True Then
                'If they clicked a category
                If Not hoverCategory Is Nothing And Not hoverCategory Is SelectedCategory Then
                    scrollOffset = 0
                    AnimateChangeCategory()
                    bPressed = False
                    Return
                End If

                'If they clicked a button
                If Not hoverButton Is Nothing Then
                    bPressed = False
                    InvalidateButton(hoverButton)
                    If hoverButton.Enabled Then hoverButton.ButtonClick() 'RaiseEvent ButtonClick(Me, New OutlookBarButtonClickEventArgs(hoverButton))

                    Dim button As OutlookBarButton
                    Dim pushed As Boolean = hoverButton.Pushed
                    For Each button In _selectedCategory.Buttons
                        button.Pushed = False
                    Next

                    If Not pushed And hoverButton.Enabled Then hoverButton.Pushed = True
                    Return
                End If

                bPressed = False
                Invalidate()

                If tmrScroll.Enabled = True Then tmrScroll.Enabled = False
            End If
        End If
    End Sub

    Private Sub AnimateChangeCategory()
        Dim startTime As Integer
        Dim percentageThrough As Double
        Dim newCategory As OutlookBarCategory
        Dim bUP As Boolean

        newCategory = hoverCategory

        If _AnimationType <> AnimationType.None Then
            'Prepare
            bAnimating = True
            startTime = Environment.TickCount

            'Get direction
            If Categories.IndexOf(newCategory) < Categories.IndexOf(_selectedCategory) Then bUP = False Else bUP = True
            shiftButtonsCount = Math.Abs(Categories.IndexOf(newCategory) - Categories.IndexOf(_selectedCategory))
            If Not bUP Then _selectedCategory = newCategory

            'Debug stuff
            animatePosition = 25
            InvalidateLayout()

            Do
                'Calculate position through
                percentageThrough = Environment.TickCount - startTime
                percentageThrough /= _AnimationSpeed
                percentageThrough *= (Math.PI / 2)
                If _AnimationType = AnimationType.FastSlow Then
                    percentageThrough = Math.Cos((Math.PI / 2) - percentageThrough)
                Else
                    percentageThrough = 1 - Math.Cos(percentageThrough)
                End If
                'percentageThrough ^= 2

                'Adjust properties and redraw
                If bUP Then
                    animatePosition = 100 - Convert.ToInt32(percentageThrough * 100)
                Else
                    animatePosition = Convert.ToInt32(percentageThrough * 100)
                End If
                InvalidateLayout()
                Application.DoEvents()
            Loop Until Environment.TickCount > startTime + _AnimationSpeed
            bAnimating = False
        End If

        'Finalise
        SelectedCategory = newCategory
        InvalidateLayout()
    End Sub

    Private Sub tmrScroll_Tick(ByVal sender As Object, ByVal e As System.EventArgs) Handles tmrScroll.Tick
        If tmrScroll.Interval = 300 Then
            tmrScroll.Enabled = False
            tmrScroll.Interval = 50
            tmrScroll.Enabled = True
        End If

        PerformScroll()
    End Sub

    Private Sub PerformScroll()
        Dim wrct As Rectangle

        wrct = _selectedCategory.ClientBounds
        If scrollUpHover Then
            scrollOffset -= SCROLLAMOUNT
            If scrollOffset < 0 Then scrollOffset = 0
        ElseIf scrollDownHover Then
            scrollOffset += SCROLLAMOUNT
            If scrollOffset > _selectedCategory.idealHeight - wrct.Height Then
                scrollOffset = _selectedCategory.idealHeight - wrct.Height
            End If
        End If

        If scrollUpHover Or scrollDownHover Then InvalidateLayout()
    End Sub

    Protected Overrides Sub OnMouseLeave(ByVal e As System.EventArgs)
        MyBase.OnMouseLeave(e)

        Dim bChanged As Boolean

        'If a category header is highlighted
        If Not hoverCategory Is Nothing Then
            InvalidateCategory(hoverCategory)
            hoverCategory = Nothing
        End If

        'If a button is highlighted
        If Not hoverButton Is Nothing Then
            InvalidateButton(hoverButton)
            hoverButton = Nothing
        End If

        'If a scroll button is highlighted
        If scrollUpHover Or scrollDownHover Then
            scrollUpHover = False
            scrollDownHover = False
            bChanged = True
        End If

        If bChanged Then Invalidate()
    End Sub

    Protected Overrides Sub OnResize(ByVal e As System.EventArgs)
        MyBase.OnResize(e)

        InvalidateLayout()
    End Sub

    Protected Overrides Sub OnFontChanged(ByVal e As System.EventArgs)
        MyBase.OnFontChanged(e)

        InvalidateLayout()
    End Sub

End Class

Public Class OutlookBarButtonClickEventArgs
    Inherits EventArgs

    Private _Button As OutlookBarButton

    Public Sub New(ByVal Button As OutlookBarButton)
        _Button = Button
    End Sub

    Public ReadOnly Property Button() As OutlookBarButton
        Get
            Return _Button
        End Get
    End Property
End Class
