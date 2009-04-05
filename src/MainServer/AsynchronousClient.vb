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

'Imports System
'Imports System.Net
'Imports System.Net.Sockets
'Imports System.Threading
'Imports System.Text


'' State object for receiving data from remote device.

'Public Class StateObject
'    ' Client socket.
'    Public workSocket As Socket = Nothing
'    ' Size of receive buffer.
'    Public Const BufferSize As Integer = 256
'    ' Receive buffer.
'    Public buffer(BufferSize) As Byte
'    ' Received data string.
'    Public sb As New StringBuilder
'End Class 'StateObject


'Public Class AsynchronousClient
'    ' The port number for the remote device.
'    Private Const port As Integer = 11000

'    ' ManualResetEvent instances signal completion.
'    Private Shared connectDone As New ManualResetEvent(False)
'    Private Shared sendDone As New ManualResetEvent(False)
'    Private Shared receiveDone As New ManualResetEvent(False)

'    ' The response from the remote device.
'    Private Shared response As String = String.Empty


'    Public Shared Sub Main()
'        ' Establish the remote endpoint for the socket.
'        ' For this example use local machine.
'        Dim ipHostInfo As IPHostEntry = Dns.Resolve(Dns.GetHostName())
'        Dim ipAddress As IPAddress = ipHostInfo.AddressList(0)
'        Dim remoteEP As New IPEndPoint(ipAddress, port)

'        ' Create a TCP/IP socket.
'        Dim client As New Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)

'        ' Connect to the remote endpoint.
'        client.BeginConnect(remoteEP, New AsyncCallback(AddressOf ConnectCallback), client)

'        ' Wait for connect.
'        connectDone.WaitOne()

'        ' Send test data to the remote device.
'        Send(client, "This is a test<EOF>")
'        sendDone.WaitOne()

'        ' Receive the response from the remote device.
'        Receive(client)
'        receiveDone.WaitOne()

'        ' Write the response to the console.
'        Console.WriteLine("Response received : {0}", response)

'        ' Release the socket.
'        client.Shutdown(SocketShutdown.Both)
'        client.Close()
'    End Sub 'Main


'    Private Shared Sub ConnectCallback(ByVal ar As IAsyncResult)
'        ' Retrieve the socket from the state object.
'        Dim client As Socket = CType(ar.AsyncState, Socket)

'        ' Complete the connection.
'        client.EndConnect(ar)

'        Console.WriteLine("Socket connected to {0}", client.RemoteEndPoint.ToString())

'        ' Signal that the connection has been made.
'        connectDone.Set()
'    End Sub 'ConnectCallback


'    Private Shared Sub Receive(ByVal client As Socket)

'        ' Create the state object.
'        Dim state As New StateObject
'        state.workSocket = client

'        ' Begin receiving the data from the remote device.
'        client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, New AsyncCallback(AddressOf ReceiveCallback), state)
'    End Sub 'Receive


'    Private Shared Sub ReceiveCallback(ByVal ar As IAsyncResult)

'        ' Retrieve the state object and the client socket 
'        ' from the asynchronous state object.
'        Dim state As StateObject = CType(ar.AsyncState, StateObject)
'        Dim client As Socket = state.workSocket

'        ' Read data from the remote device.
'        Dim bytesRead As Integer = client.EndReceive(ar)

'        If bytesRead > 0 Then
'            ' There might be more data, so store the data received so far.
'            state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead))

'            ' Get the rest of the data.
'            client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, New AsyncCallback(AddressOf ReceiveCallback), state)
'        Else
'            ' All the data has arrived; put it in response.
'            If state.sb.Length > 1 Then
'                response = state.sb.ToString()
'            End If
'            ' Signal that all bytes have been received.
'            receiveDone.Set()
'        End If
'    End Sub 'ReceiveCallback


'    Private Shared Sub Send(ByVal client As Socket, ByVal data As String)
'        ' Convert the string data to byte data using ASCII encoding.
'        Dim byteData As Byte() = Encoding.ASCII.GetBytes(data)

'        ' Begin sending the data to the remote device.
'        client.BeginSend(byteData, 0, byteData.Length, 0, New AsyncCallback(AddressOf SendCallback), client)
'    End Sub 'Send


'    Private Shared Sub SendCallback(ByVal ar As IAsyncResult)
'        ' Retrieve the socket from the state object.
'        Dim client As Socket = CType(ar.AsyncState, Socket)

'        ' Complete sending the data to the remote device.
'        Dim bytesSent As Integer = client.EndSend(ar)
'        Console.WriteLine("Sent {0} bytes to server.", bytesSent)

'        ' Signal that all bytes have been sent.
'        sendDone.Set()
'    End Sub 'SendCallback
'End Class 'AsynchronousClient
