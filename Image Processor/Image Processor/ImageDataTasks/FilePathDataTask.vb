Imports Image_Processor.ImageFileProcessors

Namespace ImageDataTasks
    Public Class FilePathDataTask
        Inherits ImageDataTask(Of ImageFileProcessInfo, String)
        Sub New(ByVal Info As String)
            MyBase.New(Info, $"Convert file path to {NameOf(ImageFileProcessInfo)}", Info)
        End Sub

        Protected Overrides Iterator Function GenerateChildeSteps() As IEnumerable(Of ImageDataTask)
            Yield New HistogramDataTask(TaskInfo.Result, TaskInfo.SourceFilePath)
        End Function

        Protected Overrides ReadOnly Property Work As Func(Of String, ImageFileProcessInfo)
            Get
                Return Function(filepath)
                           Dim proc = FileProcessorFactory.GetImageProcessor(filepath)
                           If proc Is Nothing Then Return Nothing
                           Return proc.Process()
                       End Function

            End Get
        End Property
    End Class
End Namespace