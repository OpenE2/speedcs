Imports System.IO

Public Class clsCWlog
    Public cwLogChannels As New SortedList(Of UInt32, clsLogChannel)

    Public Sub WriteCWlog(ByVal Message() As Byte)
        Dim key As UInt32

        Using ms As New MemoryStream
            ms.Write(Message, 8, 8)
            key = BitConverter.ToUInt32(clsCRC32.CRC32OfByte(ms.ToArray), 0)
        End Using

        Dim logChannel As clsLogChannel

        If cwLogChannels.ContainsKey(key) Then
            logChannel = TryCast(cwLogChannels(key), clsLogChannel)
        Else
            logChannel = New clsLogChannel
            logChannel.iSRVID = GetLittleEndian(BitConverter.ToUInt16(Message, 8))
            logChannel.iCAID = GetLittleEndian(BitConverter.ToUInt16(Message, 10))
            logChannel.iPROVID = GetLittleEndian(BitConverter.ToUInt32(Message, 12))
            cwLogChannels.Add(key, logChannel)
            logChannel.SetEnv()
        End If

        logChannel.ResolveCMD1(Message)
    End Sub


End Class

Public Class clsLogChannel
    Private filepath As String = Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), Application.ProductName)
    Private lastOddChecksum As UInt32
    Private lastOddTimestamp As Date
    Private lastEvenChecksum As UInt32
    Private lastEvenTimestamp As Date
    Private actualFilePath As String
    Private actualFileName As String

    Public iSRVID As UInt16
    Public iCAID As UInt16
    Public iPROVID As UInt32

    Public Sub New()

    End Sub

    Public Sub SetEnv()
        Dim d As Date = Date.Now
        actualFilePath = Path.Combine(filepath, "CWL-" & d.Year & d.Month & d.Day)
        If Not Directory.Exists(actualFilePath) Then Directory.CreateDirectory(actualFilePath)

        actualFileName = d.Year & d.Month & d.Day & "-C" & _
                            iCAID.ToString("X4") & "-Ixxxx-" & _
                            Services.GetServiceInfo(iCAID.ToString("X4") & ":" & iSRVID.ToString("X4")).Name & _
                            ".cwl"

        actualFileName = Path.Combine(actualFilePath, actualFileName)
        WriteHeaderToFile()
    End Sub

    Public Sub ResolveCMD1(ByVal Message() As Byte)

        Using ms As New MemoryStream
            ms.Write(Message, 20, 8)
            Dim actOddChecksum As UInt32 = BitConverter.ToUInt32(clsCRC32.CRC32OfByte(ms.ToArray), 0)
            If Not actOddChecksum = lastOddChecksum Then
                WriteCWtoFile(0, ms.ToArray)
                lastOddChecksum = actOddChecksum
            End If
        End Using

        Using ms As New MemoryStream
            ms.Write(Message, 28, 8)
            Dim actEvenChecksum As UInt32 = BitConverter.ToUInt32(clsCRC32.CRC32OfByte(ms.ToArray), 0)
            If Not actEvenChecksum = lastEvenChecksum Then
                WriteCWtoFile(1, ms.ToArray)
                lastEvenChecksum = actEvenChecksum
            End If
        End Using

    End Sub

    Private Sub WriteHeaderToFile()
        Dim out As String = String.Empty
        '# CWlog V1.0 - logging of ORF1 started at: 01/06/09 21:32:55
        out &= "# CWlog V1.0 - logging of "
        out &= Services.GetServiceInfo(iCAID.ToString("X4") & ":" & iSRVID.ToString("X4")).Name
        out &= " started at: " & Date.Now.ToString & " logged by speedCS"
        Using fw As New StreamWriter(actualFileName, True)
            fw.WriteLine(out)
        End Using
        '# CAID 0x0D05, PID 0x00C9, PROVIDER 0x000004
        out = "# CAID 0x" & iCAID.ToString("X4") & ", "
        out &= " PID 0x" & iSRVID.ToString("X4") & ", "
        out &= " PROVIDER 0x" & iPROVID.ToString("X8")
        Using fw As New StreamWriter(actualFileName, True)
            fw.WriteLine(out)
        End Using
    End Sub

    Private Sub WriteCWtoFile(ByVal parity As Byte, ByVal Message() As Byte)
        Dim out As String = String.Empty
        For i As Integer = 0 To Message.Length - 1
            out &= Message(i).ToString("X2") & " "
        Next
        Using fw As New StreamWriter(actualFileName, True)
            fw.WriteLine(parity & " " & out & "# " & Date.Now.ToLongTimeString)
        End Using
    End Sub
End Class
