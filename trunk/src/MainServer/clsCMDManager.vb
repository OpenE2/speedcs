Imports System.IO
Imports System.Text
Imports SpeedCS.types

Public Class clsCMDManager

    Public Sub New()
        AddHandler CMD1Answers.GotCommand, AddressOf IncommingCMD1
        AddHandler CMD0Requests.GotCommand, AddressOf IncommingCMD0
    End Sub

    Public BroadcastQueue As New Queue(Of clsCMD1Answer)
    Public Request1stLevelQueue As New Queue(Of clsCMD0Request)
    Public Request2ndLevelQueue As New Queue(Of clsCMD0Request)
    Public BroadcastCandidates As New SortedList(Of UInt32, Date)


#Region "CMD 1 Answers"

    Public Class clsCMD1Answer
        Private _ECM_CRC() As Byte
        Private _CreateDate As Date

        Public CMD As CMDType
        Public CAID() As Byte
        Public iCAID As UInt16
        Public SRVID() As Byte
        Public iSRVID As UInt16
        Public PROVID() As Byte
        Public iPROVID As UInt32
        Public BTE18_19() As Byte
        Public CW() As Byte
        Public Key As UInt32

        Public SenderIP As String
        Public senderPort As Integer

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
                If DateDiff(DateInterval.Second, _CreateDate, Date.Now) >= 6 Then
                    Return True
                Else
                    Return False
                End If
            End Get
        End Property

        Public Sub New(ByVal PlainCMD1Message() As Byte, ByVal IP As String, ByVal port As Integer)
            GetFromCMD1Message(PlainCMD1Message)
            Me.SenderIP = IP
            Me.senderPort = port
        End Sub

        Public Sub ReNew(ByVal PlainCMD1Message() As Byte)
            GetFromCMD1Message(PlainCMD1Message)
        End Sub

        Public Sub GetFromCMD1Message(ByVal PlainCMD1Message() As Byte)
            CMD = CType(PlainCMD1Message(0), CMDType)
            _Length = PlainCMD1Message(1)
            _CreateDate = Date.Now
            Using ms As New MemoryStream
                ms.Write(PlainCMD1Message, 8, 4)
                ms.Write(PlainCMD1Message, 18, 2) 'Miracle Byte 18+19
                Key = BitConverter.ToUInt32(clsCRC32.CRC32OfByte(ms.ToArray), 0)
            End Using
            Using ms As New MemoryStream
                ms.Write(PlainCMD1Message, 18, 2) 'Miracle Byte 18+19
                BTE18_19 = ms.ToArray
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
                ms.WriteByte(BTE18_19(0))
                ms.WriteByte(BTE18_19(1))
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
            If CMD0Message(0) = CMDType.ECMRequest Or CMD0Message(0) = CMDType.sCSRequest Then
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
        Public Event GotCommand(ByVal sender As Object, ByVal command As CMDType)

        Public Overloads Sub Add(ByVal PlainCMD1Message() As Byte, ByVal IP As String, ByVal port As Integer)
            Dim cmd As CMDType = CType(PlainCMD1Message(0), CMDType)

            Me.Clean()
            'SyncLock Me
            If cmd = CMDType.ECMResponse Then
                Dim a As New clsCMD1Answer(PlainCMD1Message, IP, port)

                If Me.ContainsKey(a.Key) Then
                    If Me(a.Key).Dead Then
                        Me(a.Key).ReNew(PlainCMD1Message)
                        'Threading.Thread.Sleep(50)
                        RaiseEvent GotCommand(Me(a.Key), Me(a.Key).CMD)
                        Debug.WriteLine("Renew CMD1" & a.CMD)
                    End If
                Else
                    'a.GetFromCMD1Message(PlainCMD1Message)
                    Me.Add(a.Key, a)
                    Debug.WriteLine("Add CMD1" & a.CMD)
                    'Threading.Thread.Sleep(50)
                    RaiseEvent GotCommand(a, a.CMD)
                End If
                'End SyncLock
            ElseIf cmd = CMDType.BroadCastResponse Then
                Dim a As New clsCMD1Answer(PlainCMD1Message, IP, port)
                If Not Me.ContainsKey(a.Key) Then
                    Me.Add(a.Key, a)
                    Dim IDs As String = Hex(a.iCAID).PadLeft(4, CChar("0")) & ":" & Hex(a.iSRVID).PadLeft(4, CChar("0")) & ":" & Hex(a.iPROVID).PadLeft(6, CChar("0"))
                    Output("Add CMD1 from Broadcast [" & IDs & "] " & Services.GetServiceInfo(IDs).Name, LogDestination.none, LogSeverity.info, ConsoleColor.DarkYellow)
                    'Threading.Thread.Sleep(50)
                    RaiseEvent GotCommand(a, a.CMD)
                End If
            End If

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
        Public CMD As CMDType
        Public CAID() As Byte
        Public iCAID As UInt16
        Public SRVID() As Byte
        Public iSRVID As UInt16
        Public srvidcaid As UInt32
        Public PROVID() As Byte
        Public iPROVID As UInt32
        Public CW() As Byte
        Public PlainMessage() As Byte
        Public Key As UInt32
        Public IncomingTimeStamp As Long = Environment.TickCount

        Public SenderIP As String
        Public senderPort As Integer

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
                If DateDiff(DateInterval.Second, _CreateDate, Date.Now) >= 6 Then
                    Return True
                Else
                    Return False
                End If
            End Get
        End Property

        Public Sub New(ByVal PlainCMD0Message() As Byte, ByVal sUCRC As UInt32, ByVal IP As String, ByVal port As Integer)
            GetFromCMD0Message(PlainCMD0Message, sUCRC)
            'Me.UCRC.Add(sUCRC, Nothing)
        End Sub

        Public Sub GetFromCMD0Message(ByVal PlainCMD0Message() As Byte, ByVal sUCRC As UInt32)
            CMD = CType(PlainCMD0Message(0), CMDType)
            PlainMessage = PlainCMD0Message
            _Length = PlainCMD0Message(1)
            _CreateDate = Date.Now
            UCRC.Add(sUCRC, PlainCMD0Message)
            Using ms As New MemoryStream
                ms.Write(PlainCMD0Message, 8, 4)
                ms.Write(PlainCMD0Message, 18, 2) 'Miracle Byte 18+19
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

            'Set last requested Service to client
            CfgClients.Clients.FindByUCRC(sUCRC).lastRequestedService = _
            Services.GetServiceInfo(Hex(iCAID).PadLeft(4, CChar("0")) & ":" & Hex(iSRVID).PadLeft(4, CChar("0")))

            'Set last requested CAID:SRVID to cliet
            CfgClients.Clients.FindByUCRC(sUCRC).lastRequestedCAIDSRVID = _
            Hex(iCAID).PadLeft(4, CChar("0")) & ":" & Hex(iSRVID).PadLeft(4, CChar("0"))

            Using ms As New MemoryStream
                ms.Write(PlainCMD0Message, 8, 4)
                srvidcaid = BitConverter.ToUInt32(ms.ToArray, 0)
                srvidcaid = CUInt(Math.Floor(srvidcaid / 65536) + 65536 * (srvidcaid And 65535))
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
        Public Event GotCommand(ByVal sender As Object, ByVal command As CMDType)

        Public Overloads Sub Add(ByVal PlainCMD0Message() As Byte, ByVal sUCRC As UInt32, ByVal IP As String, ByVal port As Integer)
            Me.Clean()
            Dim a As New clsCMD0Request(PlainCMD0Message, sUCRC, IP, port)
            SyncLock Me
                If Not Me.ContainsKey(a.Key) Then
                    Me.Add(a.Key, a)
                    RaiseEvent GotCommand(a, CMDType.ECMRequest)
                Else
                    If Not Me(a.Key).UCRC.ContainsKey(sUCRC) Then
                        Me(a.Key).UCRC.Add(sUCRC, PlainCMD0Message)
                    Else
                        Me(a.Key).UCRC(sUCRC) = PlainCMD0Message
                    End If
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


    Private Sub IncommingCMD1(ByVal sender As Object, ByVal type As CMDType)
        Dim answer As clsCMD1Answer = TryCast(sender, clsCMD1Answer)
        Debug.WriteLine("CMD from " & answer.SenderIP)

        Dim request As clsCMD0Request = Nothing
        If CMD0Requests.ContainsKey(answer.Key) Then
            request = TryCast(CMD0Requests(answer.Key), clsCMD0Request)
        End If

        If Not request Is Nothing Then
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
                               & ":" _
                               & Hex(request.iPROVID).PadLeft(6, CChar("0")) _
                               & "] found in " _
                               & Environment.TickCount - request.IncomingTimeStamp _
                               & "ms", LogDestination.none, LogSeverity.info, ConsoleColor.Green)

                        'DebugOutputBytes(c.MD5_Password, c.Username & " -n: ")
                        'DebugOutputBytes(ms.ToArray, "New Send: ")
                    End Using
                    'Debug.WriteLine("sent to: " & c.Username)
                Next
                'clear client's requests
                request.UCRC.Clear()
                'Delete this request
                CMD0Requests.Remove(request.Key)
            Else
                Debug.WriteLine("No Request found")
            End If
        End If

        If Not type = CMDType.BroadCastResponse Then
            SyncLock BroadcastQueue
                BroadcastQueue.Enqueue(answer)
            End SyncLock
            Dim t As New Threading.Thread(AddressOf SendBroadcast)
            t.Priority = Threading.ThreadPriority.BelowNormal
            t.Start()
        ElseIf type = CMDType.BroadCastResponse Then
            If BroadcastCandidates.ContainsKey(answer.Key) Then

            Else
                BroadcastCandidates.Add(answer.Key, Date.Now)
            End If

        End If

    End Sub

    Private Sub IncommingCMD0(ByVal sender As Object, ByVal type As CMDType)
        Dim request As clsCMD0Request = TryCast(sender, clsCMD0Request)
        If Me.CMD1Answers.ContainsKey(request.Key) Then
            If Not Me.CMD1Answers(request.Key).Dead Then
                IncommingCMD1(Me.CMD1Answers(request.Key), CMDType.EMMResponse)
                Debug.WriteLine("Answer age: " & Me.CMD1Answers(request.Key).Age)
                Exit Sub
            Else
                Debug.WriteLine("Answer is Dead")
            End If
        End If

        If BroadcastCandidates.ContainsKey(request.Key) Then
            If DateDiff(DateInterval.Second, Date.Now, BroadcastCandidates(request.Key)) < 7 Then
                'we should wait here n * 100 mS for an broadcast
                SyncLock Request2ndLevelQueue
                    Request2ndLevelQueue.Enqueue(request)
                    Debug.WriteLine("Enqueue to L2")
                End SyncLock
            Else
                SyncLock Request1stLevelQueue
                    Request1stLevelQueue.Enqueue(request)
                    Debug.WriteLine("Enqueue to L1a")
                End SyncLock
            End If
        Else
            SyncLock Request1stLevelQueue
                Request1stLevelQueue.Enqueue(request)
                Debug.WriteLine("Enqueue to L1b")
            End SyncLock
        End If


        Dim t As New Threading.Thread(AddressOf Order2Servers)
        t.Priority = Threading.ThreadPriority.BelowNormal
        t.Start()
    End Sub

    Private Sub Order2Servers()
        SyncLock Request1stLevelQueue
            While Request1stLevelQueue.Count > 0
                Send2Servers(Request1stLevelQueue.Dequeue)
            End While
        End SyncLock
        Threading.Thread.Sleep(200) 'L2 have to wait 4 Broadcast
        SyncLock Request2ndLevelQueue
            While Request2ndLevelQueue.Count > 0
                Send2Servers(Request2ndLevelQueue.Dequeue)
            End While
        End SyncLock
    End Sub

    Private Sub Send2Servers(ByVal request As clsCMD0Request)
        Dim canceled As Boolean = False

        Dim consoleOut As String = "S [" _
                                   & Hex(request.iCAID).PadLeft(4, CChar("0")) _
                                   & ":" _
                                   & Hex(request.iSRVID).PadLeft(4, CChar("0")) _
                                   & ":" _
                                   & Hex(request.iPROVID).PadLeft(6, CChar("0")) _
                                   & "] -> "
        Dim consoleOutReason As String = " unknown"

        For Each udpserv As clsUDPIO In udpServers
            canceled = False
            With udpserv.serverobject

                If .Active Then 'If not Disabled by Serversettings

                    If request.CMD = CMDType.sCSRequest Then 'Avoid Re-Request
                        If .IsSCS Then
                            canceled = True
                            consoleOutReason = " got from SCS"
                        Else
                            canceled = False
                            request.PlainMessage(0) = &H0 'Make a normal CMD0 for non sCS Servers
                        End If
                    Else
                        If .IsSCS Then
                            request.PlainMessage(0) = &H55 'Make a special Request for sCS Severs
                        End If
                    End If

                    If request.SenderIP = .IP Then 'Not Re-Request
                        canceled = True
                        consoleOutReason = " avoid Loop to " & .IP
                    End If

                    If Not .supportedCAID.Contains(request.iCAID) And Not .supportedCAID.Count = 0 Then 'CAID not Supported by Serversettings and CAID List not Empty
                        canceled = True
                        'consoleOut &= "CAID"
                        consoleOutReason = " Denied in Server Config!"
                    End If

                    If Not .supportedSRVID.Contains(request.iSRVID) And Not .supportedSRVID.Count = 0 Then 'Srvid not Supported by Serversettings and Srvid List not empty
                        canceled = True
                        'consoleOut &= "SRVID"
                        consoleOutReason = " Denied in Server Config!"
                    End If

                    If Not .SendECMs Then 'Not allowed send Request by Serversettings
                        canceled = True
                        consoleOutReason = " Server not accept ECM"
                    End If

                    If request.UCRC.ContainsKey(.UCRC) Then 'Not Re-Request
                        canceled = True
                        consoleOutReason = " Avoid loop to UCRC"
                    End If

                    If .AutoBlocked Then
                        If .deniedSRVIDCAID.Contains(request.srvidcaid) Then 'Not allowed because server returns 44
                            canceled = True
                            consoleOutReason = " Server cannot answer"
                        End If
                    End If

                    If Not canceled Then

                        If Not .deniedSRVIDCAID.Contains(request.srvidcaid) Then
                            Using ms As New MemoryStream
                                Dim ucrcbytes() As Byte = BitConverter.GetBytes(.UCRC)
                                Array.Reverse(ucrcbytes)
                                ms.Write(ucrcbytes, 0, 4)
                                Dim encrypted() As Byte = AESCrypt.Encrypt(request.PlainMessage, .MD5_Password)
                                ms.Write(encrypted, 0, encrypted.Length)
                                udpserv.SendUDPMessage(ms.ToArray, Net.IPAddress.Parse(udpserv.serverobject.IP), udpserv.serverobject.Port)
                            End Using
                            canceled = False
                            Debug.WriteLine("sent -> " & .Nickname)
                            Exit For
                        Else
                            consoleOutReason &= " (" & .IP & ")"
                            Output(consoleOut & consoleOutReason, LogDestination.none, LogSeverity.info, ConsoleColor.Red)
                        End If
                    End If

                End If
            End With
        Next

        If canceled Then
            Output(consoleOut & consoleOutReason, LogDestination.none, LogSeverity.info, ConsoleColor.Red)
        End If

        'Catch ex As Exception

        'End Try
    End Sub

    Private Sub SendBroadcast()
        'Try

        Dim Broadcast2send As clsCMD1Answer
        SyncLock BroadcastQueue
            Broadcast2send = BroadcastQueue.Dequeue
        End SyncLock
        For Each udpserv As clsUDPIO In udpServers
            With udpserv.serverobject
                If .Active And .SendBroadcasts And Not Broadcast2send.SenderIP = .IP Then
                    Using ms As New MemoryStream
                        Dim ucrcbytes() As Byte = BitConverter.GetBytes(.UCRC)
                        Array.Reverse(ucrcbytes)
                        ms.Write(ucrcbytes, 0, 4)
                        Dim encrypted() As Byte = AESCrypt.Encrypt(Broadcast2send.TransformCMD0toCMD66Message, .MD5_Password)
                        ms.Write(encrypted, 0, encrypted.Length)
                        udpserv.SendUDPMessage(ms.ToArray, Net.IPAddress.Parse(.IP), .Port)
                    End Using
                    Debug.WriteLine("Broadcast to " & .Hostname & ":" & .Port)
                End If
            End With 'udpserv.serverobject
        Next
        'Catch ex As Exception

        'End Try
    End Sub


End Class
