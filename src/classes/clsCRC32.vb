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

Imports System.Text
Imports System.Security.Cryptography

Public Class clsCRC32
    Inherits HashAlgorithm
    Public Shared Function CRC32OfString(ByVal s As String) As String
        Dim original As Byte()
        Dim encoded As Byte()

        Using crc32 As New clsCRC32()
            original = Encoding.ASCII.GetBytes(s)
            encoded = crc32.ComputeHash(original)
        End Using

        Dim sbEncoded As New StringBuilder()
        For i As Integer = 0 To encoded.Length - 1
            sbEncoded.Append(encoded(i).ToString("x2"))
        Next

        Return sbEncoded.ToString()
    End Function
    'Public Shared Function CRC32OfString(ByVal original As Byte()) As String
    Public Shared Function CRC32OfString(ByVal original As Byte()) As Byte()
        'Dim original As Byte() 
        Dim encoded As Byte()

        Using crc32 As New clsCRC32()
            'original = Encoding.ASCII.GetBytes(s) 
            encoded = crc32.ComputeHash(original)
        End Using

        'Dim sbEncoded As New StringBuilder()
        'For i As Integer = 0 To encoded.Length - 1
        'sbEncoded.Append(encoded(i).ToString("x2"))
        'Next
        Return encoded
        'Return sbEncoded.ToString()
    End Function

#Region "Konstanten"
    Public Const DefaultPolynomial As UInt32 = 3988292384
    Public Const DefaultSeed As UInt32 = 4294967295
#End Region

#Region "Private Variablen"
    Private _hash As UInt32
    Private _seed As UInt32
    Private _table As UInt32()
#End Region

#Region "Kontruktoren"
    Public Sub New()
        _table = InitializeTable(DefaultPolynomial)
        _seed = DefaultSeed
        Initialize()
    End Sub

    Public Sub New(ByVal polynomial As UInt32, ByVal seed As UInt32)
        _table = InitializeTable(polynomial)
        Me._seed = seed
        Initialize()
    End Sub
#End Region

#Region "Überschreibungen"
    Public Overloads Overrides Sub Initialize()
        _hash = _seed
    End Sub

    Protected Overloads Overrides Sub HashCore(ByVal buffer As Byte(), ByVal start As Integer, ByVal length As Integer)
        _hash = CalculateHash(_table, _hash, buffer, start, length)
    End Sub

    Protected Overloads Overrides Function HashFinal() As Byte()
        Dim hashBuffer As Byte() = UInt32ToBigEndianBytes(_hash)
        Me.HashValue = hashBuffer

        Return hashBuffer
    End Function

    Public Overloads Overrides ReadOnly Property HashSize() As Integer
        Get
            Return 32
        End Get
    End Property
#End Region

#Region "Implementierung"
    Public Shared Function Compute(ByVal polynomial As UInt32, ByVal seed As UInt32, ByVal buffer As Byte()) As UInt32
        Return CalculateHash(InitializeTable(polynomial), seed, buffer, 0, buffer.Length)
    End Function

    Private Shared Function InitializeTable(ByVal polynomial As UInt32) As UInt32()
        Dim createTable As UInt32() = New UInt32(255) {}
        For i As UInt32 = 0 To 255

            Dim entry As UInt32 = i
            For j As Integer = 0 To 7
                If (entry And 1) = 1 Then
                    entry = (entry >> 1) Xor polynomial
                Else
                    entry = entry >> 1
                End If
            Next

            createTable(CInt(i)) = entry
        Next

        Return createTable
    End Function

    Private Shared Function CalculateHash(ByVal table As UInt32(), ByVal seed As UInt32, ByVal buffer As Byte(), ByVal start As Integer, ByVal size As Integer) As UInt32

        Dim crc As UInt32 = seed
        For i As Integer = start To size - 1
            crc = (crc >> 8) Xor table(CInt(buffer(i) Xor crc And 255))
        Next

        Return Not crc
    End Function

    Private Function UInt32ToBigEndianBytes(ByVal x As UInt32) As Byte()
        Return New Byte() {CByte(((x >> 24) And 255)), CByte(((x >> 16) And 255)), CByte(((x >> 8) And 255)), CByte((x And 255))}

    End Function
#End Region
End Class

Module moduleCRC32

    Public crc32 As New clsCRC32
    Public Function GetUserCRC(ByVal username As String) As UInt32
        Dim md5 As New MD5CryptoServiceProvider
        'Dim ba_Username As Byte() = Encoding.ASCII.GetBytes(username)
        'Dim md5_Username As Byte() = md5.ComputeHash(ba_Username)
        'Hack: UTF8 muß hier glaube ich
        Dim md5_Username As Byte() = md5.ComputeHash(UTF8Encoding.UTF8.GetBytes(username))
        md5.Clear()
        'Dim md5_Username As Byte() = md5.ComputeHash(Encoding.Default.GetBytes(username))
        Dim ucrc As Byte() = crc32.ComputeHash(md5_Username)
        Array.Reverse(ucrc, 0, 4) 'Big Endian Konvertierung
        Return BitConverter.ToUInt32(ucrc, 0)
    End Function

    'TODO GetUserCRC_ wird nicht benutzt, evtl. entfernen
    Public Function GetUserCRC_(ByVal username() As Byte) As UInteger
        Dim md5 As New MD5CryptoServiceProvider
        Dim md5_Username As Byte() = md5.ComputeHash(username)
        Dim ucrc As Byte() = crc32.ComputeHash(md5_Username)
        md5.Clear()
        Return BitConverter.ToUInt32(ucrc, 0)
    End Function

End Module
