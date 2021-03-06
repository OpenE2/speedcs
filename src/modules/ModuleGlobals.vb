﻿' 
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

Imports System.Reflection
Imports Microsoft.Win32
Imports System.IO

Namespace types

    Public Enum CMDType
        ECMRequest = &H0
        ECMResponse = &H1
        CascadingRequest = &H3
        EMMRequest = &H5
        EMMResponse = &H6
        sCSRequest = &H55
        NotFound = &H44
        BroadCastResponse = &H66
        CRCError = &H99
        unknown = &HFF
    End Enum

End Namespace

Module ModuleGlobals



#Region "Detect Operating System"

    Public Enum eOSType
        Windows
        Mono
        Wine
        Linux
        unknown
    End Enum

    Public OSTypeIs As eOSType

    Public Function OStype() As eOSType
        Return OSTypeIs
    End Function

    Public Function CheckOStype() As eOSType

        Try
            Dim t As Type = Type.[GetType]("Mono.Runtime")
            If t IsNot Nothing Then
                Return eOSType.Linux
            Else
                Try
                    Dim r As RegistryKey = Registry.CurrentUser.OpenSubKey("Software\Wine\", False)
                    If r Is Nothing Then
                        Return eOSType.Windows
                    Else
                        Return eOSType.Linux
                    End If
                Catch ex As Exception
                    Return eOSType.Windows
                End Try
            End If
        Catch ex As Exception
            Return eOSType.unknown
        End Try

    End Function

#End Region

#Region "Application Object"

    Public Application As New clsApplication
    Public Class clsApplication
        Public ReadOnly Property ProductName() As String
            Get
                Return Assembly.GetExecutingAssembly.GetName.Name.ToString
            End Get
        End Property

        Public ReadOnly Property ProductVersion() As String
            Get
                Return Assembly.GetExecutingAssembly.GetName.Version.ToString
            End Get
        End Property

        Public ReadOnly Property Major() As Integer

            Get
                Try
                    Return CInt(Assembly.GetExecutingAssembly.GetName.Version.ToString.Split(CChar("."))(0))
                Catch ex As Exception
                    Return 0
                End Try
            End Get
        End Property

        Public ReadOnly Property Minor() As Integer
            Get
                Return CInt(Assembly.GetExecutingAssembly.GetName.Version.ToString.Split(CChar("."))(1))
            End Get
        End Property

        Public ReadOnly Property Revision() As Integer
            Get
                Return CInt(Assembly.GetExecutingAssembly.GetName.Version.ToString.Split(CChar("."))(2))
            End Get
        End Property

        Public ReadOnly Property Build() As Integer
            Get
                Return CInt(Assembly.GetExecutingAssembly.GetName.Version.ToString.Split(CChar("."))(3))
            End Get
        End Property
    End Class

#End Region

    Private filepath As String = Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), Application.ProductName)

    Public Sub DebugOutputBytes(ByVal b As Byte(), Optional ByVal prefix As String = "")
        Dim out As String = String.Empty
        For i As Integer = 0 To b.Length - 1
            out &= b(i).ToString("X2") & " "
        Next
        Debug.WriteLine(prefix & out)
    End Sub

    Public Sub WriteEMMToFile(ByVal b As Byte(), Optional ByVal prefix As String = "")
        If Not filepath.EndsWith(InstanceDir) Then
            filepath = Path.Combine(filepath, InstanceDir)
        End If
        Dim filename As String = Path.Combine(filepath, "EMMlog.txt")
        Dim out As String = String.Empty
        For i As Integer = 0 To b.Length - 1
            out &= b(i).ToString("X2") & " "
        Next
        Using fw As New StreamWriter(filename, True)
            fw.WriteLine(Date.Now.ToString & " " & prefix & out)
        End Using
    End Sub

    Public Sub WriteECMToFile(ByVal b As Byte(), Optional ByVal prefix As String = "")
        If Not filepath.EndsWith(InstanceDir) Then
            filepath = Path.Combine(filepath, InstanceDir)
        End If
        Dim filename As String = Path.Combine(filepath, "ECMlog.txt")
        Dim out As String = String.Empty
        For i As Integer = 0 To b.Length - 1
            out &= b(i).ToString("X2") & " "
        Next
        Using fw As New StreamWriter(filename, True)
            fw.WriteLine(Date.Now.ToString & " " & prefix & out)
        End Using
    End Sub

    Public Function HEX2DEC(ByVal HEX As String) As String
        Dim position As Integer = HEX.IndexOf("%", 0)
        While Not CBool(position = -1)
            Dim strHEX As String = HEX.Substring(position, 3)
            Dim strConvert As String = strHEX.Replace("%", "")
            Dim strDEC As Long = Convert.ToInt64(strConvert, 16)
            If strDEC > 126 Then
                HEX = HEX.Replace(strHEX, "")
                position = HEX.IndexOf("%", position)
            Else
                HEX = HEX.Replace(strHEX, "&#" & strDEC & ";")
                position = HEX.IndexOf("%", position)
            End If
        End While
        Return HEX
    End Function

    Public Function GetLittleEndian(ByVal value As UInt16) As UInt16
        Return CUShort(Math.Floor(value / 256) + 256 * (value And 255)) 'Convert to Little Endian
    End Function

    Public Function GetLittleEndian(ByVal value As UInt32) As UInt32
        Return CUInt(Math.Floor(value / 65536) + 65536 * (value And 65535)) 'Convert to Little Endian
    End Function

    Public CfgGlobals As New clsSettingsGlobal
    Public CfgClients As New clsSettingsClients
    Public CfgCardServers As New clsSettingsCardServers
    Public CfgCardReaders As New clsSettingsCardReaders

    Public UdpClientManager As clsUDPIO
    Public UdpServerManager As clsUDPIO

    Public UdpSysLog As New clsUDPIO(0, Net.IPAddress.Any)

    Public Webserver As New clsWebserver

    Public Cache As New clsCache
    Public WithEvents CacheManager As New clsCMDManager



    Public AESCrypt As New clsAESCrypt

    Public Services As New clsServicesList

    Public emmStack As New SortedList(Of UInt32, Byte())

    Public NewCamdServer As New clsTCPIO

    Public CWlog As New clsCWlog

End Module
