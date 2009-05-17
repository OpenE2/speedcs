Imports System.IO
Imports System.Text.RegularExpressions

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

            Dim iSRVID As UInt16 = GetLittleEndian(BitConverter.ToUInt16(Message, 8))
            Dim iCAID As UInt16 = GetLittleEndian(BitConverter.ToUInt16(Message, 10))
            Dim iPROVID As UInt32

            Using ms As New MemoryStream
                ms.Write(Message, 12, 4)
                Dim byteTmp() As Byte = ms.ToArray
                Array.Reverse(byteTmp)
                iPROVID = BitConverter.ToUInt32(byteTmp, 0)
            End Using

            logChannel = New clsLogChannel(iSRVID, iCAID, iPROVID) 'Moved variables to Constructor
            cwLogChannels.Add(key, logChannel)

        End If

        logChannel.ResolveCMD1(Message)

    End Sub
End Class

Public Class clsLogChannel
    Private filepath As String
    Private lastOddChecksum As UInt32
    Private lastOddTimestamp As Date
    Private isFirstOdd As Boolean = True
    Private lastEvenChecksum As UInt32
    Private lastEvenTimestamp As Date
    Private isFirstEven As Boolean = True

    Private actualFilePath As String
    Private actualFileName As String

    Private _iSRVID As UInt16
    Private _iCAID As UInt16
    Private _iPROVID As UInt32

    Public Sub New(ByVal iSRVID As UInt16, ByVal iCAID As UInt16, ByVal iPROVID As UInt32)

        filepath = Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), _
                                Application.ProductName)

        _iSRVID = iSRVID
        _iCAID = iCAID
        _iPROVID = iPROVID
        SetEnv()
    End Sub

    Private Sub SetEnv()
        Dim d As Date = Date.Now

        actualFilePath = Path.Combine(filepath, "CWL-" & d.Year & d.Month & d.Day)

        If Not Directory.Exists(actualFilePath) Then
            Directory.CreateDirectory(actualFilePath)

            Dim serviceName As String = Services.GetServiceInfo(_iCAID.ToString("X4") & ":" & _iSRVID.ToString("X4")).Name
            serviceName = Regex.Replace(serviceName.Trim, "[^A-Za-z0-9]", "_")

            actualFileName = d.Year.ToString.Substring(2, 2) & d.Month.ToString.PadLeft(2, CChar("0")) & d.Day.ToString.PadLeft(2, CChar("0")) _
                                & "-C" & _iCAID.ToString("X4") _
                                & "-I" & _iSRVID.ToString("X4") _
                                & "-P" & _iPROVID.ToString("X6") _
                                & "-" & serviceName _
                                & ".cwl"

            actualFileName = Path.Combine(actualFilePath, actualFileName)
            WriteHeaderToFile()
        End If

        If Not File.Exists(actualFileName) Then
            Dim serviceName As String = Services.GetServiceInfo(_iCAID.ToString("X4") & ":" & _iSRVID.ToString("X4")).Name
            serviceName = Regex.Replace(serviceName.Trim, "[^A-Za-z0-9]", "_")

            actualFileName = d.Year.ToString.Substring(2, 2) & d.Month.ToString.PadLeft(2, CChar("0")) & d.Day.ToString.PadLeft(2, CChar("0")) _
                         & "-C" & _iCAID.ToString("X4") _
                         & "-I" & _iSRVID.ToString("X4") _
                         & "-P" & _iPROVID.ToString("X6") _
                         & "-" & serviceName _
                         & ".cwl"

            actualFileName = Path.Combine(actualFilePath, actualFileName)
            WriteHeaderToFile()
        End If
    End Sub

    Public Sub ResolveCMD1(ByVal Message() As Byte)

        Using ms As New MemoryStream
            ms.Write(Message, 20, 8)
            Dim actOddChecksum As UInt32 = BitConverter.ToUInt32(clsCRC32.CRC32OfByte(ms.ToArray), 0)
            If Not actOddChecksum = lastOddChecksum Then
                If Not isFirstOdd Then WriteCWtoFile(0, ms.ToArray)
                lastOddChecksum = actOddChecksum
                isFirstOdd = False
            End If
        End Using

        Using ms As New MemoryStream
            ms.Write(Message, 28, 8)
            Dim actEvenChecksum As UInt32 = BitConverter.ToUInt32(clsCRC32.CRC32OfByte(ms.ToArray), 0)
            If Not actEvenChecksum = lastEvenChecksum Then
                If Not isFirstEven Then WriteCWtoFile(1, ms.ToArray)
                lastEvenChecksum = actEvenChecksum
                isFirstEven = False
            End If
        End Using

    End Sub

    Private Sub WriteHeaderToFile()
        Dim strOut As String = String.Empty
        '# CWlog V1.0 - logging of ORF1 started at: 01/06/09 21:32:55
        strOut &= "# CWlog V1.0 - logging of "
        strOut &= Services.GetServiceInfo(_iCAID.ToString("X4") & ":" & _iSRVID.ToString("X4")).Name
        strOut &= " started at: " & Date.Now.ToString & " logged by speedCS"

        Using fw As New StreamWriter(actualFileName, True)
            fw.WriteLine(strOut)
        End Using

        '# CAID 0x0D05, PID 0x00C9, PROVIDER 0x000004
        strOut = "# CAID 0x" & _iCAID.ToString("X4") & ","
        strOut &= " PID 0x" & _iSRVID.ToString("X4") & ","
        strOut &= " PROVIDER 0x" & _iPROVID.ToString("X6")

        Using fw As New StreamWriter(actualFileName, True)
            fw.WriteLine(strOut)
        End Using

    End Sub

    Private Sub WriteCWtoFile(ByVal parity As Byte, ByVal Message() As Byte)
        Dim strOut As String = String.Empty

        For i As Integer = 0 To Message.Length - 1
            strOut &= Message(i).ToString("X2") & " "
        Next

        If Not File.Exists(actualFileName) Then SetEnv()

        Using fw As New StreamWriter(actualFileName, True)
            fw.WriteLine(parity & " " & strOut & "# " & Date.Now.ToLongTimeString)
        End Using

    End Sub
End Class
