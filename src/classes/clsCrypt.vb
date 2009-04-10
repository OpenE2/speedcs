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

Imports System.Security.Cryptography
Imports System.Text
Imports System.IO

Public Class clsAESCrypt

    Public Function GetMD5Array(ByVal value As String) As Byte()
        Dim md5 As New MD5CryptoServiceProvider
        GetMD5Array = md5.ComputeHash(UTF8Encoding.UTF8.GetBytes(value))
        md5.Clear()
    End Function

    Public Function Decrypt(ByVal toDecrypt() As Byte, ByVal passMD5() As Byte) As Byte()
        Dim rDel As New RijndaelManaged()
        rDel.Key = passMD5
        rDel.Mode = CipherMode.ECB
        rDel.Padding = PaddingMode.Zeros
        Using cTransform As ICryptoTransform = rDel.CreateDecryptor()
            Dim resultArray As Byte() = cTransform.TransformFinalBlock(toDecrypt, 4, toDecrypt.Length - 4)
            rDel.Clear()
            Decrypt = resultArray
        End Using
    End Function

    Public Function Encrypt(ByVal toEncrypt() As Byte, ByVal passMD5() As Byte) As Byte()
        Dim rDel As New RijndaelManaged()
        rDel.Key = passMD5
        rDel.Mode = CipherMode.ECB
        rDel.Padding = PaddingMode.Zeros
        Using cTransform As ICryptoTransform = rDel.CreateEncryptor()
            Dim resultArray As Byte() = cTransform.TransformFinalBlock(toEncrypt, 0, toEncrypt.Length)
            rDel.Clear()
            Encrypt = resultArray
        End Using
    End Function



    'Hack: Some DES Stuff from MSDN and a good link: http://www.codeproject.com/KB/security/Crypto.aspx
    Private oKey As New DESCryptoServiceProvider

    Public Sub InitNCDKey(ByVal key() As Byte)
        oKey.Key = New Byte() {&H1, &H2, &H3, &H4, &H5, &H6, &H7, &H8, &H9, &H10, &H11, &H12, &H13, &H14}
    End Sub
    ' Encrypt the string.
    Public Function NewCamdEncrypt(ByVal PlainText As String) As Byte()
        ' Create a memory stream.
        Dim ms As New MemoryStream()

        ' Create a CryptoStream using the memory stream and the 
        ' CSP DES key.  
        Dim encStream As New CryptoStream(ms, oKey.CreateEncryptor(), CryptoStreamMode.Write)

        ' Create a StreamWriter to write a string
        ' to the stream.
        Dim sw As New StreamWriter(encStream)

        ' Write the plaintext to the stream.
        sw.WriteLine(PlainText)

        ' Close the StreamWriter and CryptoStream.
        sw.Close()
        encStream.Close()

        ' Get an array of bytes that represents
        ' the memory stream.
        Dim buffer As Byte() = ms.ToArray()

        ' Close the memory stream.
        ms.Close()

        ' Return the encrypted byte array.
        Return buffer
    End Function 'Encrypt


    ' Decrypt the byte array.
    Public Function NewCamdDecrypt(ByVal CypherText() As Byte) As String
        ' Create a memory stream to the passed buffer.
        Dim ms As New MemoryStream(CypherText)

        ' Create a CryptoStream using the memory stream and the 
        ' CSP DES key. 
        Dim encStream As New CryptoStream(ms, oKey.CreateDecryptor(), CryptoStreamMode.Read)

        ' Create a StreamReader for reading the stream.
        Dim sr As New StreamReader(encStream)

        ' Read the stream as a string.
        Dim val As String = sr.ReadLine()

        ' Close the streams.
        sr.Close()
        encStream.Close()
        ms.Close()

        Return val
    End Function 'Decrypt


End Class
