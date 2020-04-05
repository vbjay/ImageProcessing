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