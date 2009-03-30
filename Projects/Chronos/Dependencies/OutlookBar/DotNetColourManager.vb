Imports System.Drawing.Imaging

Public Class DotNetColourManager
    Implements IDisposable

    Public Enum HighlightMode
        Hot
        Pushed
        Checked
    End Enum

    'Drawing objects
    Private _background As SolidBrush
    Private _hotBackground As SolidBrush
    Private _checkedBackground As SolidBrush
    Private _pushedBackground As SolidBrush
    Private _menuBackground As SolidBrush
    Private _menuMargin As SolidBrush
    Private _separator As Pen
    Private _hotBorder As Pen

    'Image attributes
    Private _blendAttributes As ImageAttributes
    Private _disabledBlendAttributes As ImageAttributes
    Private _shadowBlendAttributes As ImageAttributes

    'Customizing
    Private _useCustomColours As Boolean = False

    Friend Sub New()
        Dim colormatrix As ColorMatrix

        CalculateColours()
        AddHandler Microsoft.Win32.SystemEvents.UserPreferenceChanged, AddressOf UserPreferenceChanged

        'Set up colormatrix for alpha-blended images
        colormatrix = New ColorMatrix()
        colormatrix.Matrix33 = 0.7
        _blendAttributes = New ImageAttributes()
        _blendAttributes.SetColorMatrix(colormatrix)

        'Set up colormatrix for disabled alpha-blended images
        colormatrix = New ColorMatrix()
        colormatrix.Matrix33 = 0.5
        _disabledBlendAttributes = New ImageAttributes()
        _disabledBlendAttributes.SetColorMatrix(colormatrix)

        'Set up colormatrix for shadows of images
        colormatrix = New ColorMatrix()
        colormatrix.Matrix33 = 0.25
        _shadowBlendAttributes = New ImageAttributes()
        Dim cm As New ColorMap()
        cm.OldColor = Color.White
        cm.NewColor = Color.Black
        _shadowBlendAttributes.SetRemapTable(New ColorMap() {cm})
        _shadowBlendAttributes.SetGamma(10)
        _shadowBlendAttributes.SetColorMatrix(colormatrix)
    End Sub

    Public Sub SetDefaultColours()
        CalculateColours()
    End Sub

    Public Sub SetBackgroundColour(ByVal color As Color)
        _background = New SolidBrush(color)
        _useCustomColours = True
    End Sub

    Public Sub SetHighlightColourBase(ByVal baseColor As Color)
        CalculateHighlightColours(baseColor)
        _useCustomColours = True
    End Sub

    Public Sub SetSeparatorColour(ByVal color As Color)
        _separator = New Pen(color)
        _useCustomColours = True
    End Sub

    Private Sub UserPreferenceChanged(ByVal sender As Object, ByVal e As Microsoft.Win32.UserPreferenceChangedEventArgs)
        If e.Category = Microsoft.Win32.UserPreferenceCategory.Color And Not _useCustomColours Then CalculateColours()
    End Sub

    Public ReadOnly Property BlendAttributes() As ImageAttributes
        Get
            Return _blendAttributes
        End Get
    End Property

    Public ReadOnly Property DisabledBlendAttributes() As ImageAttributes
        Get
            Return _disabledBlendAttributes
        End Get
    End Property

    Public ReadOnly Property ShadowBlendAttributes() As ImageAttributes
        Get
            Return _shadowBlendAttributes
        End Get
    End Property

    Public ReadOnly Property HotBorder() As Pen
        Get
            Return _hotBorder
        End Get
    End Property

    Public ReadOnly Property Background() As SolidBrush
        Get
            Return _background
        End Get
    End Property

    Public ReadOnly Property HotBackground() As SolidBrush
        Get
            Return _hotBackground
        End Get
    End Property

    Public ReadOnly Property PushedBackground() As SolidBrush
        Get
            Return _pushedBackground
        End Get
    End Property

    Public ReadOnly Property CheckedBackground() As SolidBrush
        Get
            Return _checkedBackground
        End Get
    End Property

    Public ReadOnly Property Separator() As Pen
        Get
            Return _separator
        End Get
    End Property

    Public ReadOnly Property MenuBackground() As SolidBrush
        Get
            Return _menuBackground
        End Get
    End Property

    Public ReadOnly Property MenuMargin() As SolidBrush
        Get
            Return _menuMargin
        End Get
    End Property

    Public Sub DrawImageDisabled(ByVal g As Graphics, ByVal image As Image, ByVal bounds As Rectangle, ByVal backColor As Color)
        Dim bt As New Bitmap(bounds.Width, bounds.Height)
        Dim gr As Graphics = Graphics.FromImage(bt)
        ControlPaint.DrawImageDisabled(gr, image, 0, 0, backColor)
        gr.Dispose()
        g.DrawImage(bt, bounds, 0, 0, bounds.Width, bounds.Height, GraphicsUnit.Pixel, DisabledBlendAttributes)
        bt.Dispose()
    End Sub

    Public Sub DrawButtonHighlight(ByVal g As Graphics, ByVal bounds As Rectangle, ByVal dropDown As Boolean, ByVal highlightMode As HighlightMode)
        If highlightMode = highlightMode.Checked Then
            g.FillRectangle(CheckedBackground, RectangleF.op_Implicit(bounds))
            g.DrawRectangle(HotBorder, bounds)
        Else
            'Draw outer highlight
            If highlightMode = highlightMode.Pushed Then
                If dropDown Then
                    g.FillRectangle(HotBackground, RectangleF.op_Implicit(bounds))
                    bounds.Width -= 11
                    g.FillRectangle(PushedBackground, RectangleF.op_Implicit(bounds))
                    bounds.Width += 11
                Else
                    g.FillRectangle(PushedBackground, RectangleF.op_Implicit(bounds))
                End If
            Else
                g.FillRectangle(HotBackground, RectangleF.op_Implicit(bounds))
            End If
            g.DrawRectangle(HotBorder, bounds)

            'Dropdown outer ring
            If dropDown Then
                bounds.Offset(bounds.Width - 11, 0)
                bounds.Width -= (bounds.Width - 11)
                g.DrawRectangle(HotBorder, bounds)
            End If
        End If
    End Sub

    Private Sub CalculateColours()
        Dim background As Color

        'Dispose of existing brushes if necessary
        If Not _background Is Nothing Then
            DisposeBrushes()
        End If

        'Calculate colours
        background = InterpolateColours(SystemColors.Control, SystemColors.Window, 0.15)

        'Calculate colours
        _background = New SolidBrush(background)
        _separator = New Pen(InterpolateColours(SystemColors.ControlDark, SystemColors.Control, 0.39))
        _menuBackground = New SolidBrush(InterpolateColours(SystemColors.Window, SystemColors.Control, 0.22))
        _menuMargin = New SolidBrush(InterpolateColours(SystemColors.Window, SystemColors.Control, 0.8))

        CalculateHighlightColours(SystemColors.Highlight)
    End Sub

    Private Sub CalculateHighlightColours(ByVal baseColor As Color)
        Dim hotBackground As Color

        'Calculate colours
        hotBackground = InterpolateColours(baseColor, SystemColors.Window, 0.7)
        hotBackground = EnsureDarkness(_background.Color, hotBackground, 0.05)

        'Interpolate to create background brushes
        _hotBackground = New SolidBrush(hotBackground)
        _pushedBackground = New SolidBrush(InterpolateColours(baseColor, SystemColors.Window, 0.5))
        _checkedBackground = New SolidBrush(InterpolateColours(baseColor, SystemColors.Window, 0.85))

        _hotBorder = New Pen(baseColor)
    End Sub

    Private Function EnsureDarkness(ByVal Colour1 As Color, ByVal Colour2 As Color, ByVal Percentage As Single) As Color
        Dim b1, b2 As Single
        Dim i As Integer = 0

        b1 = Colour1.GetBrightness()
        b2 = Colour2.GetBrightness()

        If b2 > b1 - Percentage Then
            Colour2 = InterpolateColours(Colour2, Color.Black, 0.14)
        End If

        Return Colour2
    End Function

    Public Sub Dispose() Implements System.IDisposable.Dispose
        DisposeBrushes()

        'Dispose our image attributes
        _blendAttributes.Dispose()
        _disabledBlendAttributes.Dispose()
        _shadowBlendAttributes.Dispose()

        RemoveHandler Microsoft.Win32.SystemEvents.UserPreferenceChanged, AddressOf UserPreferenceChanged
    End Sub

    Private Sub DisposeBrushes()
        _background.Dispose()
        _hotBackground.Dispose()
        _checkedBackground.Dispose()
        _pushedBackground.Dispose()
        _separator.Dispose()
        _menuBackground.Dispose()
        _menuMargin.Dispose()
        _hotBorder.Dispose()
    End Sub

    Private Function InterpolateColours(ByVal Color1 As Color, ByVal Color2 As Color, ByVal Percentage As Single) As Color
        Dim r1, g1, b1, r2, g2, b2 As Integer
        Dim r3, g3, b3 As Byte

        r1 = Color1.R
        g1 = Color1.G
        b1 = Color1.B
        r2 = Color2.R
        g2 = Color2.G
        b2 = Color2.B

        r3 = Convert.ToByte(r1 + ((r2 - r1) * Percentage))
        g3 = Convert.ToByte(g1 + ((g2 - g1) * Percentage))
        b3 = Convert.ToByte(b1 + ((b2 - b1) * Percentage))

        Return Color.FromArgb(r3, g3, b3)
    End Function

End Class
