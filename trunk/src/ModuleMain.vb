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

Imports System.Threading

Module ModuleMain
    Public stopAll As Boolean = False
    Public InstanceDir As String

    Sub Main()
        Console.WindowWidth = 100
        Try
            OSTypeIs = CheckOStype()

            Dim t As New Thread(AddressOf RealMain)
            t.SetApartmentState(ApartmentState.MTA)
            t.Start()

        Catch ex As Exception
            Output(ex.Message & ex.StackTrace, LogDestination.file, LogSeverity.fatal)
        End Try

    End Sub

    Sub RealMain()

        Try
            UdpSysLog.OpenUDPConnection()

            Console.Title = Application.ProductName & " V" & Application.Major & "." & Application.Minor
#If DEBUG Then
            Console.Title &= " DEBUG"
#End If
            Output("Starting " & Application.ProductName & "...")

            For Each arg As String In Environment.GetCommandLineArgs()
                If CBool(InStr(UCase(arg), "SPEEDCS", CompareMethod.Text)) Then
                    GoTo NextArgument
                End If
                If CBool(InStr(UCase(arg), "/INSTANCE=", CompareMethod.Text)) Then
                    InstanceDir = Mid(arg, 11)
                    Output("Loading Config from Instance Directory: " & InstanceDir)
                    Exit For
                Else
                    Output("")
                    Output("Ivalid Arguments Passed!")
                    Output("")
                    Output("Valid Options are:")
                    Output("")
                    Output("/INSTANCE=x  : Name or Number of Instance to start")
                    Output("             : Instance Configuration is automatically created in the")
                    Output("             : %ProgramData%\SpeedCS\x Folder of the Operating System")
                    Output("")
                    Output("/HELP        : Displays this Help")
                    Output("")
                    Output("Press ENTER to continue!")
                    While True
                        Select Case Console.ReadKey.Key
                            Case ConsoleKey.Enter
                                Exit While
                        End Select
                    End While
                    Exit Sub
                End If
NextArgument:
            Next arg

            'Loading Configs
            CfgGlobals.Load()
            CfgClients.Load()

            CfgCardServers.Load()

            CfgCardReaders.Load()

            Readers.StartReaders()

            'Service Datei laden
            'Services.Load()

            'Admin Oberfläche Webserver
            Webserver.StartServer()

            StartUDP()

            NewCamdServer.StartServer()

            Output("Press ESC to quit")
            While True
                Select Case Console.ReadKey.Key
                    Case ConsoleKey.Escape
                        Exit While
                End Select
            End While

            'Console.
            'Console.ReadKey(False)
            'While Not stopAll
            'System.Threading.Thread.Sleep(100)
            'Output("Wait")
            'End While

            NewCamdServer.StopServer()

            StopUDP()

            Webserver.StopServer()

            Readers.StopReaders()

            CfgGlobals.Save()
            CfgClients.Save()
            CfgCardServers.Save()
            CfgCardReaders.Save()

            'System.Threading.Thread.Sleep(2000)
        Catch ex As Exception
            Output(ex.Message & ex.StackTrace, LogDestination.file, LogSeverity.fatal)
        End Try

    End Sub

End Module
