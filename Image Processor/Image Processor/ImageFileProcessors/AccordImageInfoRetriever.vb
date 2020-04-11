Imports Accord.Imaging
Imports Accord.Imaging.Formats
Imports Serilog

Namespace ImageFileProcessors
    Class AccordImageInfoRetriever
        Inherits BitmapImageInfoProcessor
        Public Sub New(ByVal filePath As String)
            MyBase.New(filePath)
        End Sub
        Public Overrides Function Process() As ImageFileProcessInfo
            Dim info As ImageInfo
            Using bmp = ImageDecoder.DecodeFromFile(FilePath, info)
                LogFileInfo(info)
                Return GetBitmapInfo(bmp)
            End Using
        End Function

        Sub LogFileInfo(info As ImageInfo)
            If info IsNot Nothing Then Log.Information("{file} Info {@info}", FilePath, info)
        End Sub
    End Class

End Namespace