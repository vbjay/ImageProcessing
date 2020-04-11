Imports System.Drawing
Imports System.Drawing.Imaging
Imports System.Runtime.InteropServices

Namespace ImageFileProcessors
    Public Class BitmapImageInfoProcessor
        Inherits ImageInfoRetriever
        Sub New(ByVal filePath As String)
            MyBase.New(filePath)
        End Sub

        Protected Function GetBitmapInfo(bmp As Bitmap) As ImageFileProcessInfo

            Dim info = New ImageFileProcessInfo With {
                               .File = FilePath,
                               .BitSize = Image.GetPixelFormatSize(bmp.PixelFormat),
                               .Format = bmp.PixelFormat.ToString,
                               .PixelSize = Image.GetPixelFormatSize(bmp.PixelFormat) \ 8}
            Dim bmpData As BitmapData = Nothing
            Try
                Dim rect As New Rectangle(0, 0, bmp.Width, bmp.Height)
                bmpData = bmp.LockBits(rect,
                            ImageLockMode.ReadWrite, bmp.PixelFormat)

                ' Get the address of the first line.
                Dim ptr As IntPtr = bmpData.Scan0

                ' Declare an array to hold the bytes of the bitmap.
                ' This code is specific to a bitmap with 24 bits per pixels.
                info.Stride = Math.Abs(bmpData.Stride)
                info.ByteCount = info.Stride * bmp.Height
                Dim rgbValues(info.ByteCount - 1) As Byte

                ' Copy the RGB values into the array.
                Marshal.Copy(ptr, rgbValues, 0, info.ByteCount)
                info.Bytes = rgbValues

            Catch ex As Exception
                info.Succeded = False
            Finally
                If bmpData IsNot Nothing Then bmp.UnlockBits(bmpData)
            End Try
            Return info
        End Function

        Overrides Function Process() As ImageFileProcessInfo
            Using bmp = Bitmap.FromFile(FilePath)
                Return GetBitmapInfo(bmp)
            End Using
        End Function
    End Class
End Namespace
