﻿Imports Accord.Imaging.Formats
Imports System.Drawing
Imports System.IO

Namespace ImageFileProcessors
    Public Class FITSImageInfoRetriever
        Inherits BitmapImageInfoProcessor
        Sub New(ByVal filePath As String)
            MyBase.New(filePath)
        End Sub

        Overrides Function Process() As ImageFileProcessInfo


            Dim fit As New FITSCodec
            Dim info As ImageFileProcessInfo
            Using strm As New FileStream(FilePath, FileMode.OpenOrCreate, FileAccess.Read, FileShare.Read)
                Using bmp As Bitmap = fit.DecodeSingleFrame(strm)
                    info = GetBitmapInfo(bmp)
                End Using
            End Using
            Return info
        End Function
    End Class
End Namespace