﻿Imports SpeedCS.types

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

        'Try
        Dim strClientResult As String = "undefined"
        Dim logColor As ConsoleColor = ConsoleColor.White
        Dim plainRequest() As Byte

        Dim sClient As clsSettingsClients.clsClient = _
                    TryCast(CfgClients.Clients.FindByUCRC(message.ucrcInt),  _
                            clsSettingsClients.clsClient)

        If Not sClient Is Nothing Then
            If sClient.active Then
                plainRequest = AESCrypt.Decrypt(message.ByteMessage, sClient.MD5_Password)

                'Dim ecm As New clsCache.clsCAMDMsg

                'ecm.IncomingTimeStamp = Environment.TickCount
                'ecm.LoadFromPlainByteArray(plainRequest)
                'ecm.usercrc = message.ucrcInt
                'ecm.SenderUCRC = sClient.ucrc
                'ecm.SourceIP = message.sourceIP
                'ecm.SourcePort = message.sourcePort
                'strClientResult &= ecm.CMD

                Select Case CType(plainRequest(0), CMDType)

                    Case CMDType.ECMRequest  'Request

                        Dim requestedCAID As String = String.Empty
                        Dim requestedSRVID As String = String.Empty
                        Dim DoAnyway As Boolean = False
                        Dim RequestValid As Boolean = False

                        requestedCAID = CStr(Hex(plainRequest(10))) & CStr(Hex(plainRequest(11))).PadLeft(2, CChar("0"))
                        requestedSRVID = CStr(Hex(plainRequest(8))) & CStr(Hex(plainRequest(9))).PadLeft(2, CChar("0"))
                        requestedCAID = CStr(CUShort("&H" & requestedCAID))
                        requestedSRVID = CStr(CUShort("&H" & requestedSRVID))

                        For Each s As clsSettingsCardServers.clsCardServer In CfgCardServers.CardServers
                            If s.supportedCAID.Count > 0 Then
                                If s.supportedCAID.Contains(CUShort(requestedCAID)) Then
                                    If s.supportedSRVID.Count > 0 Then
                                        If s.supportedSRVID.Contains(CUShort(requestedSRVID)) Then
                                            RequestValid = True
                                            DoAnyway = True
                                        End If
                                    Else
                                        'if server has no supportedSRVID´s all requests must be sent
                                        DoAnyway = True
                                    End If
                                End If
                            Else
                                'if server has no supportedCAID´s all requests must be sent
                                DoAnyway = True
                            End If
                        Next

                        If RequestValid Or DoAnyway Then
                            For Each s As clsSettingsCardServers.clsCardServer In CfgCardServers.CardServers
                                If s.mapCAID.Count > 0 Then
                                    For Each iCAID In s.mapCAID
                                        Dim strTmp1 As UInt16 = CUShort("&H" & iCAID.Substring(0, 2))
                                        Dim strTmp2 As UInt16 = CUShort("&H" & iCAID.Substring(2, 2))
                                        If plainRequest(10).ToString.Equals(strTmp1.ToString) And plainRequest(11).ToString.Equals(strTmp2.ToString) Then
                                            Debug.WriteLine("Map CAID (Source:Destination): " & iCAID)
                                            plainRequest = mapCAID(plainRequest, iCAID) 'Modify ECM Request
                                            sClient.CurrentCAIDMapping = iCAID
                                            Exit For
                                        Else
                                            sClient.CurrentCAIDMapping = String.Empty
                                        End If
                                    Next
                                Else
                                    sClient.CurrentCAIDMapping = String.Empty
                                End If
                            Next

                            If sClient.logecm Then WriteECMToFile(plainRequest, sClient.Username & " Request: ")

                            If Not sClient.SourceIp = message.sourceIP Then sClient.SourceIp = message.sourceIP
                            If Not sClient.SourcePort = message.sourcePort Then sClient.SourcePort = CUShort(message.sourcePort)

                            If DateDiff(DateInterval.Second, sClient.lastRequest, Now) > 120 Then sClient.LoginTime = Now

                            sClient.lastRequest = Now
                            'Cache.Requests.Add(ecm)
                            'strClientResult = "Request: '" & sClient.Username & "' [" & ecm.ServiceName & "]"
                            If Not emmSender.Enabled Then emmSender.Start()

                            CacheManager.CMD0Requests.Add(plainRequest, message.ucrcInt, message.sourceIP, message.sourcePort)
                            Debug.WriteLine("Requests in Cachemanager: " & CacheManager.CMD0Requests.Count)
                            Debug.WriteLine("Requested Service Name: " & sClient.lastRequestedService.Name)
                        Else
                            logColor = ConsoleColor.Red
                            strClientResult = sClient.Username & " " & Hex(requestedCAID).PadLeft(4, CChar("0")) & ":" & Hex(requestedSRVID).PadLeft(4, CChar("0")) & " denied in Server Config!"
                        End If

                    Case CMDType.sCSRequest 'Special sCS Request
                        CacheManager.CMD0Requests.Add(plainRequest, message.ucrcInt, message.sourceIP, message.sourcePort)

                    Case CMDType.BroadCastResponse  'Answer
                        CacheManager.CMD1Answers.Add(plainRequest, message.sourceIP, message.sourcePort)

                    Case CMDType.CascadingRequest  'Request cascading (MPCS Source)
                        strClientResult = "Command 03 Cascading?!"

                    Case CMDType.NotFound  'Fehler ?!
                        strClientResult = "Command 44 Error"

                    Case CMDType.CRCError  'CRC false
                        strClientResult = "CRC of ECM wrong!"

                    Case CMDType.EMMResponse
                        Dim emmCRC As UInt32 = BitConverter.ToUInt32(plainRequest, 4)
                        With emmStack
                            SyncLock emmStack
                                If Not .ContainsKey(emmCRC) Then .Add(emmCRC, plainRequest)
                            End SyncLock

                            If sClient.logemm Then WriteEMMToFile(plainRequest, sClient.Username & " Response: ")

                            logColor = ConsoleColor.Cyan
                            strClientResult = "Emm Client Response. Stack: " & .Count
                        End With

                    Case Else
                        strClientResult = "Command " & Hex(plainRequest(0))

                End Select
            Else
                logColor = ConsoleColor.Red
                strClientResult = " !Account locked!"
            End If

        Else
            logColor = ConsoleColor.Red
            strClientResult = "Illegal access or Account locked"
            DebugOutputBytes(message.ucrc, "Illegal: " & message.ucrcInt & " ")
        End If

        If Not strClientResult = "undefined" Then
            Dim strLog As String = ""
            strLog &= strClientResult

            Dim adressData As String = message.sourceIP & ":" & message.sourcePort
            adressData = adressData.PadRight(22)

            Output("C " & adressData & _
                                strLog, _
                                LogDestination.none, _
                                LogSeverity.info, _
                                logColor)
        End If
        'Catch ex As Exception
        '    Output("Client incoming: " & ex.Message & vbCrLf & ex.StackTrace, LogDestination.file)
        'End Try

    End Sub

    Private Sub ServerIncoming(ByVal sender As Object, ByVal message As clsUDPIO.structUdpMessage)
        'Try

        Dim mSender As clsUDPIO = TryCast(sender, clsUDPIO)

        Dim plainRequest() As Byte = Nothing
        Dim strServerResult As String = "undefined"
        Dim logColor As ConsoleColor = ConsoleColor.Blue

        plainRequest = AESCrypt.Decrypt(message.ByteMessage, mSender.serverobject.MD5_Password)

        Dim ecm As New clsCache.clsCAMDMsg
        ecm.LoadFromPlainByteArray(plainRequest)
        ecm.SenderUCRC = mSender.serverobject.UCRC

        Select Case ecm.CMD

            Case CMDType.ECMRequest 'Request
                strServerResult = " CMD00 shouldn't be here"

            Case CMDType.ECMResponse  'Answer
                If mSender.serverobject.LogECM Then WriteECMToFile(plainRequest, "Server Response: ")

                CacheManager.CMD1Answers.Add(plainRequest, message.sourceIP, message.sourcePort)
                Debug.WriteLine("Incomming from " & message.sourceIP & " - Answers in Cachemanager: " & CacheManager.CMD1Answers.Count)

                'Dim found As Boolean = False
                'For Each sr As clsCache.clsCAMDMsg In Cache.ServerRequests
                '    If sr.ClientPID.Equals(ecm.ClientPID) Then
                '        ecm.ecmcrc = sr.ecmcrc
                '    End If
                'Next
                'For Each answer As clsCache.clsCAMDMsg In Cache.Answers
                '    If answer.ClientPID.Equals(ecm.ClientPID) Then
                '        found = True
                '        Exit For
                '    End If
                'Next

                'If Not found Then Cache.Answers.Add(ecm)
                strServerResult = "Answer: '" & mSender.serverobject.Username & "' [" & ecm.CAId.ToString("X4") & ":" & ecm.SRVId.ToString("X4") & ":" & ecm.PRID.ToString("X6") & "]"

                'CWLog for TSDEC 
                If CfgGlobals.CWLogIsEnabled Then CWlog.WriteCWlog(plainRequest)

            Case CMDType.EMMRequest  'Emm Zeuchs
                logColor = ConsoleColor.Cyan

                Dim serialAlreadyAssigned As Boolean = False
                Dim c As clsSettingsClients.clsClient
                Dim s As clsSettingsCardServers.clsCardServer

                If Not plainRequest(1) = &H70 Then
                    If mSender.serverobject.LogEMM Then WriteEMMToFile(plainRequest, "Server Request: ")

                    strServerResult = "EMM Request CMD05"

                    Dim cardSerial As UInt32 = BitConverter.ToUInt32(plainRequest, 40)
                    For Each c In CfgClients.Clients
                        If c.AUSerial = cardSerial Then serialAlreadyAssigned = True
                        strServerResult = "EMM Request CMD05 already assigned"
                        Exit For
                    Next

                    If Not serialAlreadyAssigned Then

                        For Each s In CfgCardServers.CardServers
                            For Each c In CfgClients.Clients
                                If ((c.AUServer = s.Nickname) And (c.active)) Or ((c.AUServer = "All") And (c.active)) Then
                                    If c.AUSerial = 0 Then
                                        c.AUSerial = cardSerial
                                        If DateDiff(DateInterval.Minute, c.AUisActiveSince, Date.Now) > 30 Then
                                            Dim ucrcbytes() As Byte = BitConverter.GetBytes(GetUserCRC(c.Username))
                                            Array.Reverse(ucrcbytes)
                                            Using ms As New MemoryStream
                                                ms.Write(ucrcbytes, 0, 4)
                                                Dim eArr() As Byte = AESCrypt.Encrypt(plainRequest, c.MD5_Password)
                                                ms.Write(eArr, 0, eArr.Length)
                                                UdpClientManager.SendUDPMessage(ms.ToArray, _
                                                                                Net.IPAddress.Parse(CStr(c.SourceIp)), _
                                                                                c.SourcePort)
                                                c.AUisActiveSince = Date.Now
                                            End Using
                                        End If
                                        strServerResult = "EMM Request CMD05 assigned to '" & c.Username & "'"
                                        Exit For
                                    End If ' Not c.AUSerial = 0
                                End If
                            Next
                            If c.AUSerial = cardSerial Then Exit For
                        Next
                    End If

                Else
                    strServerResult = "EMM Request CMD05 suppressed "
                End If

            Case CMDType.NotFound  'Fehler timeout/notfound whatever?!
                strServerResult = "not found CMD44"
                With mSender.serverobject
                    If .AutoBlocked Then
                        If Not .deniedSRVIDCAID.Contains(ecm.srvidcaid) Then
                            .deniedSRVIDCAID.Add(ecm.srvidcaid)
                        End If
                    End If
                End With
                DebugOutputBytes(plainRequest, "CMD44: ")

            Case CMDType.CRCError  'CRC false
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

        'Catch ex As Exception
        '    Output("Server incoming: " & ex.Message & vbCrLf & ex.StackTrace, LogDestination.file)
        'End Try
    End Sub


    Private Sub emmSender_Elapsed(ByVal sender As Object, ByVal e As System.Timers.ElapsedEventArgs) Handles emmSender.Elapsed
        'Debug.WriteLine("Emm Timer")

        SyncLock emmStack
            If emmStack.Count > 0 Then
                Dim emm() As Byte = TryCast(emmStack(emmStack.Keys(0)), Byte())

                'emm = modifyEmm(emm) 'ORF Emm Fix

                For Each udpserv As clsUDPIO In udpServers
                    With udpserv.serverobject
                        If .SendEMMs Then
                            Dim ucrcbytes() As Byte = BitConverter.GetBytes(GetUserCRC(.Username))
                            Array.Reverse(ucrcbytes)
                            Using ms As New MemoryStream
                                ms.Write(ucrcbytes, 0, 4)
                                Dim eArr() As Byte = AESCrypt.Encrypt(emm, .MD5_Password)
                                ms.Write(eArr, 0, eArr.Length)
                                udpserv.SendUDPMessage(ms.ToArray, Net.IPAddress.Parse(CStr(.IP)), .Port)
                            End Using
                        End If
                    End With 'udpserv.serverobject
                Next
                emmStack.RemoveAt(0)
            Else
                'Debug.WriteLine("Emm Stack empty")
            End If
        End SyncLock
        emmSender.Stop()
    End Sub

    Private Function modifyEmm(ByVal value() As Byte) As Byte()
        Dim retVal() As Byte = value
        If retVal(8) = &H32 And retVal(10) = &HD And retVal(11) = &H5 And retVal(15) = &H4 Then
            retVal(15) = &H0
            WriteEMMToFile(retVal, "SRVMod: ")
        End If

        If retVal(8) = &H0 And retVal(10) = &HD And retVal(11) = &H5 And retVal(15) = &H0 Then
            retVal(15) = &H4
            WriteEmmToFile(retVal, "CLIMod: ")
        End If

        Return retVal
    End Function

    Private Function mapCAID(ByVal value() As Byte, ByVal CAID As String) As Byte()
        Dim retVal() As Byte = value

        Dim strTmp1 = CUShort("&H" & CAID.Substring(5, 2))
        Dim strTmp2 = CUShort("&H" & CAID.Substring(7, 2))

        retVal(10) = CByte(strTmp1)
        retVal(11) = CByte(strTmp2)

        If CAID.Length > 9 Then
            Dim strTmp3 = CUShort("&H" & CAID.Substring(10, 2))
            Dim strTmp4 = CUShort("&H" & CAID.Substring(12, 2))
            Dim strTmp5 = CUShort("&H" & CAID.Substring(14, 2))
            Dim strTmp6 = CUShort("&H" & CAID.Substring(16, 2))

            retVal(12) = CByte(strTmp3)
            retVal(13) = CByte(strTmp4)
            retVal(14) = CByte(strTmp5)
            retVal(15) = CByte(strTmp6)
        End If

        Return retVal
    End Function

#Region "ErrorHandler"

    Private Sub ServerIncomingError(ByVal sender As Object, ByVal message As String)
        Dim udpClient As clsUDPIO = TryCast(sender, clsUDPIO)
        If Not udpClient Is Nothing Then
            If Not udpClient.endWasRequested Then
                If message.Contains("Receive Thread: Socket Closed") Then
                    Output("ServerIncomingError: " & message & " -> try restart " & udpClient.serverobject.Nickname, LogDestination.file)
                    udpClient.serverobject.deniedSRVIDCAID.Clear()
                    udpClient.OpenUDPConnection()
                ElseIf message.Contains("Receive Thread Exception") Then
                    Output("ServerIncomingError: " & message & " -> try restart " & udpClient.serverobject.Nickname, LogDestination.file)
                    udpClient.serverobject.deniedSRVIDCAID.Clear()
                    StopUDP()
                    StartUDP()
                End If
            End If
        Else
            Output("ClientIncomingError: " & message & " -> UDP Client destroyed " & udpClient.serverobject.Nickname, LogDestination.none)
        End If

    End Sub

    Private Sub ClientIncomingError(ByVal sender As Object, ByVal message As String)
        Dim udpClient As clsUDPIO = TryCast(sender, clsUDPIO)
        If Not udpClient Is Nothing Then
            If Not udpClient.endWasRequested Then
                Output("ClientIncomingError: " & message & " -> try restart " & udpClient.serverobject.Nickname, LogDestination.file)
                udpClient.serverobject.deniedSRVIDCAID.Clear()
                udpClient.OpenUDPConnection()
            End If
        Else
            Output("ClientIncomingError: " & message & " -> UDP Client destroyed " & udpClient.serverobject.Nickname, LogDestination.none)
        End If

    End Sub

#End Region

End Module
