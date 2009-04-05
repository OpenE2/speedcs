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

Imports System.IO
Imports System.IO.Ports

Module Readers

    Public Readers As New clsReaders

    Public Class clsReader

        Private WithEvents SP As New Ports.SerialPort()
        Public SettingsObject As clsSettingsCardReaders.clsCardReader
        Private t As Threading.Thread
        Private stopReader As Boolean = False

#Region "Open Close Port"

        Public Sub StartSP()

            Try
                StopSP()
                'pClosePort = False

                If SP Is Nothing Then
                    'GPSPort = New Ports.SerialPort(AppSettings.GPSPort, AppSettings.GPSBaudrate, Ports.Parity.None, 8, Ports.StopBits.One)
                End If

                With SP
                    .PortName = SettingsObject.PortName
                    .BaudRate = SettingsObject.Baudrate
                    .ReadTimeout = SettingsObject.TimeOut
                    .WriteTimeout = SettingsObject.TimeOut
                    '.BaudRate = 16000
                    '.DataBits = 8
                    .StopBits = StopBits.Two
                    .DtrEnable = True
                    .RtsEnable = False
                    'Parity = Parity.Even
                    '.Handshake = Handshake.RequestToSend
                    '.Handshake = sett
                    .Open()
                    'Data Terminal Ready setzten, damit die Karte mit dem ATR antwortet
                    '.DataBits = 7

                    '.DiscardInBuffer()
                    '.RtsEnable = True
                    Output("Reader " & SettingsObject.UniqueName & " started.")
                    Output("Receive Bytes Threshold:" & .ReceivedBytesThreshold)
                    t = New Threading.Thread(AddressOf Poll)
                    t.IsBackground = True
                    t.Priority = Threading.ThreadPriority.Lowest
                    t.Start()
                End With

                Dim InitCmd() As Byte
                Using ms As New MemoryStream
                    ms.Write(BitConverter.GetBytes(1), 0, 1)
                    ms.Write(BitConverter.GetBytes(2), 0, 1)
                    ms.Write(BitConverter.GetBytes(0), 0, 1)
                    ms.Write(BitConverter.GetBytes(3), 0, 1)
                    ms.Write(BitConverter.GetBytes(0), 0, 1)
                    ms.Write(BitConverter.GetBytes(0), 0, 1)
                    ms.Write(BitConverter.GetBytes(15), 0, 1)
                    InitCmd = ms.ToArray
                    ms.Close()
                End Using
                SP.ReadExisting()
                'SP.Write("ksdfj")
                'SP.Write(InitCmd, 0, InitCmd.Length)
            Catch ex As Exception
                Output("StartReader() " & ex.Message & ex.StackTrace, LogDestination.file, LogSeverity.fatal, ConsoleColor.Red)
            End Try

        End Sub

        Public Sub StopSP()
            If Not SP Is Nothing Then
                If SP.IsOpen Then
                    stopReader = True
                    Debug.WriteLine("Stop()")
                    't.Abort()
                    Try
                        Debug.WriteLine("Try To Close")
                        SP.DiscardInBuffer()
                        SP.Close()
                        Debug.WriteLine("closed")
                    Catch ex As Exception
                        Debug.WriteLine(ex.Message)
                    End Try
                    'Debug.WriteLine("End Stopping")
                    'pPositionFixed = False
                End If
            End If

        End Sub

        Public Sub Poll()

            While stopReader
                Threading.Thread.Sleep(10)

            End While
        End Sub
#End Region

        Private Sub SP_DataReceived(ByVal sender As Object, ByVal e As System.IO.Ports.SerialDataReceivedEventArgs) Handles SP.DataReceived
            'Output("DataReceived:" & e.EventType.ToString)
            Dim p As SerialPort = DirectCast(sender, SerialPort)
            Dim ReceivedBytes() As Byte = Nothing

            p.Read(ReceivedBytes, 0, p.BytesToRead)

            DebugOutputBytes(ReceivedBytes)
        End Sub

        Private Sub SP_Disposed(ByVal sender As Object, ByVal e As System.EventArgs) Handles SP.Disposed
            Output("Disposed:")
        End Sub

        Private Sub SP_ErrorReceived(ByVal sender As Object, ByVal e As System.IO.Ports.SerialErrorReceivedEventArgs) Handles SP.ErrorReceived
            Output("ErrorReceived:" & e.EventType.ToString)
        End Sub

        Private Sub SP_PinChanged(ByVal sender As Object, ByVal e As System.IO.Ports.SerialPinChangedEventArgs) Handles SP.PinChanged
            Dim p As SerialPort = DirectCast(sender, SerialPort)

            Output("PinChanged:" & e.EventType.ToString & " " & p.BreakState.ToString)
            Output(p.BreakState.ToString)
            Output(p.CDHolding.ToString)
            Output(p.CtsHolding.ToString)
            Output(p.DtrEnable.ToString)
            Output(p.RtsEnable.ToString)

        End Sub
    End Class

    Public Class clsReaders
        Inherits CollectionBase

        Default Public Property Item(ByVal index As Integer) As clsReader
            Get
                Return CType(List(index), clsReader)
            End Get
            Set(ByVal Value As clsReader)
                List(index) = Value
            End Set
        End Property

        Public Sub Add(ByVal column As clsReader)
            List.Add(column)
        End Sub

        Public Function IndexOf(ByVal value As clsReader) As Integer
            Return List.IndexOf(value)
        End Function

        Public Sub Insert(ByVal index As Integer, ByVal value As clsReader)
            List.Insert(index, value)
        End Sub

        Public Sub Remove(ByVal value As clsReader)
            List.Remove(value)
        End Sub

        Public Function Contains(ByVal value As clsReader) As Boolean
            Return List.Contains(value)
        End Function

    End Class

    Public Sub StartReaders()

        Try

            For Each r As clsSettingsCardReaders.clsCardReader In CfgCardReaders.CardReaders
                If r.Active Then
                    Dim Reader As New clsReader()
                    Reader.SettingsObject = r
                    Reader.StartSP()
                    Readers.Add(Reader)
                End If
            Next

        Catch ex As Exception
            Output("StartReaders() " & ex.Message & ex.StackTrace, LogDestination.file, LogSeverity.fatal, ConsoleColor.Red)
        End Try

    End Sub

    Public Sub StopReaders()

        Try
            For Each r As clsReader In Readers
                r.StopSP()
                r = Nothing
            Next
            Readers.Clear()
        Catch ex As Exception
            Output("StopReaders() " & ex.Message & ex.StackTrace, LogDestination.file, LogSeverity.fatal, ConsoleColor.Red)
        End Try

    End Sub
End Module