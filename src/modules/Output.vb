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

Imports System.Environment
Imports System.IO

Module OutputModule

    Public Enum LogDestination
        eventlog
        file
        none
    End Enum

    Public Enum LogSeverity
        debug
        info
        warning
        fatal
    End Enum

    Public Function Output(ByVal msg As String, _
                           Optional ByVal dst As LogDestination = LogDestination.none, _
                           Optional ByVal severity As LogSeverity = LogSeverity.info, _
                           Optional ByVal LogColor As ConsoleColor = ConsoleColor.Gray) As Boolean

        msg = Date.Now.ToString & "  " & msg

        If Environment.UserInteractive Then
            Console.ForegroundColor = ConsoleColor.Gray
            Console.ForegroundColor = LogColor
            Console.WriteLine(msg)
        End If

        If CfgGlobals.SysLogEnabled Then
            UdpSysLog.SendUDPMessage(Text.Encoding.Default.GetBytes(msg), Net.IPAddress.Parse(CfgGlobals.SysLogHostname), CfgGlobals.SysLogPort)
        End If

        If Not OStype() = eOSType.Windows Then dst = LogDestination.none

        Select Case dst
            Case LogDestination.file
                Dim LogFilePath As String = Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), Application.ProductName)
                Dim LogFileName As String = Path.Combine(LogFilePath, Application.ProductName & ".log")
                Try
                    Dim fs As New StreamWriter(LogFileName, True)
                    fs.WriteLine(Now & vbCrLf & "-----------------" & vbCrLf & msg)
                    fs.Close()
                    Return True
                Catch ex As Exception
                    MsgBox(ex.Message)
                    Return False
                End Try
            Case LogDestination.eventlog
                Dim entrytype As EventLogEntryType
                Select Case severity
                    Case LogSeverity.info
                        entrytype = EventLogEntryType.Information
                    Case LogSeverity.warning
                        entrytype = EventLogEntryType.Warning
                    Case LogSeverity.fatal
                        entrytype = EventLogEntryType.Error
                End Select
                EventLog.WriteEntry(Application.ProductName, msg, entrytype)
        End Select

    End Function

End Module
