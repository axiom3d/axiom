Imports System.ComponentModel
Imports System.Drawing.Design

Friend Class ButtonIconEditor
    Inherits UITypeEditor

    Private imageEditor As UITypeEditor
    Private il As ImageList

    Public Sub New()
        imageEditor = DirectCast(TypeDescriptor.GetEditor(GetType(Image), GetType(UITypeEditor)), UITypeEditor)
    End Sub

    Public Overloads Overrides Function GetPaintValueSupported(ByVal context As System.ComponentModel.ITypeDescriptorContext) As Boolean
        Return imageEditor.GetPaintValueSupported(context)
    End Function

    Public Overloads Overrides Sub PaintValue(ByVal e As System.Drawing.Design.PaintValueEventArgs)
        Dim i As Integer

        'Validate context
        If il Is Nothing Then Exit Sub
        If il.Images.Count = 0 Then Exit Sub

        If Not (imageEditor Is Nothing) And TypeOf e.Value Is Integer Then
            i = DirectCast(e.Value, Integer)
            If i >= 0 And i <= il.Images.Count - 1 Then
                imageEditor.PaintValue(New PaintValueEventArgs(e.Context, il.Images(i), e.Graphics, e.Bounds))
            End If
        End If
    End Sub

    Public Overloads Overrides Function GetEditStyle(ByVal context As System.ComponentModel.ITypeDescriptorContext) As System.Drawing.Design.UITypeEditorEditStyle
        il = DirectCast(context.Instance, OutlookBarButton).ImageList

        Return imageEditor.GetEditStyle(context)
    End Function
End Class