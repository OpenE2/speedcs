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

Imports System.Xml.Serialization
Imports System.IO

Public Class clsSettingsGlobal

#Region "Loading and Saving"

    Private filepath As String = Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), Application.ProductName)
    Private filename As String = Path.Combine(filepath, "config.xml")

    Public Function Load() As Boolean

        Dim serializer As New XmlSerializer(GetType(clsSettingsGlobal))
        System.IO.Directory.CreateDirectory(filepath)

        Dim fs As New FileStream(filename, FileMode.OpenOrCreate, FileAccess.ReadWrite)
        Try
            If fs.Length = 0 Then
                Output(IO.Path.GetFileName(filename) & " loaded with defaults.")
            Else
                CfgGlobals = CType(serializer.Deserialize(fs), clsSettingsGlobal)
                Output(IO.Path.GetFileName(filename) & " loaded.")
            End If
        Catch ex As Exception
            Output(ex.Message, LogDestination.eventlog, LogSeverity.fatal)
            Return False
        Finally
            fs.Close()
        End Try

    End Function

    Public Function Save() As Boolean

        Dim fs As New FileStream(filename, FileMode.Create)
        Try
            If Not IO.Directory.Exists(filepath) Then IO.Directory.CreateDirectory(filepath)
            Dim serializer As New XmlSerializer(GetType(clsSettingsGlobal))
            serializer.Serialize(fs, Me)
            fs.Close()
            Output(IO.Path.GetFileName(filename) & " saved.")
            Return True
        Catch ex As Exception
            Output(ex.Message, LogDestination.eventlog, LogSeverity.fatal)
            fs.Close()
            Return False
        End Try

    End Function

#End Region

#Region "WebInterface Settings"

    Public AdminPort As Integer = 8100
    Public AdminUsername As String = "admin"
    Public AdminPassword As String = ""

#End Region

#Region "Syslog Settings"

    Public SysLogEnabled As Boolean = False
    Public SysLogHostname As String = "127.0.0.1"
    Public SysLogPort As UInt16 = 514

#End Region

#Region "cs357x Settings"

    Public cs357xUse As Boolean = True
    Public cs357xPort As Integer = 20248

#End Region

#Region "NEWCAMD Settings"

    Public NewCamdUse As Boolean = True
    Public NewCamdPort As Integer = 20247
    Public NewCamdKey As String = "0102030405060708091011121314"

#End Region

End Class

<Serializable()> Public Class clsSettingsClients

#Region "Serialization"

#Region "Loading and Saving"

    Private filepath As String = Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), Application.ProductName)
    Private filename As String = Path.Combine(filepath, "clients.xml")

    Public Function Load() As Boolean

        'Wenn die Datei settings.xml noch nicht vorhanden ist,
        'wird versucht die Settings aus der Registry zu laden (Import der Altdaten)
        Dim serializer As New XmlSerializer(GetType(clsSettingsClients))
        System.IO.Directory.CreateDirectory(filepath)

        Dim fs As New FileStream(filename, FileMode.OpenOrCreate, FileAccess.ReadWrite)
        Try
            CfgClients = CType(serializer.Deserialize(fs), clsSettingsClients)
            Output(IO.Path.GetFileName(filename) & " loaded.")
        Catch ex As Exception
            Return False
        Finally
            fs.Close()
        End Try

    End Function

    Public Function Save() As Boolean

        'SaveClients
        Try
            If Not IO.Directory.Exists(filepath) Then IO.Directory.CreateDirectory(filepath)
            Dim serializer As New XmlSerializer(GetType(clsSettingsClients))
            Dim fs As New FileStream(filename, FileMode.Create)
            serializer.Serialize(fs, Me)
            fs.Close()
            Output(IO.Path.GetFileName(filename) & " saved.")
            Return True
        Catch ex As Exception
            Output("Settings.Save:" & ex.Message)
            Return False
        End Try
    End Function

#End Region

#End Region

#Region "Clients"


    Public Class clsClient
        Public active As Boolean = False
        Private _Username As String = ""
        Private _Password As String = ""
        Public Unique As String = ""
        Public AutoUpdate As String = ""
        Public SourceIp As String
        Public SourcePort As UInt16
        Public Notice As String
        Public street As String
        Public postid As String
        Public city As String
        Public phone As String
        Public mobile As String
        Public icq As String
        Public msn As String
        Public skype As String
        Public payeduntil As Date = Now
        Public lastrequest As Date = Now
        Public AUServer As String = String.Empty
        <XmlIgnore()> Public AUisActiveSince As Date
        <XmlIgnore()> Public AUSerial As UInt32

        Private _ucrc As UInteger
        Public ReadOnly Property ucrc() As UInteger
            Get
                Return _ucrc
            End Get
        End Property

        Private _MD5_Password() As Byte
        Public ReadOnly Property MD5_Password() As Byte()
            Get
                Return _MD5_Password
            End Get
        End Property

        Public Property Password() As String
            Get
                Return _Password
            End Get
            Set(ByVal value As String)
                _Password = value
                _MD5_Password = AESCrypt.GetMD5Array(value)
            End Set
        End Property

        Public Property Username() As String
            Get
                Return _Username
            End Get
            Set(ByVal value As String)
                _Username = value
                _ucrc = GetUserCRC(value)
            End Set
        End Property

        Private _email As String
        Public Property email() As String
            Get
                Return _email
            End Get
            Set(ByVal value As String)
                _email = value
            End Set
        End Property

        Private _AlternativeName As String
        Public Property AlternativeName() As String
            Get
                Return _AlternativeName
            End Get
            Set(ByVal value As String)
                _AlternativeName = value
            End Set
        End Property

    End Class

    Public Class clsClients
        Inherits CollectionBase
        Public GetPassByUCRC As New Hashtable

        Default Public Property Item(ByVal index As Integer) As clsClient
            Get
                Return CType(List(index), clsClient)
            End Get
            Set(ByVal Value As clsClient)
                List(index) = Value
            End Set
        End Property

        Public Sub Add(ByVal column As clsClient)

            Dim found As Boolean = False
            For Each r As clsClient In List
                If r.Username = column.Username Then
                    found = True
                    Exit For
                End If
            Next
            If found = False Then
                List.Add(column)
                GetPassByUCRC.Add(column.ucrc, column.Password)
            End If

        End Sub

        Public Function IndexOf(ByVal value As clsClient) As Integer
            Return List.IndexOf(value)
        End Function

        Public Sub Insert(ByVal index As Integer, ByVal value As clsClient)
            List.Insert(index, value)
        End Sub

        Public Sub Remove(ByVal value As clsClient)
            List.Remove(value)
            GetPassByUCRC.Remove(value.ucrc)
        End Sub

        Public Function Contains(ByVal value As clsClient) As Boolean
            Return List.Contains(value)
        End Function

        Public Function FindByUCRC(ByVal ucrc As UInteger) As clsClient

            For Each c As clsClient In List
                If c.ucrc.Equals(ucrc) Then
                    If c.active Then Return c
                End If
            Next
            Return Nothing

        End Function

    End Class

    Private _Clients As New clsClients
    Public Property Clients() As clsClients
        Get
            Return _Clients
        End Get
        Set(ByVal value As clsClients)
            _Clients = value
        End Set
    End Property

#End Region

End Class

<Serializable()> Public Class clsSettingsCardServers

#Region "Serialization"

#Region "Loading and Saving"

    Private filepath As String = Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), Application.ProductName)
    Private filename As String = Path.Combine(filepath, "servers.xml")

    Public Function Load() As Boolean

        Dim serializer As New XmlSerializer(GetType(clsSettingsCardServers))
        System.IO.Directory.CreateDirectory(filepath)

        Dim fs As New FileStream(filename, FileMode.OpenOrCreate, FileAccess.ReadWrite)
        Try
            CfgCardServers = CType(serializer.Deserialize(fs), clsSettingsCardServers)
            Output(IO.Path.GetFileName(filename) & " loaded.")
        Catch ex As Exception
            Return False
        Finally
            fs.Close()
        End Try

    End Function

    Public Function Save() As Boolean

        'SaveServers
        Try
            If Not IO.Directory.Exists(filepath) Then IO.Directory.CreateDirectory(filepath)
            Dim serializer As New XmlSerializer(GetType(clsSettingsCardServers))
            Dim fs As New FileStream(filename, FileMode.Create)
            serializer.Serialize(fs, Me)
            fs.Close()
            Output(IO.Path.GetFileName(filename) & " saved.")
            Return True
        Catch ex As Exception
            Output("Settings.Save:" & ex.Message)
            Return False
        End Try
    End Function

#End Region

#End Region

#Region "CardServer"
    Public Class clsCardServer
        Public Active As Boolean = False
        Public Hostname As String = "localhost"
        Public Port As Integer = 20248
        Public SendBroadcasts As Boolean = False
        Public SendEMMs As Boolean = False
        Public SendECMs As Boolean = True
        'ToDo: Build IsSCS in Webif
        Public IsSCS As Boolean = False
        Public acceptedCAID As List(Of UInt16)
        <XmlIgnore()> Public deniedSRVIDCAID As New List(Of UInt32)

        Private _Password As String = String.Empty
        Private _Username As String = String.Empty
        Private _UCRC As UInteger = 0
        Private _MD5_Password() As Byte
        Public ReadOnly Property MD5_Password() As Byte()
            Get
                Return _MD5_Password
            End Get
        End Property

        Public Property Password() As String
            Get
                Return _Password
            End Get
            Set(ByVal value As String)
                _Password = value
                _MD5_Password = AESCrypt.GetMD5Array(value)
            End Set
        End Property
        Public Property Username() As String
            Get
                Return _Username
            End Get
            Set(ByVal value As String)
                _Username = value
                _UCRC = GetUserCRC(_Username)
            End Set
        End Property

        Public ReadOnly Property UCRC() As UInteger
            Get
                Return _UCRC
            End Get
        End Property

        Public ReadOnly Property IP() As String
            Get
                Try
                    Return Net.Dns.GetHostEntry(Hostname).AddressList(0).ToString
                Catch ex As Exception
                    Return ""
                End Try
            End Get
        End Property

    End Class

    Public Class clsCardServers
        Inherits CollectionBase
        Default Public Property Item(ByVal index As Integer) As clsCardServer
            Get
                Return CType(List(index), clsCardServer)
            End Get
            Set(ByVal Value As clsCardServer)
                List(index) = Value
            End Set
        End Property

        Public Sub Add(ByVal column As clsCardServer)
            List.Add(column)
        End Sub

        Public Function IndexOf(ByVal value As clsCardServer) As Integer
            Return List.IndexOf(value)
        End Function

        Public Sub Insert(ByVal index As Integer, ByVal value As clsCardServer)
            List.Insert(index, value)
        End Sub

        Public Sub Remove(ByVal value As clsCardServer)
            List.Remove(value)
        End Sub

        Public Function Contains(ByVal value As clsCardServer) As Boolean
            Return List.Contains(value)
        End Function

    End Class

    Private _CardServers As New clsCardServers
    Public Property CardServers() As clsCardServers
        Get
            Return _CardServers
        End Get
        Set(ByVal value As clsCardServers)
            _CardServers = value
        End Set
    End Property
#End Region

End Class

<Serializable()> Public Class clsSettingsCardReaders

#Region "Serialization"

#Region "Loading and Saving"

    Private filepath As String = Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), Application.ProductName)
    Private filename As String = Path.Combine(filepath, "readers.xml")

    Public Function Load() As Boolean

        Dim serializer As New XmlSerializer(GetType(clsSettingsCardReaders))
        System.IO.Directory.CreateDirectory(filepath)

        Dim fs As New FileStream(filename, FileMode.OpenOrCreate, FileAccess.ReadWrite)
        Try
            CfgCardReaders = CType(serializer.Deserialize(fs), clsSettingsCardReaders)
            Output(IO.Path.GetFileName(filename) & " loaded.")
        Catch ex As Exception
            Return False
        Finally
            fs.Close()
        End Try

    End Function

    Public Function Save() As Boolean

        'SaveReaders
        Try
            If Not IO.Directory.Exists(filepath) Then IO.Directory.CreateDirectory(filepath)
            Dim serializer As New XmlSerializer(GetType(clsSettingsCardReaders))
            Dim fs As New FileStream(filename, FileMode.Create)
            serializer.Serialize(fs, Me)
            fs.Close()
            Output(IO.Path.GetFileName(filename) & " saved.")
            Return True
        Catch ex As Exception
            Output("Settings.Save:" & ex.Message)
            Return False
        End Try
    End Function

#End Region

#End Region

#Region "Readers"

    Public Class clsCardReader

        Public Active As Boolean = False
        Public UniqueName As String = String.Empty
        Public PortName As String = String.Empty
        Public Baudrate As UInt16 = 4800
        Public Parity As Ports.Parity = Ports.Parity.None
        Public DataBits As UInt16 = 8
        Public StopBits As UInt16 = 1
        Public TimeOut As UInt16 = 2000

    End Class

    Public Class clsCardReaders
        Inherits CollectionBase
        Default Public Property Item(ByVal index As Integer) As clsCardReader
            Get
                Return CType(List(index), clsCardReader)
            End Get
            Set(ByVal Value As clsCardReader)
                List(index) = Value
            End Set
        End Property

        Public Sub Add(ByVal column As clsCardReader)
            Dim found As Boolean = False
            For Each r As clsCardReader In List
                If r.UniqueName = column.UniqueName Then
                    found = True
                    Exit For
                End If
            Next
            If found = False Then List.Add(column)
        End Sub

        Public Function IndexOf(ByVal value As clsCardReader) As Integer
            Return List.IndexOf(value)
        End Function

        Public Sub Insert(ByVal index As Integer, ByVal value As clsCardReader)
            List.Insert(index, value)
        End Sub

        Public Sub Remove(ByVal value As clsCardReader)
            List.Remove(value)
        End Sub

        Public Function Contains(ByVal value As clsCardReader) As Boolean
            Return List.Contains(value)
        End Function

    End Class

    Private _CardReaders As New clsCardReaders
    Public Property CardReaders() As clsCardReaders
        Get
            Return _CardReaders
        End Get
        Set(ByVal value As clsCardReaders)
            _CardReaders = value
        End Set
    End Property

#End Region

End Class

