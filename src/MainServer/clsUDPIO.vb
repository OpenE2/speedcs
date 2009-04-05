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

Imports System.Threading
Imports System.Net.Sockets
Imports System.Net
Imports System.Text
Imports System.IO

Public Class clsUDPIO

    Public Event UdpError(ByVal sender As Object, ByVal ErrorMessage As String)
    Public Event UdpMessageReceived(ByVal sender As Object, ByVal Message As structUdpMessage)

    Public Structure structUdpMessage
        Dim sourceIP As String
        Dim sourcePort As Integer
        Dim destinationIP As String
        Dim destinationPort As Integer
        Dim ucrc() As Byte
        Dim username As String
        Dim ByteMessage() As Byte
        Dim ucrcInt As UInteger
        Dim IsCrypted As Boolean
    End Structure

    Private UdpReceiveThread As Thread

    Private _oUdpClient As UdpClient
    Private _iPort As Integer = 0
    Private _localIP As IPAddress
    Private _boolThreadStopped As Boolean
    Private IEP As New IPEndPoint(IPAddress.Any, 0)
    Private LEP As New IPEndPoint(IPAddress.Any, 0)
    Public serverobject As clsSettingsCardServers.clsCardServer

    Public hadError As Boolean = False
    Public endWasRequested As Boolean = False

    Public Sub New(ByVal Port As Integer, Optional ByVal localIP As IPAddress = Nothing)
        _iPort = Port
        If localIP Is Nothing Then
            _localIP = IPAddress.Any
        Else
            _localIP = localIP
        End If
        LEP = New IPEndPoint(_localIP, _iPort)
    End Sub

    Public Sub New(ByVal localIP As IPAddress)
        _localIP = localIP
        _iPort = 0
        LEP = New IPEndPoint(_localIP, _iPort)
    End Sub

    Public Sub New()
        _localIP = IPAddress.Any
        _iPort = 0
        LEP = New IPEndPoint(_localIP, _iPort)
    End Sub


    Public Property RemotePoint() As IPEndPoint
        Get
            Return IEP
        End Get
        Set(ByVal value As IPEndPoint)
            IEP = value
        End Set
    End Property


    Public Property LocalIP() As String
        Get
            Return _localIP.ToString
        End Get
        Set(ByVal value As String)
            If value = "Any Adapter" Then
                _localIP = IPAddress.Any
            Else
                _localIP = IPAddress.Parse(value)
            End If

            LEP = New IPEndPoint(_localIP, _iPort)
        End Set
    End Property

    Public Property Port() As Integer
        Get
            Return _iPort
        End Get
        Set(ByVal value As Integer)
            _iPort = value
            LEP = New IPEndPoint(_localIP, _iPort)
        End Set
    End Property

    Private _IsRunning As Boolean
    Public ReadOnly Property IsRunning() As Boolean
        Get
            Return _IsRunning
        End Get
    End Property


    Public Sub OpenUDPConnection()
        hadError = False
        Try
            'Output("OpenUDPConnection() " & LocalIP & ":" & Port)
            If TryCast(_oUdpClient, UdpClient) Is Nothing Then
                _oUdpClient = New UdpClient(LEP)
            End If
            _boolThreadStopped = False
            UdpThreadStart()
            _IsRunning = True
        Catch ex As Exception
            RaiseEvent UdpError(Me, "OpenUDPConnection: " & ex.Message)
            _IsRunning = False
            hadError = True
        End Try
    End Sub

    Private Sub UdpThreadStart()
        Try
            If Not _boolThreadStopped Then
                UdpReceiveThread = New Thread(AddressOf ReceiveUdpMessages)
                UdpReceiveThread.IsBackground = True
                UdpReceiveThread.Start()
            End If

        Catch ex As Exception
            RaiseEvent UdpError(Me, "UdpThreadStart: " & ex.Message)
            _IsRunning = False
            hadError = True
        End Try
    End Sub

    Private Sub ReceiveUdpMessages()
        Try
            While Not _boolThreadStopped
                If Not DirectCast(_oUdpClient, UdpClient) Is Nothing Then
                    Dim receiveBytes() As Byte = _oUdpClient.Receive(IEP)
                    Dim ucrcbytes As Byte()

                    Using mS As New MemoryStream(receiveBytes, 0, 4)
                        ucrcbytes = mS.ToArray
                    End Using
                    Array.Reverse(ucrcbytes)

                    Dim sUdpMessage As New  _
                        structUdpMessage With {.sourceIP = IEP.Address.ToString, _
                                                .sourcePort = IEP.Port, _
                                                .destinationIP = LEP.Address.ToString, _
                                                .destinationPort = LEP.Port, _
                                                .ByteMessage = receiveBytes, _
                                                .ucrc = ucrcbytes, _
                                                .ucrcInt = BitConverter.ToUInt32(ucrcbytes, 0)}
                    RaiseEvent UdpMessageReceived(Me, sUdpMessage)
                    Thread.Sleep(0)

                End If
            End While

        Catch exS As SocketException
            RaiseEvent UdpError(Me, "Receive Thread: Socket Closed")
            hadError = True
        Catch ex As Exception
            _boolThreadStopped = True
            _IsRunning = False
            hadError = True
        End Try
    End Sub

    Public Sub CloseUDPConnection()
        endWasRequested = True

        _boolThreadStopped = True
        Try
            If Not UdpReceiveThread Is Nothing Then
                If Not TryCast(_oUdpClient, UdpClient) Is Nothing Then
                    _oUdpClient.Close()
                End If
                _IsRunning = False
            End If

        Catch ex As SocketException
            RaiseEvent UdpError(Me, "CloseUDPConnection: Socket Closed")
            _IsRunning = False
            hadError = True
        End Try
    End Sub

    Public Sub SendUDPMessage(ByVal Message As String)
        If Not TryCast(_oUdpClient, UdpClient) Is Nothing Then
            Dim sendbytes() As Byte = Encoding.ASCII.GetBytes(Message)
            _oUdpClient.Send(sendbytes, sendbytes.Length, IEP)
        End If
    End Sub

    Public Sub SendUDPMessage(ByVal Message As Byte(), ByVal IP As IPAddress, ByVal port As Integer)
        Try

            If Not TryCast(_oUdpClient, UdpClient) Is Nothing Then
                If Not _oUdpClient.Client Is Nothing Then
                    _oUdpClient.Send(Message, Message.Length, New IPEndPoint(IP, port))
                End If
            End If

        Catch ex As SocketException
            RaiseEvent UdpError(Me, ex.Message)
            hadError = True
        End Try
    End Sub
End Class
