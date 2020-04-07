
Imports Image_Processor.ImageFileProcessors
Imports MoreLinq

Namespace ImageDataTasks
    Public Class HistogramData
        Property Count As Long
        Property Pixel As UInteger
    End Class

    Public Class HistogramDataTask
        Inherits ImageDataTask(Of IEnumerable(Of HistogramData), ImageFileProcessInfo)
        Sub New(ByVal Info As ImageFileProcessInfo, SourceFilePath As String)
            MyBase.New(Info, $"Convert {NameOf(ImageFileProcessInfo)} to {NameOf(IEnumerable(Of HistogramData))}", SourceFilePath)
        End Sub

        Protected Overrides Iterator Function GenerateChildeSteps() As IEnumerable(Of ImageDataTask)
            Yield New HistogramDataWriterTask(TaskInfo.Result, TaskInfo.SourceFilePath)

        End Function

        Protected Overrides ReadOnly Property Work As Func(Of ImageFileProcessInfo, IEnumerable(Of HistogramData))
            Get
                Return Function(info)
                           Dim pixels = From pixel In info.Bytes.Batch(info.PixelSize).AsParallel.WithExecutionMode(ParallelExecutionMode.ForceParallelism)
                                        Select BitConverter.ToUInt32(pixel.Concat(Enumerable.Range(0, 4 - pixel.Count).Select(Function(n) New Byte)).ToArray, 0)

                           Return (From p In pixels
                                   Group By p Into HistG = Group
                                   Select New HistogramData With {.Pixel = p, .Count = HistG.LongCount}).ToArray
                       End Function
            End Get
        End Property
    End Class
End Namespace