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
</Query>

Sub Main
	Dim fit As New AForge.Imaging.Formats.FITSCodec

	Using strm As New IO.FileStream(IO.Path.Combine(IO.Path.GetDirectoryName(Util.CurrentQueryPath), "M101_SBIG.FIT"), FileMode.OpenOrCreate, FileAccess.Read, FileShare.Read)
		Dim sw As New Stopwatch
		sw.Start

		Using bmp As Bitmap = fit.DecodeSingleFrame(strm)
			Dim bitSize = Image.GetPixelFormatSize(bmp.PixelFormat)
			Dim pixelSize = bitSize \ 8
			'bitSize.Dump("Bits per pixel")
			'bmp.PixelFormat.ToString.Dump("Pixel format")
			Dim bmpData As BitmapData
			Try
				Dim rect As New Rectangle(0, 0, bmp.Width, bmp.Height)
				bmpData = bmp.LockBits(rect,
			   ImageLockMode.ReadWrite, bmp.PixelFormat)

				' Get the address of the first line.
				Dim ptr As IntPtr = bmpData.Scan0

				' Declare an array to hold the bytes of the bitmap.
				' This code is specific to a bitmap with 24 bits per pixels.
				Dim bytes As Integer = Math.Abs(bmpData.Stride) * bmp.Height
				Dim rgbValues(bytes - 1) As Byte

				' Copy the RGB values into the array.
				Marshal.Copy(ptr, rgbValues, 0, bytes)


				'Do stuff here with rgbvalues to either edit or process
				'convert bytes to pixels
				Dim pixels = (From pixel In rgbValues.Batch(pixelSize)
							  Select BitConverter.ToUInt32(pixel, 0)).ToArray

				Dim histData = pixels '.AsParallel


				Dim hist = (From d In histData
							Group By d Into HistG = Group
							Select New With {.Pixel = d, .Count = HistG.LongCount}).ToArray


				'hist.Dump

				My.Computer.FileSystem.WriteAllText(IO.Path.Combine(IO.Path.GetDirectoryName(Util.CurrentQueryPath), "Hist.txt"),
					String.Join(Environment.NewLine, hist.OrderByDescending(Function(h) h.Count).Select(Function(h) $"{h.Pixel}{vbTab}{h.Count}")), False)


				' Copy the RGB values back to the bitmap
				'System.Runtime.InteropServices.Marshal.Copy(rgbValues, 0, ptr, bytes)
			Catch
			Finally
				If bmpData IsNot Nothing Then bmp.UnlockBits(bmpData)

			End Try

		End Using
		sw.Stop
		sw.Dump


	End Using

End Sub



