Namespace ImageDataTasks
    Public MustInherit Class ImageDataTask
        Protected MustOverride Iterator Function GenerateChildeSteps() As IEnumerable(Of ImageDataTask)

        Overridable Sub Cleanup()

        End Sub
        MustOverride Async Function Start() As Task(Of ImageDataTaskInfo)
    End Class

    Public MustInherit Class ImageDataTask(Of TResult, TData)
        Inherits ImageDataTask

        Dim _data As TData

        Sub New(Info As TData, Description As String, SourceFilePath As String)
            TaskInfo = New ImageDataTaskInfo(Of TResult)(Description, SourceFilePath) With {.TaskType = Me.GetType}
            Data = Info

        End Sub

        Protected Overridable Async Function Process() As Task(Of TResult)
            Dim result As TResult
            result = Await Task.Run(Function()
                                        Return Work(Data)
                                    End Function)
            Return result
        End Function

        Protected MustOverride ReadOnly Property Work As Func(Of TData, TResult)

        Overrides Sub Cleanup()
            MyBase.Cleanup()
            If Data IsNot Nothing Then
                If GetType(IDisposable).IsAssignableFrom(Data.GetType) Then DirectCast(Data, IDisposable).Dispose()
                Data = Nothing
            End If


            If TaskInfo.Result IsNot Nothing Then
                If GetType(IDisposable).IsAssignableFrom(TaskInfo.Result.GetType) Then DirectCast(TaskInfo.Result, IDisposable).Dispose()
                TaskInfo.Result = Nothing
            End If


        End Sub
        Overrides Async Function Start() As Task(Of ImageDataTaskInfo)
            If TaskInfo.Status <> ImageDataTaskStatus.NotStarted Then Await Task.CompletedTask
            Dim sw As New Stopwatch
            sw.Start()
            TaskInfo.Status = ImageDataTaskStatus.Running
            Try
                Dim result As TResult = Await Process()
                TaskInfo.Result = result
                TaskInfo.Status = ImageDataTaskStatus.Success
                TaskInfo.ChildTasks = GenerateChildeSteps().ToArray
            Catch ex As Exception
                TaskInfo.Result = Nothing
                TaskInfo.Status = ImageDataTaskStatus.Failed
                TaskInfo.Description = $"{TaskInfo.Description}-{ex.Message}"
                TaskInfo.ChildTasks = Enumerable.Empty(Of ImageDataTask)
            Finally
                sw.Stop()
                TaskInfo.Time = sw.Elapsed
            End Try
            Return TaskInfo
        End Function

        Property Data As TData
            Get
                Return _data
            End Get
            Private Set
                _data = Value
            End Set
        End Property
        ReadOnly Property TaskInfo As ImageDataTaskInfo(Of TResult)
    End Class
End Namespace