' 
'	Copyright (C) 2009 SpeedCS Team
'	http://streamboard.gmc.to
'
'  This Program is free software; you can redistribute it and/or modify
'  it under the terms of the GNU General Public License as published by
'  the Free Software Foundation; either version 2, or (at your option)
'  any later version.
'   
'  This Program is distributed in the hope that it will be useful,
'  but WITHOUT ANY WARRANTY; without even the implied warranty of
'  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
'  GNU General Public License for more details.
'   
'  You should have received a copy of the GNU General Public License
'  along with GNU Make; see the file COPYING.  If not, write to
'  the Free Software Foundation, 675 Mass Ave, Cambridge, MA 02139, USA. 
'  http://www.gnu.org/copyleft/gpl.html
'
'

Public Class clsCache

    Public Enum CMDType
        ECMRequest = &H0
        ECMResponse = &H1
        CascadingRequest = &H3
        EMMRequest = &H5
        EMMResponse = &H6
        NotFound = &H44
        BroadCastResponse = &H66
        CRCError = &H99
        unknown = &HFF
    End Enum

    Public Class clsCAMDMsg
        Public CMD As clsCache.CMDType
        Public _ecmcrc As UInt32
        Public CHID As UInt16
        Public PRID As UInt32
        Public usercrc As UInt32
        Public data As Byte()
        Public reqtime As DateTime
        Public SenderUCRC As UInteger
        Public unknown As UInt16
        Public _OriginalLength As Integer
        Public OriginalRaw As Byte()
        Public IncomingTimeStamp As Long = Environment.TickCount
        Public srvidcaid As UInt32
        Public SourceIP As String
        Public SourcePort As Integer

        Public Property ecmcrc() As UInt32
            Get
                Return _ecmcrc
            End Get
            Set(ByVal value As UInt32)
                _ecmcrc = value
            End Set
        End Property

#Region "CAID"

        Public _CAId As UInt16
        '''<summary>
        ''' Conditional Access Identification - 
        ''' Zeigt an welcher Verschlüsselungsanbieter benutzt wird
        ''' Bsp.: 0x1700-0x17FF=BetaTechnik (BetaCrypt)
        ''' </summary>  
        Public Property CAId() As UInt16
            Get
                'Return CUShort(Math.Floor(_CAId / 256) + 256 * (_CAId And 255)) 'Convert to Little Endian
                Return _CAId
            End Get
            Set(ByVal value As UInt16)
                '_CAId = CUShort(Math.Floor(value / 256) + 256 * (value And 255)) 'Convert to Big Endian
                _CAId = value
            End Set
        End Property

#End Region

#Region "SRVID"

        Public _SRVID As UInt16
        '''<summary>
        ''' Service Identification - 
        ''' Zeigt an welcher Kanal genutzt wird
        ''' Bsp.: 000A Premiere1,000B Premiere2
        ''' </summary>  
        Public Property SRVId() As UInt16
            Get
                'Return CUShort(Math.Floor(_SRVID / 256) + 256 * (_SRVID And 255)) 'Convert to Little Endian
                Return _SRVID
            End Get
            Set(ByVal value As UInt16)
                '_SRVID = CUShort(Math.Floor(value / 256) + 256 * (value And 255)) 'Convert to Big Endian
                _SRVID = value
            End Set
        End Property

#End Region

#Region "ClientPID"

        Public _ClientPID As UInt16
        '''<summary>
        ''' Process ID es Clients - 
        ''' </summary>  
        Public Property ClientPID() As UInt16
            Get
                'Return CUShort(Math.Floor(_ClientPID / 256) + 256 * (_ClientPID And 255)) 'Convert to Little Endian
                Return _ClientPID
            End Get
            Set(ByVal value As UInt16)
                '_ClientPID = CUShort(Math.Floor(value / 256) + 256 * (value And 255)) 'Convert to Big Endian
                _ClientPID = value
            End Set
        End Property

#End Region

#Region "Load / GET Function"

        Public Sub LoadFromPlainByteArray(ByVal plainRequest As Byte())

            Try
                OriginalRaw = plainRequest

                _OriginalLength = plainRequest.Length
                reqtime = Now
                Select Case plainRequest(0)
                    Case &H0
                        CMD = CMDType.ECMRequest
                    Case &H1
                        CMD = CMDType.ECMResponse
                    Case &H3
                        CMD = CMDType.CascadingRequest
                    Case &H5
                        CMD = CMDType.EMMRequest
                    Case &H6
                        CMD = CMDType.EMMResponse
                    Case &H44
                        CMD = CMDType.NotFound
                    Case &H66
                        CMD = CMDType.BroadCastResponse
                    Case &H99
                        CMD = CMDType.CRCError
                    Case Else
                        Debug.WriteLine("unknown command: " & Hex(plainRequest(0)))
                        CMD = CMDType.unknown
                End Select
                unknown = BitConverter.ToUInt16(plainRequest, 6 - 4)
                unknown = CUShort(Math.Floor(unknown / 256) + 256 * (unknown And 255)) 'Convert to Little Endian
                ecmcrc = BitConverter.ToUInt32(plainRequest, 8 - 4)
                ecmcrc = CUInt(Math.Floor(ecmcrc / 65536) + 65536 * (ecmcrc And 65535)) 'Convert to Little Endian
                _SRVID = BitConverter.ToUInt16(plainRequest, 12 - 4)
                _SRVID = CUShort(Math.Floor(_SRVID / 256) + 256 * (_SRVID And 255)) 'Convert to Little Endian
                _CAId = BitConverter.ToUInt16(plainRequest, 14 - 4)
                _CAId = CUShort(Math.Floor(_CAId / 256) + 256 * (_CAId And 255)) 'Convert to Little Endian
                srvidcaid = BitConverter.ToUInt32(plainRequest, 12 - 4)
                srvidcaid = CUInt(Math.Floor(srvidcaid / 65536) + 65536 * (srvidcaid And 65535))

                Using ms As New IO.MemoryStream(plainRequest, 16 - 4, 4) 'Convert to Little Endian
                    Dim PRID_Bytes() As Byte = ms.ToArray
                    Array.Reverse(PRID_Bytes)
                    PRID = BitConverter.ToUInt32(PRID_Bytes, 0)
                    ms.Close()
                End Using
                'PRID = CUInt(Math.Floor(PRID / 65536) + 65536 * (PRID And 65535)) 'Convert to Little Endian
                _ClientPID = BitConverter.ToUInt16(plainRequest, 20 - 4)
                _ClientPID = CUShort(Math.Floor(_ClientPID / 256) + 256 * (_ClientPID And 255)) 'Convert to Little Endian
                CHID = BitConverter.ToUInt16(plainRequest, 22 - 4)
                CHID = CUShort(Math.Floor(CHID / 256) + 256 * (CHID And 255)) 'Convert to Little Endian

                Using ms As New IO.MemoryStream(plainRequest, 24 - 4, plainRequest(5 - 4))
                    data = ms.ToArray
                    If Not BitConverter.ToUInt32(crc32.ComputeHash(ms.ToArray), 0).Equals(BitConverter.ToUInt32(plainRequest, 8 - 4)) Then
                        CMD = CMDType.CRCError  'CRC false
                    End If
                    ms.Close()
                End Using
            Catch ex As Exception
                Output("LoadFromPlainByteArray() " & ex.Message & vbCrLf & ex.StackTrace, LogDestination.file)
                'Return Nothing
            End Try

        End Sub

        Private Function _ReturnAsByteArray() As Byte()

            Dim ret As Byte() = Nothing
            Try
                Using ms As New IO.MemoryStream
                    ms.Write(BitConverter.GetBytes(CMD), 0, 1)
                    ms.WriteByte(CByte(data.Length))
                    Dim tmp() As Byte

                    tmp = BitConverter.GetBytes(unknown)
                    'Array.Reverse(tmp)
                    ms.Write(tmp, 0, 2)
                    tmp = crc32.ComputeHash(data)
                    ms.Write(tmp, 0, 4)

                    tmp = BitConverter.GetBytes(_SRVID)
                    Array.Reverse(tmp)
                    ms.Write(tmp, 0, 2)

                    tmp = BitConverter.GetBytes(_CAId)
                    Array.Reverse(tmp)
                    ms.Write(tmp, 0, 2)

                    tmp = BitConverter.GetBytes(PRID)
                    Array.Reverse(tmp)
                    ms.Write(tmp, 0, 4)

                    tmp = BitConverter.GetBytes(_ClientPID)
                    Array.Reverse(tmp)
                    ms.Write(tmp, 0, 2)

                    tmp = BitConverter.GetBytes(CHID)
                    Array.Reverse(tmp)
                    ms.Write(tmp, 0, 2)

                    ms.Write(data, 0, data.Length)
                    For i As Long = ms.Length To _OriginalLength - 1
                        ms.WriteByte(255)
                    Next
                    ms.Close()
                    'DebugOutputBytes(OriginalRaw, "Origi:")
                    'DebugOutputBytes(ms.ToArray, "Plain:")
                    ret = ms.ToArray
                End Using
                Return ret
            Catch ex As Exception
                Output("ReturnAsByteArray() " & ex.Message & vbCrLf & ex.StackTrace)
                Return ret
            End Try

        End Function

        Public Function ReturnAsCryptedArray(ByVal key() As Byte) As Byte()

            Dim CryptBytes As Byte()
            Using ms As New IO.MemoryStream(_ReturnAsByteArray, 0, _ReturnAsByteArray.Length)
                CryptBytes = AESCrypt.Encrypt(ms.ToArray, key)
                ms.Close()
            End Using
            Dim ucrcbytes() As Byte = BitConverter.GetBytes(usercrc)
            Array.Reverse(ucrcbytes)
            Using ms As New IO.MemoryStream()
                ms.Write(ucrcbytes, 0, 4)
                ms.Write(CryptBytes, 0, CryptBytes.Length)
                ReturnAsCryptedArray = ms.ToArray
                ms.Close()
            End Using
            'DebugOutputBytes(ReturnAsCryptedArray, "Crypte: ")
        End Function

#End Region

        Public ReadOnly Property ServiceName() As String

            Get
                If Services.serviceIDList.Contains(CAId.ToString("X4") & ":" & SRVId.ToString("X4")) Then
                    Try
                        Return CStr(Services.serviceIDList.Item(CAId.ToString("X4") & ":" & SRVId.ToString("X4"))).Split(CChar("|"))(1)
                    Catch ex As Exception
                        Return CAId.ToString("X4") & " [" & SRVId.ToString("X4") & "]"
                    End Try
                Else
                    Return CAId.ToString("X4") & " [" & SRVId.ToString("X4") & "]"
                End If

            End Get

        End Property

        Public Function Clone() As clsCache.clsCAMDMsg
            Dim r As New clsCAMDMsg
            r._CAId = _CAId
            r.CHID = CHID
            r._ClientPID = _ClientPID
            r.CMD = CMD
            r.data = data
            r.ecmcrc = ecmcrc
            r.OriginalRaw = OriginalRaw
            r.PRID = PRID
            r.reqtime = reqtime
            r.SenderUCRC = SenderUCRC
            r._SRVID = _SRVID
            r.unknown = unknown
            r.usercrc = usercrc
            r._OriginalLength = _OriginalLength
            r.IncomingTimeStamp = IncomingTimeStamp
            r.srvidcaid = srvidcaid
            r.SourceIP = SourceIP
            r.SourcePort = SourcePort
            Return r
        End Function

    End Class

#Region "ECM Requests"

    Public Class clsRequests
        Inherits CollectionBase

        Public Sub Add(ByVal value As clsCAMDMsg)
            Dim SameRequestInQueue As Boolean = False
            'For Each e As clsECM In List
            'If e.ecmcrc.Equals(value.ecmcrc) And e.usercrc.Equals(value.ecmcrc) Then
            'SameRequestInQueue = True
            'Debug.WriteLine("Same Request in Queue")
            'Exit For
            'End If
            'Next

            'Hack: Client guckt sofort in cache
            Dim idx As Integer
            Dim ECMFoundInCache As Boolean = False
            'Do While idx < Cache.Answers.Count
            '    'For i As Integer = Cache.Answers.Count - 1 To 0 Step -1
            '    Dim cEcm As clsCache.clsCAMDMsg = Cache.Answers.Item(idx)
            '    If value.ecmcrc.Equals(cEcm.ecmcrc) Then
            '        Dim c As clsSettingsClients.clsClient = CfgClients.Clients.FindByUCRC(value.usercrc)
            '        UdpClientManager.SendUDPMessage(value.ReturnAsCryptedArray(c.MD5_Password), Net.IPAddress.Parse(c.SourceIp), c.SourcePort)
            '        Debug.WriteLine("Direct Cache access -> Sent")
            '        ECMFoundInCache = True
            '        Exit Do
            '    Else
            '        idx += 1
            '    End If
            'Loop
            'Next
            'For i As Integer = Cache.Answers.Count - 1 To 0 Step -1
            '    Dim cEcm As clsCache.clsCAMDMsg = Cache.Answers.Item(i)
            '    If value.ecmcrc.Equals(cEcm.ecmcrc) Then
            '        Dim c As clsSettingsClients.clsClient = CfgClients.Clients.FindByUCRC(value.usercrc)
            '        UdpClientManager.SendUDPMessage(value.ReturnAsCryptedArray(c.MD5_Password), Net.IPAddress.Parse(c.SourceIp), c.SourcePort)
            '        Debug.WriteLine("Direct Cache access -> Sent")
            '        ECMFoundInCache = True
            '        Exit For
            '    End If
            'Next

            If Not ECMFoundInCache Then
                List.Add(value)
                'If Not SameRequestInQueue Then
                Dim r As New RedirectRequests(value)
                Dim t As New Threading.Thread(AddressOf r.Start)
                t.IsBackground = True
                t.Start()
                'End If
            End If
            Clean()
        End Sub

        Public Sub Clean()

            Dim idx As Integer
            Do While idx < List.Count
                Dim c As clsCAMDMsg = CType(List(idx), clsCAMDMsg)
                If DateDiff(DateInterval.Second, c.reqtime, Now) > 20 Then
                    List.Remove(c)
                Else
                    idx += 1
                End If
            Loop

        End Sub

        Default Public Property Item(ByVal index As Integer) As clsCAMDMsg
            Get
                Return CType(List(index), clsCAMDMsg)
            End Get
            Set(ByVal Value As clsCAMDMsg)
                List(index) = Value
            End Set
        End Property

        Public Class RedirectRequests
            Private _ecm As clsCAMDMsg

            Public Sub New(ByVal e As clsCAMDMsg)
                _ecm = e.Clone
            End Sub

            Public Sub Start()
                'Wenn die Anfrage bereits in den wartenden Server requests ist,
                'dann nicht nochmal reinsellen und am besten warten bis server geantwortet hat.
                Dim idx As Integer
                Dim StartWaitTimer As New Stopwatch
                StartWaitTimer.Start()
                Do While idx < Cache.ServerRequests.Count
                    Dim c As clsCAMDMsg = CType(Cache.ServerRequests(idx), clsCAMDMsg)
                    If _ecm.ecmcrc.Equals(Cache.ServerRequests(idx).ecmcrc) Then
                        Do While True
                            If (StartWaitTimer.ElapsedMilliseconds > 10000) Then
                                Output("Timeout reached for " & _ecm.ServiceName, LogColor:=ConsoleColor.Red)
                                Return
                            End If
                            'Debug.WriteLine("Wait for Server Response" & StartWaitTimer.ElapsedMilliseconds)
                            Threading.Thread.Sleep(10)
                            If Cache.Answers.Contains(_ecm.ecmcrc) Then
                                'Output("Server has answered request after " & StartWaitTimer.ElapsedMilliseconds)
                                Return
                            End If
                        Loop
                        Exit Do
                    Else
                        idx += 1
                    End If
                Loop

                'HACK Wie bekomme ich eine eindeutige Zahl für einen Server Request hin?
                _ecm.ClientPID = CUShort(Cache.ServerRequests.GetUniqueId)
                Cache.ServerRequests.Add(_ecm)

                For Each udpserv As clsUDPIO In udpServers
                    Try
                        If udpserv.serverobject.SendECMs And Not udpserv.serverobject.IP.Equals(_ecm.SourceIP) And Not udpserv.serverobject.Port.Equals(_ecm.SourcePort) Then
                            If Not udpserv.serverobject.deniedSRVIDCAID.Contains(_ecm.srvidcaid) Then
                                Debug.WriteLine("Send to " & udpserv.serverobject.Hostname & ":" & udpserv.serverobject.Port & " with " & udpserv.serverobject.Username & ":" & udpserv.serverobject.Password & "-" & udpserv.serverobject.IP & "-" & _ecm.SourceIP)
                                _ecm.usercrc = udpserv.serverobject.UCRC
                                udpserv.SendUDPMessage(_ecm.ReturnAsCryptedArray(udpserv.serverobject.MD5_Password), Net.IPAddress.Parse(udpserv.serverobject.IP), udpserv.serverobject.Port)
                            End If
                        End If
                    Catch ex As Exception
                        Output("Send2Server:" & ex.Message & vbCrLf & ex.StackTrace, LogDestination.file)
                    End Try
                Next

            End Sub

        End Class

    End Class

    Private _Requests As New clsRequests

    Public Property Requests() As clsRequests
        Get
            Return _Requests
        End Get
        Set(ByVal value As clsRequests)
            _Requests = value
        End Set
    End Property

#End Region

#Region "ECM Pending Server Requests"

    Public Class clsServerRequests
        Inherits CollectionBase
        Public Sub Add(ByVal value As clsCAMDMsg)
            List.Add(value)
            Clean()
        End Sub
        Public Function IndexOf(ByVal value As clsCAMDMsg) As Integer
            Return List.IndexOf(value)
        End Function
        Public Sub Clean()

            Dim idx As Integer
            Do While idx < List.Count
                Dim c As clsCAMDMsg = CType(List(idx), clsCAMDMsg)
                If DateDiff(DateInterval.Second, c.reqtime, Now) > 20 Then
                    List.Remove(c)
                Else
                    idx += 1
                End If
            Loop

        End Sub
        Public ReadOnly Property GetUniqueId() As UInt16
            Get
                Dim r As New Random
                Return CUShort(r.Next(UInt16.MinValue, UInt16.MaxValue))
                'SyncLock List
                'Return CUShort(List.Count)
                'End SyncLock
            End Get
        End Property
        Default Public Property Item(ByVal index As Integer) As clsCAMDMsg
            Get
                Return CType(List(index), clsCAMDMsg)
            End Get
            Set(ByVal Value As clsCAMDMsg)
                List(index) = Value
            End Set
        End Property
    End Class
    Private _ServerRequests As New clsServerRequests

    Public Property ServerRequests() As clsServerRequests
        Get
            Return _ServerRequests
        End Get
        Set(ByVal value As clsServerRequests)
            _ServerRequests = value
        End Set
    End Property


#End Region

#Region "ECM Answers"

    Public Class clsAnswers
        Inherits CollectionBase
        Public Sub Add(ByVal value As clsCAMDMsg)
            'For Each e As clsECM In Cache.ServerRequests
            'If e.CAId.Equals(value.CAId) And e.ClientPID.Equals(value.ClientPID) And e.unknown.Equals(value.unknown) Then
            'value.raw2 = e.raw
            'End If
            'Next
            Dim isBroadcast As Boolean = value.CMD = CMDType.BroadCastResponse

            If isBroadcast Then
                value.CMD = CMDType.ECMResponse
            End If
            SyncLock List
                List.Add(value)
            End SyncLock
            Dim r As New RedirectAnswers(value)
            Dim t As New Threading.Thread(AddressOf r.Start)
            t.IsBackground = True
            t.Priority = Threading.ThreadPriority.BelowNormal
            t.Start()

            'Nur nicht Broadcast Packete broadcasten
            If Not isBroadcast Then
                Dim b As New Threading.Thread(AddressOf r.Broadcast)
                b.IsBackground = True
                b.Priority = Threading.ThreadPriority.BelowNormal
                b.Start()
            End If

            Clean()
        End Sub

        Public Sub Clean()
            SyncLock List
                Dim idx As Integer
                Do While idx < List.Count
                    Dim c As clsCAMDMsg = TryCast(List(idx), clsCAMDMsg)
                    If Not c Is Nothing Then
                        If DateDiff(DateInterval.Second, c.reqtime, Now) > 5 Then
                            List.Remove(c)
                        Else
                            idx += 1
                        End If
                    Else
                        idx += 1
                    End If
                Loop
            End SyncLock
        End Sub

        Public Sub Remove(ByVal value As clsCAMDMsg)
            List.Remove(value)
        End Sub

        Default Public Property Item(ByVal index As Integer) As clsCAMDMsg
            Get
                Return CType(List(index), clsCAMDMsg)
            End Get
            Set(ByVal Value As clsCAMDMsg)
                List(index) = Value
            End Set
        End Property

        Public Class RedirectAnswers
            Private _ecm As clsCAMDMsg

            Public Sub New(ByVal e As clsCAMDMsg)
                _ecm = e.Clone
            End Sub

            Public Sub Start()

                Dim idx As Integer

                'Server Pending Requests entfernen, die beantwortet sind.
                idx = 0
                SyncLock Cache.ServerRequests
                    Do While idx < Cache.ServerRequests.Count
                        Dim req As clsCAMDMsg = CType(Cache.ServerRequests(idx), clsCAMDMsg)
                        If req.ClientPID.Equals(_ecm.ClientPID) Then
                            Cache.ServerRequests.RemoveAt(idx)
                        Else
                            idx += 1
                        End If
                    Loop
                End SyncLock
                'Client Peding Requests
                idx = 0
                SyncLock Cache.ServerRequests
                    Do While idx < Cache.Requests.Count
                        Dim req As clsCAMDMsg = CType(Cache.Requests(idx), clsCAMDMsg)
                        If req.ecmcrc.Equals(_ecm.ecmcrc) Then
                            Dim c As clsSettingsClients.clsClient = CfgClients.Clients.FindByUCRC(req.usercrc)
                            _ecm.usercrc = req.usercrc
                            _ecm.ClientPID = req.ClientPID
                            If Not c Is Nothing Then
                                _ecm.CMD = CMDType.ECMResponse
                                UdpClientManager.SendUDPMessage(_ecm.ReturnAsCryptedArray(CfgClients.Clients.FindByUCRC(_ecm.usercrc).MD5_Password), Net.IPAddress.Parse(CStr(c.SourceIp)), c.SourcePort)

                                Dim adressData As String = c.SourceIp & ":" & c.SourcePort
                                adressData = adressData.PadRight(22)

                                Output("C " & adressData & c.Username & " " & _ecm.ServiceName & " found in " & Environment.TickCount - req.IncomingTimeStamp & "ms", LogDestination.none, LogSeverity.info, ConsoleColor.Green)
                            End If
                            'Debug.WriteLine("Remove sendet request")
                            Try
                                SyncLock Cache.Requests
                                    Cache.Requests.RemoveAt(idx)
                                End SyncLock
                            Catch ex As Exception
                                Debug.WriteLine(ex.Message)
                            End Try
                        Else
                            idx += 1
                        End If
                    Loop
                End SyncLock
                Exit Sub

            End Sub

            Public Sub Broadcast()
                Try

                    For Each udpserv As clsUDPIO In udpServers
                        If udpserv.serverobject.SendBroadcasts And udpserv.serverobject.Active Then
                            If _ecm.CMD = CMDType.ECMResponse Then
                                'Broadcast Message muss nun kopiert werden,
                                'da sie manchmal durch den RedirectAnswers.Start Thread verändert wird
                                Dim BroadcastMsg As clsCache.clsCAMDMsg = _ecm.Clone
                                BroadcastMsg.usercrc = udpserv.serverobject.UCRC
                                BroadcastMsg.CMD = CMDType.BroadCastResponse
                                udpserv.SendUDPMessage(BroadcastMsg.ReturnAsCryptedArray(udpserv.serverobject.MD5_Password), _
                                                       Net.IPAddress.Parse(udpserv.serverobject.IP), _
                                                       udpserv.serverobject.Port)

                                Debug.WriteLine("Broadcast to " & udpserv.serverobject.Hostname & ":" & udpserv.serverobject.Port)
                            Else
                                Debug.WriteLine("Avoid Loop " & udpserv.serverobject.Hostname)
                            End If
                        End If
                    Next

                Catch ex As Exception
                    Output("BroadCast() " & ex.Message & vbCrLf & ex.StackTrace)
                End Try
            End Sub

        End Class

        Public Function Contains(ByVal ecmcrc As UInt32) As Boolean
            'TODO hash zur schnelleren Abfrage
            Dim idx As Integer
            Do While idx < List.Count
                Dim c As clsCAMDMsg = TryCast(List(idx), clsCAMDMsg)
                If Not c Is Nothing Then
                    If c.ecmcrc.Equals(ecmcrc) Then
                        Return True
                    Else
                        idx += 1
                    End If
                Else
                    idx += 1
                End If
            Loop
            Return False
        End Function

    End Class

    Private _Answers As New clsAnswers
    Public Property Answers() As clsAnswers
        Get
            Return _Answers
        End Get
        Set(ByVal value As clsAnswers)
            _Answers = value
        End Set
    End Property

#End Region

End Class