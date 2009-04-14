Imports System.IO

Public Class clsCMDManager

    Public Sub New()
        AddHandler CMD1Answers.GotCommand, AddressOf IncommingCMD1
        AddHandler CMD0Requests.GotCommand, AddressOf IncommingCMD0
    End Sub

    Public BroadcastQueue As New Queue(Of Byte())

#Region "CMD 1 Answers"

    Public Class clsCMD1Answer
        Private _ECM_CRC() As Byte
        Private _CreateDate As Date

        Public CMD As types.CMDType
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
                If DateDiff(DateInterval.Second, _CreateDate, Date.Now) > 5 Then
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
            CMD = CType(PlainCMD1Message(0), types.CMDType)
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
                'ms.Write(PlainCMD1Message, 16, _Length)
                ms.Write(PlainCMD1Message, 20, 16)
                CW = ms.ToArray
            End Using
        End Sub

        Public Function TransformCMD0toCMD66Message() As Byte()
            Using ms As New MemoryStream
                ms.WriteByte(&H66)
                ms.WriteByte(_Length)
                ms.WriteByte(&H0)
                ms.WriteByte(&H0)
                ms.Write(_ECM_CRC, 0, 4)
                ms.Write(SRVID, 0, 2)
                ms.Write(CAID, 0, 2)
                ms.Write(PROVID, 0, 4)
                ms.WriteByte(&H0)
                ms.WriteByte(&H0)
                ms.WriteByte(&H0)
                ms.WriteByte(&H0)
                ms.Write(CW, 0, _Length)
                While Not ms.Length = 48
                    ms.WriteByte(&HFF)
                End While
                TransformCMD0toCMD66Message = ms.ToArray
            End Using
        End Function

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
                    ms.WriteByte(CMD0Message(2))
                    ms.WriteByte(CMD0Message(3))
                    ms.Write(_ECM_CRC, 0, 4)
                    ms.Write(SRVID, 0, 2)
                    ms.Write(CAID, 0, 2)
                    ms.Write(PROVID, 0, 4)
                    ms.Write(CMD0Message, 16, 4) 'CHID
                    ms.Write(CW, 0, _Length)
                    While Not ms.Length = 48
                        ms.WriteByte(&HFF)
                    End While
                    'DebugOutputBytes(CMD0Message, "original ")
                    'DebugOutputBytes(ms.ToArray, "transform ")
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
        Public Event GotCommand(ByVal sender As Object, ByVal command As types.CMDType)

        Public Overloads Sub Add(ByVal PlainCMD1Message() As Byte)
            'Me.Clean()
            'SyncLock Me
            Dim a As New clsCMD1Answer(PlainCMD1Message)
            If Me.ContainsKey(a.Key) Then
                If Me(a.Key).Dead Then
                    Me(a.Key).ReNew(PlainCMD1Message)
                    Threading.Thread.Sleep(50)
                    RaiseEvent GotCommand(Me(a.Key), Me(a.Key).CMD)
                    Debug.WriteLine("Renew CMD1")
                End If
            Else
                Me.Add(a.Key, a)
                Debug.WriteLine("Add CMD1")
                Threading.Thread.Sleep(50)
                RaiseEvent GotCommand(a, a.CMD)
            End If
            'End SyncLock

        End Sub

        Public Sub Clean()
            SyncLock Me
                For idx As Integer = Me.Count - 1 To 0 Step -1
                    If Me.Item(Keys(idx)).Dead Then
                        Me.Remove(Keys(idx))
                    End If
                Next
            End SyncLock
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

#End Region

#Region "CMD 0 Requests"

    Public Class clsCMD0Request
        Private _ECM_CRC() As Byte
        Private _CreateDate As Date

        Public UCRC As New SortedList(Of UInt32, Byte())
        Public CMD As types.CMDType
        Public CAID() As Byte
        Public iCAID As UInt16
        Public SRVID() As Byte
        Public iSRVID As UInt16
        Public PROVID() As Byte
        Public iPROVID As UInt32
        Public CW() As Byte
        Public PlainMessage() As Byte
        Public Key As UInt32
        Public IncomingTimeStamp As Long = Environment.TickCount

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
                If DateDiff(DateInterval.Second, _CreateDate, Date.Now) > 4 Then
                    Return True
                Else
                    Return False
                End If
            End Get
        End Property

        Public Sub New(ByVal PlainCMD0Message() As Byte, ByVal sUCRC As UInt32)
            GetFromCMD0Message(PlainCMD0Message, sUCRC)
            'Me.UCRC.Add(sUCRC, Nothing)
        End Sub

        Public Sub GetFromCMD0Message(ByVal PlainCMD0Message() As Byte, ByVal sUCRC As UInt32)
            CMD = CType(PlainCMD0Message(0), types.CMDType)
            PlainMessage = PlainCMD0Message
            _Length = PlainCMD0Message(1)
            _CreateDate = Date.Now
            UCRC.Add(sUCRC, PlainCMD0Message)
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
            'Using ms As New MemoryStream
            '    ms.Write(PlainCMD0Message, 16, _Length)
            '    CW = ms.ToArray
            'End Using
        End Sub
    End Class

    Public Class clsCMD0Requests
        Inherits SortedList(Of UInt32, clsCMD0Request)
        Public Event GotCommand(ByVal sender As Object, ByVal command As types.CMDType)

        Public Overloads Sub Add(ByVal PlainCMD0Message() As Byte, ByVal sUCRC As UInt32)
            Me.Clean()
            Dim a As New clsCMD0Request(PlainCMD0Message, sUCRC)
            SyncLock Me
                If Not Me.ContainsKey(a.Key) Then
                    Me.Add(a.Key, a)
                Else
                    Me(a.Key).UCRC.Add(sUCRC, PlainCMD0Message)
                End If
            End SyncLock
        End Sub

        Public Sub Clean()
            SyncLock Me
                For idx As Integer = Me.Count - 1 To 0 Step -1
                    If Me.Item(Keys(idx)).Dead Then
                        Me.Remove(Keys(idx))
                    End If
                Next
            End SyncLock
        End Sub

        Public Function GetCMD0ByKey(ByVal Key As UInt32) As clsCMD0Request
            If Me.ContainsKey(Key) Then
                Return Me(Key)
            Else
                Return Nothing
            End If
        End Function
    End Class

    Private _CMD0Requests As New clsCMD0Requests
    Public Property CMD0Requests() As clsCMD0Requests
        Get
            Return _CMD0Requests
        End Get
        Set(ByVal value As clsCMD0Requests)
            _CMD0Requests = value
        End Set
    End Property
#End Region


    'Hack: aktueller Clientsender - funzt nicht (client sagt "wrong Password")
    Private Sub IncommingCMD1(ByVal sender As Object, ByVal type As types.CMDType)
        Dim answer As clsCMD1Answer = TryCast(sender, clsCMD1Answer)
        Dim request As clsCMD0Request = TryCast(CMD0Requests(answer.Key), clsCMD0Request)
        'If one of the request matches
        Debug.WriteLine("Answer Action")
        If Not request Is Nothing Then
            Debug.WriteLine("Request found")
            'for each requesting client in this request
            For Each ucrc As UInt32 In request.UCRC.Keys
                'transform cmd0 to cmd1 with client's cmd 0
                Dim preSend() As Byte = answer.TransformCMD0toCMD1Message(request.UCRC(ucrc))
                'DebugOutputBytes(preSend, "Pre Send: ")
                'find clientdata by ucrc
                Dim c As clsSettingsClients.clsClient = CfgClients.Clients.FindByUCRC(ucrc)
                Using ms As New MemoryStream
                    'built ucrc header
                    Dim ucrcbytes() As Byte = BitConverter.GetBytes(ucrc)
                    Array.Reverse(ucrcbytes)
                    ms.Write(ucrcbytes, 0, 4)
                    'put encrypted behind
                    Dim encrypted() As Byte = AESCrypt.Encrypt(preSend, c.MD5_Password)
                    ms.Write(encrypted, 0, encrypted.Length)
                    'send to client
                    UdpClientManager.SendUDPMessage(ms.ToArray, Net.IPAddress.Parse(CStr(c.SourceIp)), c.SourcePort)

                    Dim adressData As String = c.SourceIp & ":" & c.SourcePort
                    adressData = adressData.PadRight(22)

                    Output("C " _
                           & adressData _
                           & c.Username _
                           & " [" _
                           & Hex(request.iCAID).PadLeft(4, CChar("0")) _
                           & ":" _
                           & Hex(request.iSRVID).PadLeft(4, CChar("0")) _
                           & "] found in " _
                           & Environment.TickCount - request.IncomingTimeStamp _
                           & "ms", LogDestination.none, LogSeverity.info, ConsoleColor.Green)

                    'DebugOutputBytes(c.MD5_Password, c.Username & " -n: ")
                    'DebugOutputBytes(ms.ToArray, "New Send: ")
                End Using
                'Debug.WriteLine("sent to: " & c.Username)
            Next
            'clear client's requests
            'request.UCRC.Clear()
            'Delete this request
            CMD0Requests.Remove(request.Key)
        Else
            Debug.WriteLine("No Request found")
        End If

        If Not type = types.CMDType.BroadCastResponse Then
            SyncLock BroadcastQueue
                BroadcastQueue.Enqueue(answer.TransformCMD0toCMD66Message)
            End SyncLock
            Dim t As New Threading.Thread(AddressOf SendBroadcast)
            t.Priority = Threading.ThreadPriority.BelowNormal
            t.Start()
        End If

    End Sub

    Private Sub IncommingCMD0(ByVal sender As Object, ByVal type As types.CMDType)
        Dim request As clsCMD0Request = TryCast(sender, clsCMD0Request)
        If Me.CMD1Answers.ContainsKey(request.Key) Then
            IncommingCMD1(Me.CMD1Answers(request.Key), types.CMDType.EMMResponse)
        End If
    End Sub

    Private Sub SendBroadcast()
        Dim Broadcast2send() As Byte
        SyncLock BroadcastQueue
            Broadcast2send = BroadcastQueue.Dequeue
        End SyncLock
        For Each udpserv As clsUDPIO In udpServers
            With udpserv.serverobject
                If .SendBroadcasts And .Active Then
                    Using ms As New MemoryStream
                        Dim ucrcbytes() As Byte = BitConverter.GetBytes(.UCRC)
                        ms.Write(ucrcbytes, 0, 4)
                        Dim encrypted() As Byte = AESCrypt.Encrypt(Broadcast2send, .MD5_Password)
                        ms.Write(encrypted, 0, encrypted.Length)
                        udpserv.SendUDPMessage(ms.ToArray, Net.IPAddress.Parse(.IP), .Port)
                    End Using
                End If
                Debug.WriteLine("Broadcast to " & .Hostname & ":" & .Port)
            End With 'udpserv.serverobject
        Next
    End Sub
End Class
