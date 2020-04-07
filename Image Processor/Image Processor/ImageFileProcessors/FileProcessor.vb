Namespace ImageFileProcessors
    Public MustInherit Class FileProcessor(Of T)
        Sub New(ByVal filePath As String)
            Me.FilePath = filePath
        End Sub

        MustOverride Function Process() As T
        ReadOnly Property FilePath As String

    End Class

    Public MustInherit Class ImageInfoRetriever
        Inherits FileProcessor(Of ImageFileProcessInfo)

        Sub New(ByVal filePath As String)
            MyBase.New(filePath)
        End Sub
    End Class
End Namespace