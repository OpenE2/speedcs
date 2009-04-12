Imports System.IO

Public Class clsCMDManager

    Public Class clsCMD1Answer
        Private _ECM_CRC() As Byte
        Private _CreateDate As Date

        Public CMD As Byte
        Public CAID() As Byte
        Public iCAID As UInt16
        Public SRVID() As Byte
        Public iSRVID As UInt16
        Public PROVID() As Byte
        Public iPROVID As UInt32
        Public CW() As Byte
        Public Key As UInt32

        Private _Length As Byte
        Public ReadOnly Property Length() As Byte
            Get
                Return _Length
            End Get
        End Property

        Public ReadOnly Property Age() As Long
            Get
                Return DateDiff(DateInterval.Second, _CreateDate, Date.Now)
            End Get
        End Property

        Public ReadOnly Property Dead() As Boolean
            Get
                If DateDiff(DateInterval.Second, _CreateDate, Date.Now) > 6 Then
                    Return True
                Else
                    Return False
                End If
            End Get
        End Property

        Public Sub New(ByVal PlainCMD1Message() As Byte)
            GetFromCMD1Message(PlainCMD1Message)
        End Sub

        Public Sub ReNew(ByVal PlainCMD1Message() As Byte)
            GetFromCMD1Message(PlainCMD1Message)
        End Sub

        Public Sub GetFromCMD1Message(ByVal PlainCMD1Message() As Byte)
            CMD = PlainCMD1Message(0)
            _Length = PlainCMD1Message(1)
            _CreateDate = Date.Now
            Using ms As New MemoryStream
                ms.Write(PlainCMD1Message, 8, 8)
                Key = BitConverter.ToUInt32(clsCRC32.CRC32OfByte(ms.ToArray), 0)
            End Using
            Using ms As New MemoryStream
                ms.Write(PlainCMD1Message, 4, 4)
                _ECM_CRC = ms.ToArray
            End Using
            Using ms As New MemoryStream
                ms.Write(PlainCMD1Message, 8, 2)
                SRVID = ms.ToArray
                iSRVID = BitConverter.ToUInt16(SRVID, 0)
                iSRVID = CUShort(Math.Floor(iSRVID / 256) + 256 * (iSRVID And 255)) 'Convert to Little Endian
            End Using
            Using ms As New MemoryStream
                ms.Write(PlainCMD1Message, 10, 2)
                CAID = ms.ToArray
                iCAID = BitConverter.ToUInt16(CAID, 0)
                iCAID = CUShort(Math.Floor(iCAID / 256) + 256 * (iCAID And 255)) 'Convert to Little Endian
            End Using
            Using ms As New MemoryStream
                ms.Write(PlainCMD1Message, 12, 4)
                PROVID = ms.ToArray
                iPROVID = BitConverter.ToUInt32(PROVID, 0)
                iPROVID = CUInt(Math.Floor(iPROVID / 65536) + 65536 * (iPROVID And 65535)) 'Convert to Little Endian
            End Using
            Using ms As New MemoryStream
                ms.Write(PlainCMD1Message, 16, _Length)
                CW = ms.ToArray
            End Using
        End Sub

        Private Function GetECMFromStructure() As Byte()
            Using ms As New MemoryStream
                ms.Write(SRVID, 0, 2)
                ms.Write(CAID, 0, 2)
                ms.Write(PROVID, 0, 4)
                ms.Write(CW, 0, CW.Length)
                GetECMFromStructure = ms.ToArray
            End Using
        End Function

        Public Function TransformCMD0toCMD1Message(ByVal CMD0Message() As Byte) As Byte()
            If CMD0Message(0) = types.CMDType.ECMRequest Then
                Using ms As New MemoryStream
                    ms.WriteByte(&H1)
                    ms.WriteByte(_Length)
                    ms.WriteByte(CMD0Message(3))
                    ms.WriteByte(CMD0Message(2))
                    ms.Write(_ECM_CRC, 0, 4)
                    ms.Write(SRVID, 0, 2)
                    ms.Write(CAID, 0, 2)
                    ms.Write(PROVID, 0, 4)
                    ms.Write(CW, 0, _Length)
                    While Not ms.Length = 48
                        ms.WriteByte(&HFF)
                    End While
                    TransformCMD0toCMD1Message = ms.ToArray
                End Using
            Else
                Using ms As New MemoryStream
                    While Not ms.Length = 48
                        ms.WriteByte(&H99)
                    End While
                    TransformCMD0toCMD1Message = ms.ToArray
                End Using
                Debug.WriteLine("No CMD0 given")
            End If
        End Function
    End Class

    Public Class clsCMD1Answers
        Inherits SortedList(Of UInt32, clsCMD1Answer)
        Public Event GotCommand(ByVal sender As Object, ByVal command As Byte)

        Public Overloads Sub Add(ByVal PlainCMD1Message() As Byte)
            Dim a As New clsCMD1Answer(PlainCMD1Message)
            If Me.ContainsKey(a.Key) Then
                If Me(a.Key).Dead Then
                    Me(a.Key).ReNew(PlainCMD1Message)
                    RaiseEvent GotCommand(Me, Me(a.Key).CMD)
                    Debug.WriteLine("Renew CMD1")
                End If
            Else
                Me.Add(a.Key, a)
                RaiseEvent GotCommand(Me, a.CMD)
                Debug.WriteLine("Add CMD1")
            End If
        End Sub

        Public Sub Clean()
            For Each a As clsCMD1Answer In Me.Values
                If a.Dead Then
                    Me.Remove(a.Key)
                End If
            Next
        End Sub

        Public Function GetCMD1ByKey(ByVal Key As UInt32) As clsCMD1Answer
            If Me.ContainsKey(Key) Then
                Return Me(Key)
            Else
                Return Nothing
            End If
        End Function

    End Class

    Private _CMD1Answers As New clsCMD1Answers
    Public Property CMD1Answers() As clsCMD1Answers
        Get
            Return _CMD1Answers
        End Get
        Set(ByVal value As clsCMD1Answers)
            _CMD1Answers = value
        End Set
    End Property




    Public Class clsCMD0Request
        Private _ECM_CRC() As Byte
        Private _CreateDate As Date

        Public UCRC As UInt32
        Public CMD As Byte
        Public CAID() As Byte
        Public iCAID As UInt16
        Public SRVID() As Byte
        Public iSRVID As UInt16
        Public PROVID() As Byte
        Public iPROVID As UInt32
        Public CW() As Byte
        Public PlainMessage() As Byte
        Public Key As UInt32

        Private _Length As Byte
        Public ReadOnly Property Length() As Byte
            Get
                Return _Length
            End Get
        End Property

        Public ReadOnly Property Age() As Long
            Get
                Return DateDiff(DateInterval.Second, _CreateDate, Date.Now)
            End Get
        End Property

        Public ReadOnly Property Dead() As Boolean
            Get
                If DateDiff(DateInterval.Second, _CreateDate, Date.Now) > 6 Then
                    Return True
                Else
                    Return False
                End If
            End Get
        End Property

        Public Sub New(ByVal PlainCMD0Message() As Byte, ByVal sUCRC As UInt32)
            GetFromCMD0Message(PlainCMD0Message, sUCRC)
        End Sub

        Public Sub GetFromCMD0Message(ByVal PlainCMD0Message() As Byte, ByVal rUCRC As UInt32)
            CMD = PlainCMD0Message(0)
            UCRC = rUCRC
            PlainMessage = PlainCMD0Message
            _Length = PlainCMD0Message(1)
            _CreateDate = Date.Now
            Using ms As New MemoryStream
                ms.Write(PlainCMD0Message, 8, 8)
                Key = BitConverter.ToUInt32(clsCRC32.CRC32OfByte(ms.ToArray), 0)
            End Using
            Using ms As New MemoryStream
                ms.Write(PlainCMD0Message, 4, 4)
                _ECM_CRC = ms.ToArray
            End Using
            Using ms As New MemoryStream
                ms.Write(PlainCMD0Message, 8, 2)
                SRVID = ms.ToArray
                iSRVID = BitConverter.ToUInt16(SRVID, 0)
                iSRVID = CUShort(Math.Floor(iSRVID / 256) + 256 * (iSRVID And 255)) 'Convert to Little Endian
            End Using
            Using ms As New MemoryStream
                ms.Write(PlainCMD0Message, 10, 2)
                CAID = ms.ToArray
                iCAID = BitConverter.ToUInt16(CAID, 0)
                iCAID = CUShort(Math.Floor(iCAID / 256) + 256 * (iCAID And 255)) 'Convert to Little Endian
            End Using
            Using ms As New MemoryStream
                ms.Write(PlainCMD0Message, 12, 4)
                PROVID = ms.ToArray
                iPROVID = BitConverter.ToUInt32(PROVID, 0)
                iPROVID = CUInt(Math.Floor(iPROVID / 65536) + 65536 * (iPROVID And 65535)) 'Convert to Little Endian
            End Using
            Using ms As New MemoryStream
                ms.Write(PlainCMD0Message, 16, _Length)
                CW = ms.ToArray
            End Using
        End Sub
    End Class

    Public Class clsCMD0Requests
        Inherits SortedList(Of UInt32, clsCMD0Request)
        Public Event GotCommand(ByVal sender As Object, ByVal command As Byte)

        Public Overloads Sub Add(ByVal PlainCMD0Message() As Byte, ByVal sUCRC As UInt32)
            Dim a As New clsCMD0Request(PlainCMD0Message, sUCRC)

        End Sub

        Public Sub Clean()
            For Each a As clsCMD0Request In Me.Values
                If a.Dead Then
                    Me.Remove(a.Key)
                End If
            Next
        End Sub

        Public Function GetCMD1ByKey(ByVal Key As UInt32) As clsCMD0Request
            If Me.ContainsKey(Key) Then
                Return Me(Key)
            Else
                Return Nothing
            End If
        End Function
    End Class
End Class
