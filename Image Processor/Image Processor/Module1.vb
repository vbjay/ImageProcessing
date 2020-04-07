Imports Image_Processor.ImageDataTasks
Imports MoreLinq
Imports System.IO
Imports System.Runtime.CompilerServices

Module Module1
    Sub Main(args As String())
        Dim pth As String
        If args.Length = 0 Then
            pth = My.Application.Info.DirectoryPath
        Else
            pth = args(0)
        End If
        Dim extensions As String() = ".fts;.fits;.fit".Split(";".ToCharArray) 'adjust to add any other file extensions that can be processed
        Dim files = Directory.GetFiles(pth).
                Where(Function(f) extensions.Contains(Path.GetExtension(f).ToLower)).ToArray

        Dim sw As New Stopwatch
        sw.Start()
        Dim allWorkTsk As Task(Of TimeSpan) = ProcessFiles(files)
        allWorkTsk.Wait()
        sw.Stop()
        DisplayMemoryInfo()
        Dim allWork = allWorkTsk.Result

        Console.WriteLine($"Work time:{allWork}")
        Console.WriteLine($"Run time:{sw.Elapsed}")
        Task.Delay(5000).Wait()
        DisplayMemoryInfo()
        Console.ReadKey()

    End Sub


    Private Function BytesToString(ByVal byteCount As Long) As String
        Dim suf As String() = {"B", "KB", "MB", "GB", "TB", "PB", "EB"}
        If byteCount = 0 Then Return $"0{suf(0)}"
        Dim bytes As Long = Math.Abs(byteCount)
        Dim place As Integer = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)))
        Dim num As Double = Math.Round(bytes / Math.Pow(1024, place), 1)
        Return (Math.Sign(byteCount) * num).ToString() & suf(place)
    End Function

    Private Sub DisplayMemoryInfo()
        Console.WriteLine($"Current working set: {BytesToString(Environment.WorkingSet)}")
        Console.WriteLine($"Current private bytes: {BytesToString(Process.GetCurrentProcess.PrivateMemorySize64)}")
    End Sub

    Private Async Function ProcessFiles(files() As String) As Task(Of TimeSpan)
        Return Await Task.Run(Async Function()
                                  Dim workTime As TimeSpan = TimeSpan.Zero
                                  For Each btch In files.Select(Function(fl) New FilePathDataTask(fl)).Batch(My.Settings.BatchSize)
                                      Dim infos As New List(Of ImageDataTaskInfo)
                                      Dim tsks = btch.Select(Function(t) t.Start).ToArray
                                      Task.WaitAll(tsks)
                                      For Each tsk In tsks
                                          Dim info = Await tsk
                                          infos.Add(info)

                                          Dim otherInfos = Await WaitTasks(info.ChildTasks)
                                          infos.AddRange(otherInfos)
                                      Next
                                      For Each i In infos
                                          workTime += i.Time
                                          Console.WriteLine($"{i.Description}-{i.Time}")
                                      Next
                                      infos.Clear()
                                      tsks = Array.Empty(Of Task(Of ImageDataTaskInfo))

                                      DisplayMemoryInfo()

                                  Next

                                  Return workTime
                              End Function)
    End Function

    Sub DoCleanup(tasks As IEnumerable(Of ImageDataTask))
        For Each tsk In tasks
            tsk.Cleanup()
        Next
    End Sub


    <Extension>
    Function ToBasicInfo(Info As ImageDataTaskInfo) As BasicTaskInfo
        Return New BasicTaskInfo With {
        .Description = Info.Description,
        .Time = Info.Time
        }
    End Function
    Async Function WaitTasks(ChildTasks As IEnumerable(Of ImageDataTask)) As Task(Of IEnumerable(Of ImageDataTaskInfo))
        Dim cleanup As New Queue(Of ImageDataTask)(ChildTasks)
        Dim infos As New List(Of ImageDataTaskInfo)
        If Not ChildTasks.Any Then Await Task.CompletedTask
        Dim rec = ChildTasks.Select(Function(t) t.Start).ToArray
        Task.WaitAll(rec)

        If rec.Any Then

            Dim children As New List(Of ImageDataTask)
            For Each t In rec
                Dim info = Await t
                infos.Add(info)
                If info.ChildTasks.Any Then
                    children.AddRange(info.ChildTasks)
                End If
            Next
            rec = Array.Empty(Of Task(Of ImageDataTaskInfo))
            Dim others = Await WaitTasks(children)
            infos.AddRange(others)
            DoCleanup(cleanup)
        End If
        Return infos
    End Function

    Class BasicTaskInfo
        Property Description As String
        Property Time As TimeSpan

    End Class
End Module
