<Query Kind="VBProgram">
  <Reference>&lt;RuntimeDirectory&gt;\Microsoft.VisualBasic.dll</Reference>
  <Namespace>Microsoft.VisualBasic</Namespace>
  <Namespace>System.Drawing</Namespace>
  <Namespace>System.Drawing.Imaging</Namespace>
  <Namespace>System.Runtime.InteropServices</Namespace>
</Query>

Sub Main
	Using bmp As Bitmap = Bitmap.FromFile(IO.Path.Combine(io.Path.GetDirectoryName(Util.CurrentQueryPath), "cat.jpg"))
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

			' Copy the RGB values back to the bitmap
			System.Runtime.InteropServices.Marshal.Copy(rgbValues, 0, ptr, bytes)
		Catch
		Finally
			If bmpData IsNot Nothing Then bmp.UnlockBits(bmpData)

		End Try

	End Using



End Sub

	

'''<summary>Class to calculate 2D matrix statistics multi-threaded.</summary>
'''<remarks>Calculation as for UInt16 is not possible as a vector with all entries would be 2^32 entries long.</remarks>
Public Class StatMultiThread_UInt32

    Private Const OneUInt64 As UInt64 = CType(1, UInt64)

    '''<summary>The real image data.</summary>
    Public ImageData(,) As UInt32

    '''<summary>Object for each thread.</summary>
	Public Class StateObj
        Friend XOffset As Integer = -1
		Friend YOffset As Integer = -1
		Friend HistDataBayer As New Collections.Generic.Dictionary(Of Int64, UInt64)
		Friend Done As Boolean = False
	End Class

	'''<summary>Perform a calculation with 4 threads, one for each bayer channel.</summary>
	Public Sub Calculate(ByRef Results(,) As StateObj)

		'Data are processed
		Dim StObj(3) As StateObj
		For Idx As Integer = 0 To StObj.GetUpperBound(0)
			StObj(Idx) = New StateObj
		Next Idx
		StObj(0).XOffset = 0 : StObj(0).YOffset = 0
		StObj(1).XOffset = 0 : StObj(1).YOffset = 1
		StObj(2).XOffset = 1 : StObj(2).YOffset = 0
		StObj(3).XOffset = 1 : StObj(3).YOffset = 1

		'Start all threads
		For Each Slice As StateObj In StObj
			System.Threading.ThreadPool.QueueUserWorkItem(New System.Threading.WaitCallback(AddressOf HistoCalc), Slice)
		Next Slice

		'Join all threads
		Do
			'System.Threading.Thread.Sleep(1)
			Dim AllDone As Boolean = True
			For Each Slice As StateObj In StObj
				If Slice.Done = False Then
					AllDone = False : Exit For
				End If
			Next Slice
			If AllDone Then Exit Do
		Loop Until 1 = 0

		'Collect all results
		ReDim Results(1, 1)
		Results(0, 0) = StObj(0)
		Results(0, 1) = StObj(1)
		Results(1, 0) = StObj(2)
		Results(1, 1) = StObj(3)

	End Sub

	'''<summary>Histogramm calculation itself - the histogram of one bayer channel is calculated.</summary>
	Private Sub HistoCalc(ByVal Arguments As Object)

		Dim StateObj As StateObj = CType(Arguments, StateObj)
		StateObj.Done = False

		'Count one bayer part
		StateObj.HistDataBayer = New Collections.Generic.Dictionary(Of Int64, UInt64)
		For IdxX As Integer = StateObj.XOffset To ImageData.GetUpperBound(0) - 1 + StateObj.XOffset Step 2
			For IdxY As Integer = StateObj.YOffset To ImageData.GetUpperBound(1) - 1 + StateObj.YOffset Step 2
				Dim PixelValue As UInt32 = ImageData(IdxX, IdxY)
				If StateObj.HistDataBayer.ContainsKey(PixelValue) = False Then
					StateObj.HistDataBayer.Add(PixelValue, OneUInt64)
				Else
					StateObj.HistDataBayer(PixelValue) += OneUInt64
				End If
			Next IdxY
		Next IdxX

		StateObj.Done = True

	End Sub

End Class