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

    Private WithEvents tcpserver As New winsockserver

#Region "Start/Stop/Restart Server"

    Public Sub StartServer()

        If CfgGlobals.NewCamdUse Then
            Dim t As New Threading.Thread(AddressOf tcpserver.Listen)
            t.IsBackground = True
            t.Start()
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

    Public Class clsState
        Public wsocket As Socket = Nothing
        Public buffer() As Byte
        Public buffersize As Integer = 32767
        Public tag As String
        Public index As Integer
    End Class

    Public Class winsockserver
        Public Event onDataArrival(ByVal data() As Byte, ByVal state As clsState)
        Public Event onAccept(ByVal state As clsState)
        Public Event onError(ByVal err As String)
        Public Event onDisconnect(ByVal state As clsState)

        Private index As Integer = -1
        Public lst As New list

        Public Sub Send(ByVal CallId As String, ByVal varname As String, ByVal value As Object)
            Try

                For i As Integer = 0 To lst.count - 1
                    If lst.item(i) Is Nothing Then
                        Debug.Print(i & " is nothing")
                    Else
                        'Debug.Print("Connected:" & lst.item(i).wsocket.Connected)
                        'Debug.Print(lst.item(i).index)
                        Dim data() As Byte
                        'Dim str As String = "<callid>" & CallId & "</callid><varname>" & varname & "</varname><value>" & value & "</value>"
                        'data = varname & value
                        'Output("Sende nach " & lst.item(i).wsocket.RemoteEndPoint.ToString & ": " & str)
                        data = System.Text.Encoding.ASCII.GetBytes("None")
                        'lst.item(i).wsocket.Send(data, str.Length)
                        lst.item(i).wsocket.Send(data, data.Length, SocketFlags.None)
                    End If
                Next
            Catch ex As Exception
                Output("Send:" & ex.Message)
            End Try

        End Sub

        Public Sub Listen()

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
            Dim s As Socket = CType(ar.AsyncState, Socket)
            Dim ss As Socket
            'Debug.WriteLine("Accept")

            Dim state As New clsState
            Try
                index = index + 1
                ss = s.EndAccept(ar)
                state.wsocket = ss
                state.index = lst.add(state)

                ReDim state.buffer(state.buffersize)
                s.BeginAccept(New AsyncCallback(AddressOf Accept), s)
                ss.BeginReceive(state.buffer, 0, state.buffersize, SocketFlags.None, New AsyncCallback(AddressOf recieve), state)
            Catch e As Exception
                RaiseEvent onError(e.ToString())
            End Try
            RaiseEvent onAccept(state)

        End Sub

        Private Sub recieve(ByVal ar As IAsyncResult)
            Dim state As clsState = CType(ar.AsyncState, clsState)
            Dim bytes As Integer
            Dim data() As Byte
            Try
                bytes = state.wsocket.EndReceive(ar)
            Catch e As Exception
                RaiseEvent onError(e.ToString())

            End Try

            data = state.buffer
            If bytes = 0 Then
                Try
                    state.wsocket.Shutdown(SocketShutdown.Both)
                    state.wsocket.Close()
                    index = index - 1
                    lst.remove(state.index)
                    RaiseEvent onDisconnect(state)

                    Exit Sub
                Catch ex As Exception
                    RaiseEvent onError(ex.ToString())

                End Try
            End If
            Try
                state.wsocket.BeginReceive(state.buffer, 0, state.buffersize, SocketFlags.None, New AsyncCallback(AddressOf recieve), state)

            Catch ex As Exception
                RaiseEvent onError(ex.ToString())

            End Try
            RaiseEvent onDataArrival(data, state)
            'Output("in")
            'Dim datain As String = System.Text.Encoding.ASCII.GetString(data)
            'datain = Left(datain, bytes)
            'If Extract(datain, "alive") <> "" Then
            '    MakeClientEntry(state.index, "lastalive", Now)
            '    Dim senddata() As Byte = System.Text.Encoding.ASCII.GetBytes("<Alive>1</Alive>")
            '    state.wsocket.Send(senddata, senddata.Length, SocketFlags.None)
            'End If
            'If Extract(datain, "user") <> "" Then
            '    MakeClientEntry(state.index, "user", Extract(datain, "user"))
            'End If
            'Dim VersionString As String = Extract(datain, "VersionMajor") & "." & Extract(datain, "VersionMinor") & "." & Extract(datain, "VersionRevision")
            'If VersionString <> ".." Then MakeClientEntry(state.index, "clientversion", VersionString)
            'Output("out")
        End Sub

        Sub endconn(ByVal ind As Integer)
            Dim stat As clsState
            stat = lst.item(ind)
            If stat Is Nothing Then
            Else
                Output("Verbindung wird getrennt.")
                stat.wsocket.Shutdown(SocketShutdown.Both)
                stat.wsocket.Close()
                lst.remove(ind)
            End If


        End Sub

    End Class

    Public Class list
        Private Structure listtype
            Dim state As clsState

        End Structure
        Private lst(32767) As List.listtype
        Private index As Integer = -1
        Public Function add(ByRef Value As clsState) As Integer
            If index = -1 Then
                index = index + 1
                lst(index).state = Value
                add = index
            Else
                Dim i As Integer
                Dim a As Integer = -1
                For i = 0 To index
                    If lst(i).state Is Nothing Then
                        a = i
                        Exit For
                    End If
                Next
                If a = -1 Then
                    index = index + 1
                    lst(index).state = Value
                    add = index
                Else
                    lst(a).state = Value
                    add = a
                End If
            End If
        End Function

        Public Function count() As Integer
            count = index + 1
        End Function
        Public Function remove(ByVal index2 As Integer) As Integer
            lst(index2).state = Nothing
            'index = index - 1
        End Function
        Public Function item(ByVal index2 As Integer) As clsState
            item = lst(index2).state
        End Function
    End Class

    Private Sub tcpserver_onAccept(ByVal state As clsState) Handles tcpserver.onAccept
        Output("tcpserver_onAccept:" & state.ToString)
    End Sub

    Private Sub tcpserver_onDataArrival(ByVal data() As Byte, ByVal state As clsState) Handles tcpserver.onDataArrival
        Output("tcpserver_onDataArrival:" & Text.Encoding.ASCII.GetString(data))
    End Sub

    Private Sub tcpserver_onDisconnect(ByVal state As clsState) Handles tcpserver.onDisconnect
        Output("tcpserver_onDisconnect:" & state.ToString)
    End Sub

    Private Sub tcpserver_onError(ByVal err As String) Handles tcpserver.onError
        Output("tcpserver_onError:" & err)
    End Sub
End Class
