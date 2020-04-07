Imports System.IO

Namespace ImageFileProcessors
    Public Class FileProcessorFactory
        Private Sub New()

        End Sub

        Shared Function GetImageProcessor(FilePath As String) As FileProcessor(Of ImageFileProcessInfo)
            Dim ext As String = Path.GetExtension(FilePath)
            If String.IsNullOrWhiteSpace(ext) Then Return Nothing
            ext = ext.Trim(".").ToLower
            Dim proc As FileProcessor(Of ImageFileProcessInfo) = Nothing
            Select Case ext
                Case "fit", "fits", "fts"
                    proc = New FITSImageInfoRetriever(FilePath)
            End Select
            Return proc
        End Function

    End Class
End Namespace