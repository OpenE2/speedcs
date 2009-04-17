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
Imports System.Text.RegularExpressions

Public Class clsService
    Public ID As String
    Public Provider As String
    Public Name As String
    Public Type As String
    Public Comment As String
    Public Sub New(ByVal value As String)
        If Not value Is Nothing Then
            Dim arr() As String = value.Split(CChar("|"))
            Me.Provider = arr(0)
            Me.Name = arr(1)
            Me.Type = arr(2)
            Me.Comment = arr(3)
        Else
            Me.Provider = "unknown"
            Me.Name = "unknown"
            Me.Type = "unknown"
            Me.Comment = "unknown"
        End If
    End Sub
End Class

Public Class clsServices
    Public serviceIDList As SortedList

    Public Sub Load()

        Try
            Dim filepath As String = Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), Application.ProductName)
            Dim filename As String = Path.Combine(filepath, "speedcs.srvid")

            serviceIDList = New SortedList

            If IO.File.Exists(filename) Then
                Dim lineidx As Integer = 0
                Using sr As New StreamReader(filename)
                    Dim r As New Regex("(.*[0-9A-Fa-f]{4}\:[0-9A-Fa-f]{4})(\|)(.*)")
                    Dim m As Match
                    While Not sr.EndOfStream
                        Dim serviceIDLine As String = sr.ReadLine
                        lineidx += 1
                        m = r.Match(serviceIDLine.Trim)
                        Dim CaidSrvId() As String = Split(m.Groups(1).Value, ":")
                        If CaidSrvId.Length = 2 Then
                            Dim Caids() As String = Split(CaidSrvId(0), ",")
                            For Each Caid As String In Caids
                                Dim key As String = Caid & ":" & CaidSrvId(1)
                                If Not serviceIDList.ContainsKey(key.ToUpper) Then
                                    serviceIDList.Add(key.ToUpper, m.Groups(3).Value.ToUpper)
                                End If
                            Next
                        Else
                            Output("Error in Line #" & lineidx, LogColor:=ConsoleColor.Yellow)
                        End If
                    End While
                End Using
                Output(serviceIDList.Count & " Services loaded.")
            Else
                Output("Service file SpeedCS.srvid not found.", LogDestination.none, LogSeverity.warning, ConsoleColor.Yellow)
            End If
        Catch ex As Exception
            Output("Services.Load()" & ex.Message & ex.StackTrace, LogColor:=ConsoleColor.Red)
        End Try

    End Sub

    Public Function GetServiceInfo(ByVal key As String) As clsService

        GetServiceInfo = New clsService(TryCast(serviceIDList(key), String))
        GetServiceInfo.ID = key

    End Function
End Class
