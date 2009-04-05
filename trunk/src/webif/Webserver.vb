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

Public Class clsWebserver
    Public DoStop As Boolean = False
    Private ss As Socket = New Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
    Private tListener As Thread
    Private tReceiver As Thread

    Public Sub New()
        'Output("new")
        tListener = New Thread(AddressOf RunServerThread)
        't.SetApartmentState(ApartmentState.MTA)
        ' Run the service thread in the background
        ' as low priority, as to not take up the
        ' processor
        tListener.IsBackground = True
        tListener.Priority = ThreadPriority.BelowNormal
    End Sub

    Public Sub StopServer()
        Output("StopServer()")
        Me.DoStop = True
        ss.Close()
    End Sub

    Public Sub StartServer()
        Output("StartServer()")
        tListener.Start()
        
    End Sub

    Private Sub RunServerThread()
        Try
            ' Get IP address of the adapter to run the server on
            'Dim address As IPAddress = IPAddress.Parse("127.0.0.1")
            Dim address As IPAddress = IPAddress.Any
            ' map the end point with IP Address and Port
            Dim EndPoint As IPEndPoint = New IPEndPoint(address, CfgGlobals.AdminPort)

            ' Create a new socket and bind it to the address and port and listen.

            Output("Webserver listening on port " & CfgGlobals.AdminPort, LogDestination.eventlog, LogSeverity.info)
            ss.Bind(EndPoint)
            ss.Listen(20)

            Do While Not Me.DoStop
                ' Wait for an incoming connections
                Dim sock As Socket = ss.Accept()
                ' Connection accepted
                ' Initialise the Server class
                Dim ServerRun As New Server(sock)
                ' Create a new thread to handle the connection
                Dim t As Thread = New Thread(AddressOf ServerRun.HandleConnection)
                t.IsBackground = True
                t.Priority = ThreadPriority.BelowNormal
                t.Name = "Handle Connection"
                t.Start()
                ' Loop and wait for more connections
            Loop
            Output("Webserver closed", LogDestination.eventlog, LogSeverity.info)
            ss.Close()
        Catch socketex As SocketException
            Select Case socketex.ErrorCode
                Case 10048
                    Output("The TCP Port " & CfgGlobals.AdminPort & " is already open by another application!", LogDestination.file, LogSeverity.fatal, ConsoleColor.Red)
                Case Else
                    Output(socketex.Message, LogDestination.file, LogSeverity.fatal)
            End Select
        Catch ex As Exception
            Output(ex.Message, LogDestination.file, LogSeverity.fatal)
            ss.Close()
        End Try
    End Sub

End Class
