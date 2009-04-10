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

Public Class clsTCPIO

#Region "Start/Stop/Restart Server"

    Public Sub StartServer()

        If CfgGlobals.NewCamdUse Then
            Listen()
        Else
            Output("NewCamd Server disabled")
        End If

    End Sub

    Public Sub StopServer()

    End Sub

    Public Sub RestartServer()

        StopServer()

        StartServer()

    End Sub

#End Region

    Private Sub Listen()

        Dim s As New Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
        Dim lhe As IPHostEntry = System.Net.Dns.GetHostEntry(Environment.MachineName)
        Dim adruse As IPAddress = Net.IPAddress.Any
        If adruse Is Nothing Then adruse = lhe.AddressList(0)
        Dim lep As IPEndPoint = New IPEndPoint(adruse, CfgGlobals.NewCamdPort)
        s.Bind(lep)
        Output("NewCamd Server listen on " & adruse.ToString & ":" & CfgGlobals.NewCamdPort)

        s.Listen(1000)
        s.BeginAccept(New AsyncCallback(AddressOf Accept), s)

    End Sub

    Private Sub Accept(ByVal ar As IAsyncResult)

        Debug.WriteLine("Accept")
        Dim s As Socket = CType(ar.AsyncState, Socket)
        Dim ss As Socket
        Try
            ss = s.EndAccept(ar)
            s.BeginAccept(New AsyncCallback(AddressOf Accept), s)
            'ss.BeginReceive(state.buffer, 0, state.bufferSize, SocketFlags.None, New AsyncCallback(AddressOf recieve), state)
        Catch e As Exception
            'RaiseEvent onError(e.ToString())
        End Try
        'RaiseEvent onAccept(state)

    End Sub

End Class
