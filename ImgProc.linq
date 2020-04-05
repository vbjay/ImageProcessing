<Query Kind="VBProgram">
  <Reference>&lt;RuntimeDirectory&gt;\Microsoft.VisualBasic.dll</Reference>
  <NuGetReference>AForge.Imaging</NuGetReference>
  <NuGetReference>AForge.Imaging.Formats</NuGetReference>
  <NuGetReference>morelinq</NuGetReference>
  <Namespace>Microsoft.VisualBasic</Namespace>
  <Namespace>MoreLinq</Namespace>
  <Namespace>System.Drawing</Namespace>
  <Namespace>System.Drawing.Imaging</Namespace>
  <Namespace>System.Runtime.InteropServices</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
</Query>

Async Sub Main
	Dim times As New List(Of TimeSpan)
	Dim batchSize As Integer = 3 ' play with this number between 1 and higher

	Dim files = IO.Directory.EnumerateFiles(IO.Path.GetDirectoryName(Util.CurrentQueryPath), "*.fit").Batch(batchSize)
	Dim sw As New Stopwatch
	sw.Start
	For Each batch In files
		Dim tasks = batch.Select(Function(f) ProcessFile(f)).ToArray.Dump
		Task.WaitAll(tasks)
		For Each ts In tasks
			Await ts
		Next


		Dim infos = tasks.Select(Function(t) t.Result)
		Dim infoTasks = infos.Select(Function(i) i.GetHistogramData).ToArray.Dump
		Task.WaitAll(infoTasks)
		For Each ts In infoTasks
			Await ts
		Next

		Dim infoResults = infoTasks.Select(Function(i) i.Result)
		times.AddRange(infos.Select(Function(i) i.Time).Concat(infoResults.Select(Function(i) i.Time)))





	Next

	sw.Stop
	sw.Dump("How long it took to process")

	Dim total As TimeSpan = TimeSpan.Zero
	For Each time In times
		total += time
	Next
	total.Dump("Total time for work")

	Dim savings = total - sw.Elapsed

	savings.Dump("Time diff")

End Sub


Async Function ProcessFile(fl As String) As Task(Of FileProcessInfo)
	Return Await Task.Run(
	Function()
		Dim fit As New AForge.Imaging.Formats.FITSCodec
		Dim pth As String = IO.Path.Combine(IO.Path.GetDirectoryName(Util.CurrentQueryPath), $"{IO.Path.GetFileNameWithoutExtension(fl)}-Hist.txt")
		Dim info As FileProcessInfo
		Using strm As New IO.FileStream(fl, FileMode.OpenOrCreate, FileAccess.Read, FileShare.Read)
			Dim sw As New Stopwatch
			sw.Start

			Using bmp As Bitmap = fit.DecodeSingleFrame(strm)
				'bmp.Dump
				Dim bitSize = Image.GetPixelFormatSize(bmp.PixelFormat)
				Dim pixelSize = bitSize \ 8

				'fl.Dump
				Dim format = bmp.PixelFormat.ToString

				info = New FileProcessInfo With {
						   .File = fl,
						   .BitSize = bitSize,
						   .Format = format,
						   .PixelSize = pixelSize,
						   .DataFile = pth}
				Dim bmpData As BitmapData
				Try
					Dim rect As New Rectangle(0, 0, bmp.Width, bmp.Height)
					bmpData = bmp.LockBits(rect,
						ImageLockMode.ReadWrite, bmp.PixelFormat)

					' Get the address of the first line.
					Dim ptr As IntPtr = bmpData.Scan0

					' Declare an array to hold the bytes of the bitmap.
					' This code is specific to a bitmap with 24 bits per pixels.
					info.ByteCount = Math.Abs(bmpData.Stride) * bmp.Height
					Dim rgbValues(info.ByteCount - 1) As Byte

					' Copy the RGB values into the array.
					Marshal.Copy(ptr, rgbValues, 0, info.ByteCount)
					info.bytes = rgbValues

				Catch ex As Exception
					ex.Dump
				Finally
					If bmpData IsNot Nothing Then bmp.UnlockBits(bmpData)

				End Try
				sw.Stop
				info.Time = sw.Elapsed
			End Using
		End Using

		Return info
	End Function)


End Function

Public Shared Sub Test48BPP(ByVal FileName As String)
	Dim b16bpp As New Bitmap(5000, 5000, System.Drawing.Imaging.PixelFormat.Format16bppGrayScale)

	Dim rect As New Rectangle(0, 0, b16bpp.Width, b16bpp.Height)
	Dim bitmapData As System.Drawing.Imaging.BitmapData = b16bpp.LockBits(rect, Imaging.ImageLockMode.WriteOnly, b16bpp.PixelFormat)
	'Calculate the number of bytes required And allocate them.
	Dim BytePerPixel As Integer = Image.GetPixelFormatSize(b16bpp.PixelFormat) \ 8

	Dim bitmapBytes((b16bpp.Width * b16bpp.Height * BytePerPixel) - 1) As Byte
	'Fill the bitmap bytes with random data.
	Dim Rnd As New Random
	Dim RunIdx As Long = 0
	For x As Integer = 0 To b16bpp.Width - 1
		For y As Integer = 0 To b16bpp.Height - 1
			Dim PixelIdx As Integer = (y * b16bpp.Width * BytePerPixel) + (x * BytePerPixel)
			Dim BitPatternR As Byte() = BitConverter.GetBytes(CType(Rnd.Next(0, UInt16.MaxValue + 1), UInt16))
			Dim BitPatternG As Byte() = BitConverter.GetBytes(CType(Rnd.Next(0, UInt16.MaxValue + 1), UInt16))
			Dim BitPatternB As Byte() = BitConverter.GetBytes(CType(Rnd.Next(0, UInt16.MaxValue + 1), UInt16))
			bitmapBytes(PixelIdx + 0) = BitPatternR(0)
			bitmapBytes(PixelIdx + 1) = BitPatternR(1)
			bitmapBytes(PixelIdx + 2) = BitPatternG(0)
			bitmapBytes(PixelIdx + 3) = BitPatternG(1)
			bitmapBytes(PixelIdx + 4) = BitPatternB(0)
			bitmapBytes(PixelIdx + 5) = BitPatternB(1)
			RunIdx += 1
		Next y
	Next x
	'Copy the randomized bits to the bitmap pointer.
	Dim Pointer As IntPtr = bitmapData.Scan0
	Runtime.InteropServices.Marshal.Copy(bitmapBytes, 0, Pointer, bitmapBytes.Length)
	'Unlock the bitmap, we're all done.
	b16bpp.UnlockBits(bitmapData)
	b16bpp.Save(FileName, Imaging.ImageFormat.Png)
End Sub

Class HistogramInfo
	Property Time As TimeSpan
	Property Histogram As DumpContainer
End Class
Class HistogramData
	Property Pixel As UInt32
	Property Count As Long
End Class
Class FileProcessInfo
	Property File As String
	Property Time As timespan
	Property BitSize As Integer
	Property Format As String
	Property PixelSize As Integer
	Property ByteCount As Long
	Property DataFile As String
	Property Bytes As Byte()

	Function GetHistogramData() As task(Of HistogramInfo)
		'Do stuff here with rgbvalues to either edit or process
		'convert bytes to pixels
		Return Task.Run(Function()

							Dim sw As New Stopwatch
							sw.Start
							Dim pixels = From pixel In bytes.Batch(PixelSize).AsParallel.WithExecutionMode(ParallelExecutionMode.ForceParallelism)
										 Select BitConverter.ToUInt32(pixel.Concat(enumerable.range(0, 4 - pixel.Count).Select(Function(n) New Byte)).ToArray, 0)




							Dim result = (From p In pixels
										  Group By p Into HistG = Group
										  Select New HistogramData With {.Pixel = p, .Count = HistG.LongCount}).ToArray

							sw.Stop
							Return New HistogramInfo With {.Time = sw.Elapsed, .Histogram = util.OnDemand("Click to see HistogramData", Function() result.OrderByDescending(Function(d) d.Count))}


						End Function)

		'hist.Dump


	End Function

End Class
