Imports Image_Processor.ImageFileProcessors
Imports Serilog

Namespace ImageDataTasks
    Public Class FileInfoLogger
        Inherits ImageDataTask(Of Integer, ImageFileProcessInfo)
        Public Sub New(ByVal Info As ImageFileProcessInfo, ByVal SourceFilePath As String)
            MyBase.New(Info, "Log info about file", SourceFilePath)
        End Sub
        Protected Overrides ReadOnly Property Work As Func(Of ImageFileProcessInfo, Integer)
            Get
                Return Function(info)
                           Log.Information("File size: {size}- {file}", BytesToString(info.ByteCount), TaskInfo.SourceFilePath)
                           Return 0
                       End Function
            End Get
        End Property

        Protected Overrides Function GenerateChildeSteps() As IEnumerable(Of ImageDataTask)
            Return Enumerable.Empty(Of ImageDataTask)
        End Function
    End Class
End Namespace