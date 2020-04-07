Namespace ImageDataTasks
    Public Enum ImageDataTaskStatus
        NotStarted = 0
        Running
        Success
        Failed
    End Enum

    Public Class ImageDataTaskInfo
        Property ChildTasks As IEnumerable(Of ImageDataTask)
        Property Description As String
        ReadOnly Property ID As Guid = Guid.NewGuid
        Property SourceFilePath As String
        Property Status As ImageDataTaskStatus
        Property Time As TimeSpan = TimeSpan.Zero
        Property TaskType As Type

    End Class

    Public Class ImageDataTaskInfo(Of T)
        Inherits ImageDataTaskInfo

        Sub New(ByVal description As String, SourceFilePath As String)
            Me.Description = description
            Me.SourceFilePath = SourceFilePath
        End Sub

        Property Result As T
    End Class
End Namespace