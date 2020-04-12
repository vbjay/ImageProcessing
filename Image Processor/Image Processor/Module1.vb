Imports Image_Processor.ImageDataTasks
Imports MoreLinq
Imports Serilog
Imports System.IO
Imports System.Runtime.CompilerServices

Module Module1
    Sub Main(args As String())
        Log.Logger = New LoggerConfiguration().
             MinimumLevel.Verbose.
             WriteTo.File(
                    Path.Combine(My.Application.Info.DirectoryPath, "log.txt"),
                    rollingInterval:=RollingInterval.Day,
                    flushToDiskInterval:=TimeSpan.FromSeconds(3),
                    [shared]:=True).
             WriteTo().Console(restrictedToMinimumLevel:=Events.LogEventLevel.Information).
             CreateLogger()



        Dim pth As String
        If args.Length = 0 Then
            pth = My.Application.Info.DirectoryPath
        Else
            pth = args(0)
        End If
        Log.Information("Code version:{hash} Dirty? {dirty}", ThisAssembly.Git.Sha, ThisAssembly.Git.IsDirty)
        Log.Information("Folder to search {pth}", pth)
        Dim extensions As String() = ".fts;.fits;.fit;.tif".Split(";".ToCharArray) 'adjust to add any other file extensions that can be processed
        Dim files = Directory.GetFiles(pth).
                Where(Function(f) extensions.Contains(Path.GetExtension(f).ToLower)).ToArray

        Dim sw As New Stopwatch
        sw.Start()
        Dim allWorkTsk As Task(Of WorkInfo) = ProcessFiles(files)
        allWorkTsk.Wait()
        sw.Stop()
        DisplayMemoryInfo()
        Dim allWork = allWorkTsk.Result

        Log.Information("Work time(Sum of all actual work time):{HowLong}", allWork.WorkTime)
        Log.Information("Work Time by Task Type: {@Times}", allWork.Times)
        Log.Information("Run time(Actual time took to run all work):{HowLong}", sw.Elapsed)
        Log.Information("Time saved {Time}", allWork.WorkTime.Subtract(sw.Elapsed))




    End Sub


    Public Function BytesToString(ByVal byteCount As Long) As String
        Dim suf As String() = {"B", "KB", "MB", "GB", "TB", "PB", "EB"}
        If byteCount = 0 Then Return $"0{suf(0)}"
        Dim bytes As Long = Math.Abs(byteCount)
        Dim place As Integer = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)))
        Dim num As Double = Math.Round(bytes / Math.Pow(1024, place), 1)
        Return (Math.Sign(byteCount) * num).ToString() & suf(place)
    End Function

    Private Sub DisplayMemoryInfo()
        Log.Information("Current working set: {Size}", BytesToString(Environment.WorkingSet))
        Log.Information("Current private bytes: {Size}", BytesToString(Process.GetCurrentProcess.PrivateMemorySize64))
    End Sub
    Class WorkInfo
        Property WorkTime As TimeSpan
        Property Times As IEnumerable(Of TypeTime)
    End Class
    Class TypeTime
        Property TaskType As Type
        Property Time As TypeTimeInfo
    End Class
    Structure TypeTimeInfo
        Property Ticks As Long
        Property Count As Integer
        ReadOnly Property AverageTaskTime As TimeSpan
            Get
                If Ticks = 0 Then Return TimeSpan.Zero
                Return TimeSpan.FromTicks(Ticks \ Count)
            End Get
        End Property
        ReadOnly Property Time As TimeSpan
            Get
                Return TimeSpan.FromTicks(Ticks)
            End Get
        End Property
    End Structure
    Private Async Function ProcessFiles(files() As String) As Task(Of WorkInfo)
        Return Await Task.Run(Async Function()
                                  Dim workTime As TimeSpan = TimeSpan.Zero
                                  Dim InfoByType As New Dictionary(Of Type, TypeTimeInfo)
                                  Log.Information("Batch Size:{Size}", My.Settings.BatchSize)
                                  For Each btch In files.Select(Function(fl) New FilePathDataTask(fl)).Batch(My.Settings.BatchSize)
                                      Dim sw As New Stopwatch
                                      Dim id As Guid = Guid.NewGuid
                                      Log.Debug("Batch Start {id}-{count} tasks", id, btch.Count)
                                      sw.Start()
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
                                          Dim tm As TypeTimeInfo
                                          InfoByType.TryGetValue(i.TaskType, tm)
                                          tm.Ticks += i.Time.Ticks
                                          tm.Count += 1
                                          InfoByType(i.TaskType) = tm

                                          Log.Information("Task finished: {Description}({ID})-{Time}- {Status}- {SourceFile}", i.Description, i.ID, i.Time, i.Status, i.SourceFilePath)
                                      Next

                                      infos.Clear()
                                      tsks = Array.Empty(Of Task(Of ImageDataTaskInfo))

                                      DisplayMemoryInfo()

                                      sw.Stop()
                                      Log.Debug("Batch End {id}-{time} tasks", id, sw.Elapsed)
                                  Next

                                  Return New WorkInfo With {
                                  .WorkTime = workTime,
                                  .Times = InfoByType.Select(Function(t) New TypeTime With {.TaskType = t.Key, .Time = t.Value}).ToArray
                                  }
                              End Function)
    End Function

    Sub DoCleanup(tasks As IEnumerable(Of ImageDataTask))
        Dim sw As New Stopwatch
        Log.Debug("Cleaning up batch data")
        sw.Start()
        For Each tsk In tasks
            tsk.Cleanup()
        Next
        sw.Stop()
        Log.Debug("Completed cleaning up batch data {time}", sw.Elapsed)
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
