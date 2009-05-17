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

Public Class clsServerSettings
    Private _localUCRC As UInt32

    Public Structure speedCSMessage
        Dim senderUCRC As UInt32
        Dim isSpeedCS As Boolean
        Dim acceptBroadcast As Boolean
        Dim acceptECM As Boolean
        Dim acceptEmm As Boolean
        Dim acceptCAID As List(Of UInt16)       'Little Endian!
        Dim deniedSRVIDCAID As List(Of UInt32)  'Little Endian!
    End Structure

    Public Enum CMD
        MSGHandshake = &H61
        MSGSrvidDenied = &H62
    End Enum

    Public sCSMSG As speedCSMessage

    Public Sub New(ByVal localUCRC As UInt32)
        _localUCRC = localUCRC
        sCSMSG = New speedCSMessage
        sCSMSG.acceptCAID = New List(Of UInt16)
        sCSMSG.deniedSRVIDCAID = New List(Of UInt32)
    End Sub

    Public Sub AddAcceptedCAID(ByVal caid As UInt16)
        If Not sCSMSG.acceptCAID.Contains(caid) Then sCSMSG.acceptCAID.Add(caid)
    End Sub

    Public Sub RemoveAcceptedCAID(ByVal caid As UInt16)
        If sCSMSG.acceptCAID.Contains(caid) Then sCSMSG.acceptCAID.Remove(caid)
    End Sub

    Public Sub ClearAcceptedCAID()
        sCSMSG.acceptCAID.Clear()
    End Sub

    Public Sub AddDeniedSRVIDCAID(ByVal srvidcaid As UInt32)
        If Not sCSMSG.deniedSRVIDCAID.Contains(srvidcaid) Then sCSMSG.deniedSRVIDCAID.Add(srvidcaid)
    End Sub

    Public Sub RemoveDeniedSRVIDCAID(ByVal srvidcaid As UInt32)
        If sCSMSG.deniedSRVIDCAID.Contains(srvidcaid) Then sCSMSG.deniedSRVIDCAID.Remove(srvidcaid)
    End Sub

    Public Sub ClearDeniedSRVIDCAID()
        sCSMSG.deniedSRVIDCAID.Clear()
    End Sub

    Public Sub ReadMessage(ByVal plainMessage() As Byte)

        'Config Block CRC check
        Dim realCRC() As Byte
        Using ms As New MemoryStream
            ms.Write(plainMessage, 12, plainMessage(1))
            realCRC = clsCRC32.CRC32OfByte(ms.ToArray)
        End Using

        Dim paketCRC() As Byte
        Using ms As New MemoryStream
            ms.Write(plainMessage, 8, 4)
            paketCRC = ms.ToArray
        End Using

        If Equals(paketCRC.ToString, realCRC.ToString) Then 'why the hell Equals fails on Bytearrays

            sCSMSG.senderUCRC = BitConverter.ToUInt32(plainMessage, 4)

            Dim configBlockLen As Byte = plainMessage(1)
            If plainMessage(12) = &H1 Then sCSMSG.isSpeedCS = True Else sCSMSG.isSpeedCS = False
            If plainMessage(13) = &H1 Then sCSMSG.acceptBroadcast = True Else sCSMSG.acceptBroadcast = False
            If plainMessage(14) = &H1 Then sCSMSG.acceptECM = True Else sCSMSG.acceptECM = False
            If plainMessage(15) = &H1 Then sCSMSG.acceptEmm = True Else sCSMSG.acceptEmm = False

            If plainMessage(0) = CMD.MSGHandshake Then       'Shakehand Message

                sCSMSG.acceptCAID.Clear()
                Dim caid As UInt16

                For idx As Byte = 17 To CByte(17 + (plainMessage(16) * 2) - 2) Step 2
                    caid = BitConverter.ToUInt16(plainMessage, idx)
                    If Not sCSMSG.acceptCAID.Contains(caid) Then sCSMSG.acceptCAID.Add(caid)
                Next

            ElseIf plainMessage(0) = CMD.MSGSrvidDenied Then   'Reject srvidcaid Message

                Dim srvidcaid As UInt32 = BitConverter.ToUInt32(plainMessage, 17)
                If Not sCSMSG.deniedSRVIDCAID.Contains(srvidcaid) Then sCSMSG.deniedSRVIDCAID.Add(srvidcaid)

            End If

        Else
            Debug.WriteLine("Wrong Config Block CRC")
        End If
    End Sub

    Public Function WriteHandshakeMessage() As Byte()

        Dim byteConfigBlock() As Byte
        Using ms As New MemoryStream
            If sCSMSG.isSpeedCS Then ms.WriteByte(&H1) Else ms.WriteByte(&H0)
            If sCSMSG.acceptBroadcast Then ms.WriteByte(&H1) Else ms.WriteByte(&H0)
            If sCSMSG.acceptECM Then ms.WriteByte(&H1) Else ms.WriteByte(&H0)
            If sCSMSG.acceptEmm Then ms.WriteByte(&H1) Else ms.WriteByte(&H0)
            ms.WriteByte(CByte(sCSMSG.acceptCAID.Count))
            For Each caid As UInt16 In sCSMSG.acceptCAID
                ms.Write(BitConverter.GetBytes(caid), 0, 2)
            Next
            byteConfigBlock = ms.ToArray
        End Using

        Using ms As New MemoryStream
            ms.WriteByte(&H61)
            ms.WriteByte(CByte(5 + (sCSMSG.acceptCAID.Count * 2)))
            ms.WriteByte(&H0)   'byte 2+3 TBD
            ms.WriteByte(&H0)
            ms.Write(BitConverter.GetBytes(_localUCRC), 0, 4)
            ms.Write(clsCRC32.CRC32OfByte(byteConfigBlock), 0, 4)
            ms.Write(byteConfigBlock, 0, byteConfigBlock.Length)
            WriteHandshakeMessage = ms.ToArray
        End Using

    End Function

    Public Function WriteSrvidDeniedMessage(ByVal srvidcaid As UInt32) As Byte()

        Dim byteConfigBlock() As Byte
        Using ms As New MemoryStream
            If sCSMSG.isSpeedCS Then ms.WriteByte(&H1) Else ms.WriteByte(&H0)
            If sCSMSG.acceptBroadcast Then ms.WriteByte(&H1) Else ms.WriteByte(&H0)
            If sCSMSG.acceptECM Then ms.WriteByte(&H1) Else ms.WriteByte(&H0)
            If sCSMSG.acceptEmm Then ms.WriteByte(&H1) Else ms.WriteByte(&H0)
            ms.WriteByte(&H1)
            ms.Write(BitConverter.GetBytes(srvidcaid), 0, 4)
            byteConfigBlock = ms.ToArray
        End Using

        Using ms As New MemoryStream
            ms.WriteByte(&H62)
            ms.WriteByte(9)     'Config + 1 * SrvidCaid
            ms.WriteByte(&H0)   'byte 2+3 TBD
            ms.WriteByte(&H0)
            ms.Write(BitConverter.GetBytes(_localUCRC), 0, 4)
            ms.Write(clsCRC32.CRC32OfByte(byteConfigBlock), 0, 4)
            ms.Write(byteConfigBlock, 0, byteConfigBlock.Length)
            WriteSrvidDeniedMessage = ms.ToArray
        End Using

    End Function

End Class
