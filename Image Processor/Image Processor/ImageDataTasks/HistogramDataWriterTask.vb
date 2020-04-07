Imports System.IO

Namespace ImageDataTasks
    Public Class HistogramDataWriterTask
        Inherits ImageDataTask(Of String, IEnumerable(Of HistogramData))
        Sub New(ByVal Info As IEnumerable(Of HistogramData), ByVal SourceFilePath As String)
            MyBase.New(Info, "Write Histogram data to file", SourceFilePath)
        End Sub

        Protected Overrides Function GenerateChildeSteps() As IEnumerable(Of ImageDataTask)
            Return Enumerable.Empty(Of ImageDataTask)
        End Function

        Protected Overrides ReadOnly Property Work As Func(Of IEnumerable(Of HistogramData), String)
            Get
                Return Function(data)
                           Dim pth = Path.Combine(Path.GetDirectoryName(TaskInfo.SourceFilePath), $"{Path.GetFileNameWithoutExtension(TaskInfo.SourceFilePath)}-hist.txt")
                           TaskInfo.Description &= $" - {pth}"
                           Dim output = data.Select(Function(d) $"{d.Pixel}{vbTab}{d.Count}")
                           My.Computer.FileSystem.WriteAllText(pth, String.Join(Environment.NewLine, output), False)
                           Return pth
                       End Function
            End Get
        End Property
    End Class
End Namespace