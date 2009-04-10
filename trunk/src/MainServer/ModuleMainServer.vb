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

Imports System.Net
Imports System.Net.Sockets
Imports System.IO

Module ModuleMainServer
    'Private swatch As New Stopwatch
    Private WithEvents emmSender As New Timers.Timer(6000)

#Region "udpServerManagers"

    Public Class clsUdpServers

        Inherits CollectionBase
        Default Public Property Item(ByVal index As Integer) As clsUDPIO
            Get
                Return CType(List(index), clsUDPIO)
            End Get
            Set(ByVal Value As clsUDPIO)
                List(index) = Value
            End Set
        End Property

        Public Sub Add(ByVal column As clsUDPIO)
            List.Add(column)
        End Sub

        Public Function IndexOf(ByVal value As clsUDPIO) As Integer
            Return List.IndexOf(value)
        End Function

        Public Sub Insert(ByVal index As Integer, ByVal value As clsUDPIO)
            List.Insert(index, value)
        End Sub

        Public Sub Remove(ByVal value As clsUDPIO)
            List.Remove(value)
        End Sub

        Public Function Contains(ByVal value As clsUDPIO) As Boolean
            Return List.Contains(value)
        End Function

    End Class

    Public udpServers As New clsUdpServers

#End Region

#Region "Start/Stop"

    Public Sub StartUDP()

        If CfgGlobals.cs357xUse Then
            'Empfangsport für clients
            UdpClientManager = New clsUDPIO(CfgGlobals.cs357xPort)
            AddHandler UdpClientManager.UdpMessageReceived, AddressOf ClientIncoming
            AddHandler UdpClientManager.UdpError, AddressOf ClientIncomingError
            UdpClientManager.OpenUDPConnection()
            Output("cs357x Server listen on " & UdpClientManager.LocalIP.ToString & ":" & UdpClientManager.Port)
        Else
            Output("cs357x Server disabled")
        End If

        'CardServer Port
        For Each CardServer As SpeedCS.clsSettingsCardServers.clsCardServer In CfgCardServers.CardServers
            If CardServer.Active Then
                Dim udpio As New clsUDPIO(0)
                udpio.serverobject = CardServer
                'CardServer.udpServerManager = New clsUDPIO(CardServer.Port)
                'UdpServerManager = New clsUDPIO(CardServer.RemotePort)
                'UdpServerManager.RemotePoint = New IPEndPoint(IPAddress.Parse(Settings.ServerAddress), Settings.ServerPort)
                AddHandler udpio.UdpMessageReceived, AddressOf ServerIncoming
                AddHandler udpio.UdpError, AddressOf ServerIncomingError
                udpio.OpenUDPConnection()
                udpServers.Add(udpio)
                Output("Opening Server " & CardServer.Hostname & ":" & CardServer.Port & " with username " & CardServer.Username)
                'UdpSenderS = New UdpClient(0)
            End If
        Next


    End Sub

    Public Sub StopUDP()

        Try
            If Not UdpClientManager Is Nothing Then
                UdpClientManager.CloseUDPConnection()
            End If

            For Each s As clsUDPIO In udpServers
                RemoveHandler s.UdpError, AddressOf ServerIncomingError
                RemoveHandler s.UdpMessageReceived, AddressOf ServerIncoming
                s.CloseUDPConnection()
                s = Nothing
            Next
            udpServers.Clear()
            'UdpServerManager.CloseUDPConnection()
        Catch ex As Exception
            Output("StopUDP:" & ex.Message & ex.StackTrace, LogDestination.file)
        End Try

    End Sub

#End Region

    Private Sub ClientIncoming(ByVal sender As Object, ByVal message As clsUDPIO.structUdpMessage)

        Try
            Dim strClientResult As String = "undefined"
            Dim logColor As ConsoleColor = ConsoleColor.White
            Dim plainRequest() As Byte

            Dim sClient As clsSettingsClients.clsClient = _
                        TryCast(CfgClients.Clients.FindByUCRC(message.ucrcInt),  _
                                clsSettingsClients.clsClient)

            If Not sClient Is Nothing Then
                If sClient.active Then
                    plainRequest = AESCrypt.Decrypt(message.ByteMessage, sClient.MD5_Password)

                    Dim ecm As New clsCache.clsCAMDMsg

                    ecm.IncomingTimeStamp = Environment.TickCount
                    ecm.LoadFromPlainByteArray(plainRequest)
                    ecm.usercrc = message.ucrcInt
                    ecm.SenderUCRC = sClient.ucrc
                    ecm.SourceIP = message.sourceIP
                    ecm.SourcePort = message.sourcePort
                    strClientResult &= ecm.CMD

                    Select Case ecm.CMD

                        Case clsCache.CMDType.ECMRequest  'Request
                            If Not sClient.SourceIp = message.sourceIP Then sClient.SourceIp = message.sourceIP
                            If Not sClient.SourcePort = message.sourcePort Then sClient.SourcePort = CUShort(message.sourcePort)
                            sClient.lastrequest = Now
                            Cache.Requests.Add(ecm)
                            strClientResult = "Request: '" & sClient.Username & "' [" & ecm.ServiceName & "]"
                            If Not emmSender.Enabled Then emmSender.Start()

                        Case clsCache.CMDType.BroadCastResponse  'Answer
                            'ecm.CMD = &H99
                            Cache.Answers.Add(ecm)
                            logColor = ConsoleColor.DarkYellow
                            strClientResult = "Broadcast: '" & sClient.Username & "' [" & ecm.ServiceName & "]"

                        Case clsCache.CMDType.CascadingRequest  'Request cascading (MPCS Source)
                            strClientResult = "Command 03 Cascading?!"

                        Case clsCache.CMDType.NotFound  'Fehler ?!
                            strClientResult = "Command 44 Error"

                        Case clsCache.CMDType.CRCError  'CRC false
                            strClientResult = "CRC of ECM wrong!"

                        Case clsCache.CMDType.EMMResponse
                            strClientResult = "Emm Client Response"
                            logColor = ConsoleColor.Cyan

                            Dim emmCRC As UInt32 = BitConverter.ToUInt32(plainRequest, 4)

                            SyncLock emmSender
                                If Not emmStack.ContainsKey(emmCRC) Then emmStack.Add(emmCRC, plainRequest)
                            End SyncLock

                            strClientResult = "Emm Client Response. Stack: " & emmStack.Count
                            'For Each udpserv As clsUDPIO In udpServers
                            '    If sClient.AUServer = udpserv.serverobject.IP And udpserv.serverobject.SendEMMs Then
                            '        Dim ucrcbytes() As Byte = BitConverter.GetBytes(GetUserCRC(udpserv.serverobject.Username))
                            '        Array.Reverse(ucrcbytes)
                            '        Using ms As New MemoryStream
                            '            ms.Write(ucrcbytes, 0, 4)
                            '            Dim eArr() As Byte = AESCrypt.Encrypt(plainRequest, udpserv.serverobject.MD5_Password)
                            '            ms.Write(eArr, 0, eArr.Length)
                            '            udpserv.SendUDPMessage(ms.ToArray, Net.IPAddress.Parse(CStr(udpserv.serverobject.IP)), udpserv.serverobject.Port)
                            '        End Using
                            '    End If
                            'Next



                            'WriteEcmToFile(plainRequest, "CMD6: ")
                        Case Else
                            strClientResult = "Command " & Hex(ecm.CMD)

                    End Select
                Else
                    logColor = ConsoleColor.Red
                    strClientResult = " !Account locked!"
                End If

            Else
                logColor = ConsoleColor.Red
                strClientResult = "Illegal access or Account locked"
            End If

            Dim strLog As String = ""

            strLog &= strClientResult

            Dim adressData As String = message.sourceIP & ":" & message.sourcePort
            adressData = adressData.PadRight(22)

            Output("C " & adressData & _
                             strLog, LogDestination.none, _
                                                LogSeverity.info, _
                                                logColor)

        Catch ex As Exception
            Output("Client incoming: " & ex.Message & vbCrLf & ex.StackTrace, LogDestination.file)
        End Try

    End Sub

    Private Sub ServerIncoming(ByVal sender As Object, ByVal message As clsUDPIO.structUdpMessage)
        Try

            Dim mSender As clsUDPIO = TryCast(sender, clsUDPIO)
            Dim plainRequest() As Byte = Nothing
            Dim strServerResult As String = "undefined"
            Dim logColor As ConsoleColor = ConsoleColor.Blue

            plainRequest = AESCrypt.Decrypt(message.ByteMessage, mSender.serverobject.MD5_Password)

            Dim ecm As New clsCache.clsCAMDMsg
            ecm.LoadFromPlainByteArray(plainRequest)
            ecm.SenderUCRC = mSender.serverobject.UCRC

            Select Case ecm.CMD

                Case clsCache.CMDType.ECMRequest 'Request
                    strServerResult = " CMD00 shouldn't be here"

                Case clsCache.CMDType.ECMResponse  'Answer
                    Dim found As Boolean = False
                    For Each sr As clsCache.clsCAMDMsg In Cache.ServerRequests
                        If sr.ClientPID.Equals(ecm.ClientPID) Then
                            ecm.ecmcrc = sr.ecmcrc
                        End If
                    Next
                    For Each answer As clsCache.clsCAMDMsg In Cache.Answers
                        If answer.ClientPID.Equals(ecm.ClientPID) Then
                            found = True
                            Exit For
                        End If
                    Next

                    If Not found Then Cache.Answers.Add(ecm)
                    strServerResult = "Answer: '" & mSender.serverobject.Username & "' [" & ecm.CAId.ToString("X4") & ":" & ecm.SRVId.ToString("X4") & "]"


                Case clsCache.CMDType.EMMRequest  'Emm Zeuchs
                    logColor = ConsoleColor.Cyan
                    If Not plainRequest(1) = &H70 Then
                        strServerResult = "EMM Request CMD05"
                        Dim c As clsSettingsClients.clsClient
                        For Each c In CfgClients.Clients
                            If c.AUServer = message.sourceIP And c.active Then
                                If DateDiff(DateInterval.Minute, c.AUisActiveSince, Date.Now) > 30 Then
                                    Dim ucrcbytes() As Byte = BitConverter.GetBytes(GetUserCRC(c.Username))
                                    Array.Reverse(ucrcbytes)
                                    Using ms As New MemoryStream
                                        ms.Write(ucrcbytes, 0, 4)
                                        Dim eArr() As Byte = AESCrypt.Encrypt(plainRequest, c.MD5_Password)
                                        ms.Write(eArr, 0, eArr.Length)
                                        UdpClientManager.SendUDPMessage(ms.ToArray, Net.IPAddress.Parse(CStr(c.SourceIp)), c.SourcePort)
                                        c.AUisActiveSince = Date.Now
                                    End Using
                                End If
                            End If
                        Next
                        'WriteEcmToFile(plainRequest, "CMD5: ")
                    Else
                        strServerResult = "EMM Request CMD05 suppressed "
                    End If

                Case clsCache.CMDType.NotFound  'Fehler timeout/notfound whatever?!
                    strServerResult = "not found CMD44"
                    If Not mSender.serverobject.deniedSRVIDCAID.Contains(ecm.srvidcaid) Then
                        mSender.serverobject.deniedSRVIDCAID.Add(ecm.srvidcaid)
                    End If
                    DebugOutputBytes(plainRequest, "CMD44: ")
                Case clsCache.CMDType.CRCError  'CRC false
                    strServerResult = "CRC of ECM wrong!"

                Case Else
                    strServerResult = "Command " & ecm.CMD

            End Select

            Dim strLog As String = ""
            strLog &= strServerResult

            Dim adressData As String = message.sourceIP & ":" & message.sourcePort
            adressData = adressData.PadRight(22)

            Output("S " & adressData & _
                            strLog, LogDestination.none, _
                                                LogSeverity.info, _
                                                logColor)

        Catch ex As Exception
            Output("Server incoming: " & ex.Message & vbCrLf & ex.StackTrace, LogDestination.file)
        End Try
    End Sub


    Private Sub emmSender_Elapsed(ByVal sender As Object, ByVal e As System.Timers.ElapsedEventArgs) Handles emmSender.Elapsed
        'Debug.WriteLine("Emm Timer")

        SyncLock emmStack
            If emmStack.Count > 0 Then
                Dim emm() As Byte = TryCast(emmStack(emmStack.Keys(0)), Byte())
                If Not emm Is Nothing Then
                    'Debug.WriteLine("Emm Stack full")
                    For Each udpserv As clsUDPIO In udpServers
                        If udpserv.serverobject.SendEMMs Then
                            Dim ucrcbytes() As Byte = BitConverter.GetBytes(GetUserCRC(udpserv.serverobject.Username))
                            Array.Reverse(ucrcbytes)
                            Using ms As New MemoryStream
                                ms.Write(ucrcbytes, 0, 4)
                                Dim eArr() As Byte = AESCrypt.Encrypt(emm, udpserv.serverobject.MD5_Password)
                                ms.Write(eArr, 0, eArr.Length)
                                udpserv.SendUDPMessage(ms.ToArray, Net.IPAddress.Parse(CStr(udpserv.serverobject.IP)), udpserv.serverobject.Port)
                            End Using
                        End If
                    Next
                    emmStack.RemoveAt(0)
                Else
                    'Debug.WriteLine("Emm is nothing")
                End If
            Else
                'Debug.WriteLine("Emm Stack empty")
            End If
        End SyncLock
        emmSender.Stop()
    End Sub




#Region "ErrorHandler"

    Private Sub ServerIncomingError(ByVal sender As Object, ByVal message As String)
        Dim udpClient As clsUDPIO = TryCast(sender, clsUDPIO)
        If Not udpClient Is Nothing Then
            If Not udpClient.endWasRequested And udpClient.hadError Then
                udpClient.CloseUDPConnection()
                udpClient.OpenUDPConnection()
            End If
        End If
        Output("ServerIncomingError: " & message & " ->try restart", LogDestination.file)
    End Sub

    Private Sub ClientIncomingError(ByVal sender As Object, ByVal message As String)
        Dim udpClient As clsUDPIO = TryCast(sender, clsUDPIO)
        If Not udpClient Is Nothing Then
            If Not udpClient.endWasRequested And udpClient.hadError Then
                udpClient.CloseUDPConnection()
                udpClient.OpenUDPConnection()
            End If
        End If
        Output("ClientIncomingError: " & message & " ->try restart", LogDestination.file)
    End Sub

#End Region

End Module
