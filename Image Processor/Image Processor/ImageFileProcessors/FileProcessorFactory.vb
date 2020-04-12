Imports System.IO
Imports Serilog

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
                Case "bmp", "fit", "fits", "tif", "tiff", "jpg", "jpeg", "png", "pbm", "pgm", "pnm", "ppm", "gif"
                    proc = New AccordImageInfoRetriever(FilePath)
            End Select
            Log.Debug("Used {type} to process the file- {file}", proc.GetType.FullName, FilePath)
            Return proc
        End Function

    End Class
End Namespace