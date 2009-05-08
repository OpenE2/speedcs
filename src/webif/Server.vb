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

Imports System
Imports System.Net
Imports System.Net.Sockets
Imports System.Threading
Imports System.Collections
Imports System.ComponentModel
Imports System.IO
Imports System.Text
Imports System.Configuration
Imports System.Environment
Imports System.Text.RegularExpressions
Imports System.Web

Public Class Server

#Region "UserList Stuff"

    Private Const TOKEN As String = "\[account\]"
    Public Function ParseUser(ByVal userconfig As String) As Integer
        Dim ImportCount As Integer = 0
        Dim newuser As clsSettingsClients.clsClient = Nothing
        Dim m As Match
        Dim lines() As String = userconfig.Split(CChar(vbNewLine))
        For Each line As String In lines
            line = line.Trim

            Dim r As New Regex(TOKEN, RegexOptions.IgnoreCase)
            If r.IsMatch(line) Then
                newuser = New clsSettingsClients.clsClient
                'check if User already exists
                newuser.active = True
            End If

            r = New Regex("(User|#User)(.*\=)(.*)", RegexOptions.IgnoreCase)
            m = r.Match(line)
            If m.Success Then
                If m.Groups(3).Value.Trim <> "" Then newuser.Username = HttpUtility.UrlEncode(m.Groups(3).Value.Trim)
                If CfgClients.Clients.FindByUCRC(newuser.ucrc) Is Nothing Then
                    CfgClients.Clients.Add(newuser)
                    ImportCount += 1
                Else
                    Output("User " & newuser.Username & " already exists.")
                End If
            End If

            r = New Regex("(Pwd|#Pwd)(.*\=)(.*)", RegexOptions.IgnoreCase)
            m = r.Match(line)
            If m.Success Then
                newuser.Password = HttpUtility.UrlEncode(m.Groups(3).Value.Trim)
            End If

            r = New Regex("(Uniq|#Uniq)(.*\=)(.*)", RegexOptions.IgnoreCase)
            m = r.Match(line)
            If m.Success Then
                newuser.Unique = m.Groups(3).Value.Trim
            End If

            r = New Regex("(Group|#Group)(.*\=)(.*)", RegexOptions.IgnoreCase)
            m = r.Match(line)
            If m.Success Then
                'newuser.usergroups = m.Groups(3).Value.Trim
            End If

            r = New Regex("(Sleep|#Sleep)(.*\=)(.*)", RegexOptions.IgnoreCase)
            m = r.Match(line)
            If m.Success Then
                'newuser.usersleep = m.Groups(3).Value.Trim
            End If

            r = New Regex("(AU|#AU)(.*\=)(.*)", RegexOptions.IgnoreCase)
            m = r.Match(line)
            If m.Success Then
                newuser.AutoUpdate = m.Groups(3).Value.Trim
            End If

            r = New Regex("(CAID|#CAID)(.*\=)(.*)", RegexOptions.IgnoreCase)
            m = r.Match(line)
            If m.Success Then
                'newuser.usercaid = m.Groups(3).Value.Trim
            End If

            r = New Regex("(Ident|#Ident)(.*\=)(.*)", RegexOptions.IgnoreCase)
            m = r.Match(line)
            If m.Success Then
                'newuser.userident = m.Groups(3).Value.Trim
            End If

            r = New Regex("(MonLevel|#MonLevel)(.*\=)(.*)", RegexOptions.IgnoreCase)
            m = r.Match(line)
            If m.Success Then
                'newuser.usermonlevel = m.Groups(3).Value.Trim
            End If

            r = New Regex("(Services|#Services)(.*\=)(.*)", RegexOptions.IgnoreCase)
            m = r.Match(line)
            If m.Success Then
                'newuser.userservices = m.Groups(3).Value.Trim
            End If

            r = New Regex("(Hostname|#Hostname)(.*\=)(.*)", RegexOptions.IgnoreCase)
            m = r.Match(line)
            If m.Success Then
                'newuser.userhostname = m.Groups(3).Value.Trim
            End If

            r = New Regex("(class|#class)(.*\=)(.*)", RegexOptions.IgnoreCase)
            m = r.Match(line)
            If m.Success Then
                'newuser.userclass = m.Groups(3).Value.Trim
            End If

            r = New Regex("(chid|#chid)(.*\=)(.*)", RegexOptions.IgnoreCase)
            m = r.Match(line)
            If m.Success Then
                'newuser.userchid = m.Groups(3).Value.Trim
            End If

            r = New Regex("(Numusers|#Numusers)(.*\=)(.*)", RegexOptions.IgnoreCase)
            m = r.Match(line)
            If m.Success Then
                'newuser.usernumusers = m.Groups(3).Value.Trim
            End If

            r = New Regex("(Penalty|#Penalty)(.*\=)(.*)", RegexOptions.IgnoreCase)
            m = r.Match(line)
            If m.Success Then
                'newuser.userpenalty = m.Groups(3).Value.Trim
            End If

        Next
        Return ImportCount
    End Function

#End Region

#Region "Utils"

    Public Function GetMimeType(ByVal filename As String) As String
        Dim ext As String
        If InStr(filename, ".") > 0 And InStr(filename, ".") < Len(filename) Then
            ext = Mid(filename, InStrRev(filename, ".") + 1)
        Else
            ext = ""
        End If
        ext = LCase(ext)
        Select Case ext
            Case Is = "htm" : Return "text/html"
            Case Is = "js" : Return "text/html"
            Case Is = "html" : Return "text/html"
            Case Is = "txt" : Return "text/html"
            Case Is = "jpg" : Return "image/jpeg"
            Case Is = "jpeg" : Return "image/jpeg"
            Case Is = "gif" : Return "image/gif"
            Case Else : Return ""
        End Select
    End Function

    Private Function getGTMDateTime(ByVal D As DateTime) As String
        Dim ArrDay() As String = {"", "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat"}
        Dim ArrMonth() As String = {"Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"}
        Return ArrDay(Weekday(D)) & ", " & D.Day & " " & ArrMonth(Month(D) - 1) & " " & Year(D) & " " & D.ToLongTimeString() & " GMT"
    End Function

    Public Function ByteArrToStr(ByVal b() As Byte) As String
        Return (New System.Text.ASCIIEncoding()).GetString(b)
    End Function

    Public Function GetValueFromUrl(ByVal url As String, ByVal varname As String) As String

        Try
            Dim ret As String = String.Empty
            If (InStr(url, varname & "=")) = 0 Then
                Return ""
            End If

            ret = Mid(url, InStr(url, varname & "="))
            ret = Replace(ret, varname & "=", "")
            If InStr(ret, "&") > 0 Then
                ret = Mid(ret, 1, InStr(ret, "&") - 1)
            ElseIf InStr(ret, " ") > 0 Then
                ret = Mid(ret, 1, InStr(ret, " ") - 1)
            End If
            Return ret
        Catch ex As Exception
            Output("GetValueFromUrl:" & ex.StackTrace, LogDestination.file, LogSeverity.fatal)
            Return ""
        End Try

    End Function

#End Region

#Region "Connection Stuff"

    Private mySocket As Socket

    Public Sub New(ByVal s As Socket)
        mySocket = s
    End Sub

    Public Sub HandleConnection()

        Try
            Dim iStartPos As Integer = 0
            Dim sRequest, sRequestedFile As String
            Dim sMessage As New StringBuilder
            Dim bReceive As Byte()
            ReDim bReceive(256)

            Dim sbuffer As New StringBuilder
            Dim bytecount As Integer = 0

            Try
                bytecount = mySocket.Receive(bReceive, bReceive.Length, SocketFlags.None)
                sbuffer.Append(Encoding.ASCII.GetString(bReceive, 0, bytecount))
                While mySocket.Available <> 0
                    bytecount = mySocket.Receive(bReceive, bReceive.Length, SocketFlags.None)
                    sbuffer.Append(Encoding.ASCII.GetString(bReceive, 0, bytecount))
                    Threading.Thread.Sleep(3)
                End While
            Catch ex As SocketException
                Select Case ex.SocketErrorCode
                    Case SocketError.ConnectionReset
                        mySocket.Close()
                        Exit Sub
                    Case Else
                        Output(ex.SocketErrorCode.ToString)
                End Select
            End Try
            'Output(sbuffer.ToString)
            'debug2(sbuffer)

            If Not checkAuthorization(sbuffer.ToString) Then
                'Output(sbuffer.ToString)
                Send401()
                sbuffer = Nothing
                sMessage = Nothing
                mySocket.Close()
                mySocket = Nothing
                Exit Sub
            End If

            iStartPos = sbuffer.ToString.IndexOf("HTTP", 1)

            '  Get the HTTP text and version e.g. it will return "HTTP/1.1"
            Dim sHttpVersion As String = sbuffer.ToString.Substring(iStartPos, 8)

            ' the web server only accepts get requests.
            If Mid(LCase(sbuffer.ToString), 1, 3) <> "get" And Mid(LCase(sbuffer.ToString), 1, 4) <> "post" Then
                'f not GET request then close socket and exit
                Output("No Valid Request Method")
                mySocket.Close()
                Return
            End If

            ' Extract path and filename from request
            sRequest = sbuffer.ToString.Substring(0, iStartPos - 1)
            sRequest.Replace("\\", "/")
            If ((sRequest.IndexOf(".") < 1) AndAlso (Not sRequest.EndsWith("/"))) Then
                sRequest = sRequest & "/"
            End If

            iStartPos = sRequest.LastIndexOf("/") + 1

            Dim NumParameter As Integer = -1
            Dim StrParameter As String = ""

            Dim r As New Regex("(\?)(.*)")
            Dim m As Match = r.Match(sRequest)
            If m.Success Then
                If IsNumeric(m.Groups(2).Value) Then
                    NumParameter = CInt(m.Groups(2).Value)
                    sRequest = r.Replace(sRequest, "")
                Else
                    StrParameter = m.Groups(2).Value.Trim
                    sRequest = r.Replace(sRequest, "")
                End If
            End If
            r = Nothing

            ' Get the filename
            sRequestedFile = sRequest.Substring(iStartPos)
            'debug(sRequestedFile & " " & parameter)
            'Output(sRequestedFile)

            Select Case sRequestedFile
                Case "", "index.html"
                    GenerateIndex(sMessage, sbuffer)
                    sMessage.Append(PageFooter())
                    SendHeader(sHttpVersion, "", sMessage.Length, "  200 OK")
                Case "settings.html"
                    GenerateSettings(sMessage, sbuffer)
                    sMessage.Append(PageFooter())
                    SendHeader(sHttpVersion, "", sMessage.Length, "  200 OK")
                Case "readers.html"
                    GenerateReaders(sMessage, sbuffer)
                    sMessage.Append(PageFooter())
                    SendHeader(sHttpVersion, "", sMessage.Length, "  200 OK")
                Case "reader.html"
                    GenerateReader(sMessage, sbuffer)
                    sMessage.Append(PageFooter())
                    SendHeader(sHttpVersion, "", sMessage.Length, "  200 OK")
                Case "clients.html"
                    GenerateClients(sMessage, sbuffer)
                    sMessage.Append(PageFooter())
                    SendHeader(sHttpVersion, "", sMessage.Length, "  200 OK")
                Case "servers.html"
                    GenerateServers(sMessage, sbuffer)
                    sMessage.Append(PageFooter())
                    SendHeader(sHttpVersion, "", sMessage.Length, "  200 OK")
                Case "requests.html"
                    sMessage.Append(PageHeader("" & Application.ProductName & " Requests", True))
                    sMessage.Append(ButtonBar())
                    sMessage.Append("<table border=1 width='100%'>")
                    sMessage.Append("<tr class='head'>")
                    sMessage.Append("<th width='33%'>Pending Client Requests</th><th width='33%'>Pending Server Requests</th><th width='33%'>Responses (ECM Cache)</th>")
                    sMessage.Append("</tr>")
                    sMessage.Append("<tr>")
                    sMessage.Append("<td valign='top'>")
                    sMessage.Append("<table border=1 width='100%'>")
                    sMessage.Append("<tr>")
                    sMessage.Append("<th>Age</th><th>ECM CRC</th><th>User</th><th>Servicename</th><th>ClientPID</th>")
                    sMessage.Append("</tr>")
                    For Each req As KeyValuePair(Of UInt32, clsCMDManager.clsCMD0Request) In CacheManager.CMD0Requests
                        For Each ucrc As UInt32 In req.Value.UCRC.Keys
                            sMessage.Append("<tr>")
                            Dim service As String = req.Value.iCAID.ToString("X4") & ":" & req.Value.iSRVID.ToString("X4")
                            Dim c As clsSettingsClients.clsClient = CfgClients.Clients.FindByUCRC(ucrc)
                            If Not c Is Nothing Then

                                sMessage.Append("<td>" & req.Value.Age & "Sec.</td><td>" & service & "</td><td>" & HEX2DEC(c.Username) & "[" & ucrc.ToString("X4") & "]</td><td>" & Services.GetServiceInfo(service).Name & "</td><td>" & "" & "</td><td>" & "" & "</td>")
                            Else
                                sMessage.Append("<td>" & req.Value.Age & "Sec.</td><td>" & service & "</td><td>" & ucrc.ToString("X4") & "</td><td>" & Services.GetServiceInfo(service).Name & "</td><td>" & "" & "</td><td>" & "" & "</td>")
                            End If
                            sMessage.Append("</tr>")
                        Next
                    Next
                    sMessage.Append("</table>")
                    sMessage.Append("</td>")

                    sMessage.Append("<td valign='top'>")
                    sMessage.Append("<table border=1 width='100%'>")
                    sMessage.Append("<tr>")
                    sMessage.Append("<th>Age</th><th>ECM CRC</th><th>User</th><th>Servicename</th><th>ClientPID</th>")
                    sMessage.Append("</tr>")
                    For Each req As clsCache.clsCAMDMsg In Cache.ServerRequests
                        sMessage.Append("<tr>")
                        Dim c As clsSettingsClients.clsClient = CfgClients.Clients.FindByUCRC(req.usercrc)
                        If Not c Is Nothing Then
                            sMessage.Append("<td>" & DateDiff(DateInterval.Second, req.reqtime, Now) & "Sec.</td><td>" & req.ecmcrc.ToString("X8") & "</td><td>" & c.Username & "</td><td>" & req.ServiceName & "</td><td>" & req.ClientPID.ToString("X4") & "</td>")
                        Else
                            sMessage.Append("<td>" & DateDiff(DateInterval.Second, req.reqtime, Now) & "Sec.</td><td>" & req.ecmcrc.ToString("X8") & "</td><td>" & req.usercrc.ToString("X4") & "</td><td>" & req.ServiceName & "</td><td>" & req.ClientPID.ToString("X4") & "</td>")
                        End If
                        sMessage.Append("</tr>")
                    Next
                    sMessage.Append("</table>")

                    sMessage.Append("</td>")

                    sMessage.Append("<td valign='top'>")
                    sMessage.Append("<table border=1 width='100%'>")
                    sMessage.Append("<tr>")
                    sMessage.Append("<th>Age</th><th>CAID:SRVID</th><th>Servicename</th><th>CMD</th>") '<th>Unique</th><th>CMD</th><th>Len</th><th colspan=2>?</th><th colspan=4>CRC ECM</th><th colspan=2>SrvId</th><th colspan=2>Card</th><th colspan=4>PrId</th><th colspan=2>Idx</th><th colspan=2>?</th><th colspan=16>ECM</th>
                    sMessage.Append("</tr>")

                    For Each req As KeyValuePair(Of UInt32, clsCMDManager.clsCMD1Answer) In CacheManager.CMD1Answers
                        sMessage.Append("<tr>")
                        Dim service As String = req.Value.iCAID.ToString("X4") & ":" & req.Value.iSRVID.ToString("X4")
                        sMessage.Append("<td>" & req.Value.Age & "Sec.</td><td>" & service & "</td><td>" & Services.GetServiceInfo(service).Name & "</td><td>" & Hex(req.Value.CMD).PadLeft(2, CChar("0")) & "</td>")
                    Next
                    sMessage.Append("</table>")

                    sMessage.Append("</td>")
                    sMessage.Append("</tr>")
                    sMessage.Append("</table>")
                    sMessage.Append("<br>")
                    sMessage.Append(PageFooter())
                    SendHeader(sHttpVersion, "", sMessage.Length, "  200 OK")
                Case "server.html"
                    Dim lines() As String = Split(sbuffer.ToString, vbCrLf)
                    Dim hostname As String = ""
                    Dim nickname As String = ""
                    Dim port As Integer = 0
                    For Each l As String In lines
                        If Mid(LCase(l), 1, 3).Equals("get") Or Mid(LCase(l), 1, 4).Equals("post") Then
                            hostname = GetValueFromUrl(l, "hostname")
                            port = CInt(GetValueFromUrl(l, "port"))
                            Exit For
                        End If
                    Next
                    If Mid(LCase(sbuffer.ToString), 1, 4).Equals("post") Then
                        For Each s As clsSettingsCardServers.clsCardServer In CfgCardServers.CardServers
                            If s.Hostname.Equals(hostname) And s.Port.Equals(port) Then
                                'Speichern der neuen Einstellungen
                                Dim tmp() As String = Split(sbuffer.ToString, vbCrLf & vbCrLf)
                                If tmp.Length > 1 Then
                                    tmp = Split(tmp(1), "&")
                                    Dim settingschanged As Boolean = False
                                    Dim deleteEntry As Boolean = False
                                    For Each t As String In tmp
                                        Output(t)
                                        Dim l() As String = Split(t, "=")
                                        If l.Length = 2 Then
                                            Select Case l(0)
                                                Case "nickname"
                                                    If s.Nickname.Equals(l(1)) = False Then
                                                        s.Nickname = l(1)
                                                        settingschanged = True
                                                    End If
                                                Case "hostname"
                                                    If s.Hostname.Equals(l(1)) = False Then
                                                        s.Hostname = l(1)
                                                        hostname = s.Hostname
                                                        settingschanged = True
                                                    End If
                                                Case "username"
                                                    If s.Username.Equals(l(1)) = False Then
                                                        s.Username = l(1)
                                                        settingschanged = True
                                                    End If
                                                Case "password"
                                                    If s.Password.Equals(l(1)) = False Then
                                                        s.Password = l(1)
                                                        settingschanged = True
                                                    End If
                                                Case "port"
                                                    If Not s.Port = CDbl(l(1)) Then
                                                        s.Port = CInt(l(1))
                                                        port = s.Port
                                                        settingschanged = True
                                                    End If
                                                Case "active"
                                                    If s.Active.Equals(CBool(l(1))) = False Then
                                                        s.Active = CBool(l(1))
                                                        settingschanged = True
                                                    End If
                                                Case "sendbroadcasts"
                                                    If s.SendBroadcasts.Equals(CBool(l(1))) = False Then
                                                        s.SendBroadcasts = CBool(l(1))
                                                        settingschanged = True
                                                    End If
                                                Case "sendemms"
                                                    If s.SendEMMs.Equals(CBool(l(1))) = False Then
                                                        s.SendEMMs = CBool(l(1))
                                                        settingschanged = True
                                                    End If
                                                Case "IsSCS"
                                                    If s.IsSCS.Equals(CBool(l(1))) = False Then
                                                        s.IsSCS = CBool(l(1))
                                                        settingschanged = True
                                                    End If
                                                Case "sendecms"
                                                    If s.SendECMs.Equals(CBool(l(1))) = False Then
                                                        s.SendECMs = CBool(l(1))
                                                        settingschanged = True
                                                    End If
                                                Case "autoblocked"
                                                    If s.AutoBlocked.Equals(CBool(l(1))) = False Then
                                                        s.AutoBlocked = CBool(l(1))
                                                        settingschanged = True
                                                    End If
                                                Case "supportedCAIDs"
                                                    Dim strTmp As String
                                                    If s.supportedCAID.Count > 0 Then
                                                        For Each iCAID In s.supportedCAID
                                                            strTmp &= Hex(iCAID).PadLeft(4, CChar("0")) & ","
                                                            Output("DEBUG: " & strTmp)
                                                        Next
                                                        strTmp = strTmp.Substring(0, strTmp.Length - 1)
                                                    End If
                                                    If Not strTmp = HttpUtility.UrlDecode(l(1)) Then
                                                        l(1) = HttpUtility.UrlDecode(l(1))
                                                        If l(1).Length > 0 Then
                                                            Dim sCAIDs() As String = l(1).Split(CChar(","))
                                                            If sCAIDs.Length > 0 Then
                                                                s.supportedCAID.Clear()
                                                                For i As Integer = 0 To sCAIDs.Length - 1
                                                                    Dim iCAID As UInt16 = CUShort("&H" & sCAIDs(i))
                                                                    If Not s.supportedCAID.Contains(iCAID) Then
                                                                        s.supportedCAID.Add(iCAID)
                                                                    End If
                                                                Next
                                                            End If
                                                        Else
                                                            s.supportedCAID.Clear()
                                                        End If
                                                        settingschanged = True
                                                    End If
                                                Case "supportedSRVIDs"
                                                    Dim strTmp As String
                                                    If s.supportedSRVID.Count > 0 Then
                                                        For Each iSRVID In s.supportedSRVID
                                                            strTmp &= Hex(iSRVID).PadLeft(4, CChar("0")) & ","
                                                            Output("DEBUG: " & strTmp)
                                                        Next
                                                        strTmp = strTmp.Substring(0, strTmp.Length - 1)
                                                    End If
                                                    If Not strTmp = HttpUtility.UrlDecode(l(1)) Then
                                                        l(1) = HttpUtility.UrlDecode(l(1))
                                                        If l(1).Length > 0 Then
                                                            Dim sSRVIDs() As String = l(1).Split(CChar(","))
                                                            If sSRVIDs.Length > 0 Then
                                                                s.supportedSRVID.Clear()
                                                                For i As Integer = 0 To sSRVIDs.Length - 1
                                                                    Dim iSRVID As UInt16 = CUShort("&H" & sSRVIDs(i))
                                                                    If Not s.supportedSRVID.Contains(iSRVID) Then
                                                                        s.supportedSRVID.Add(iSRVID)
                                                                    End If
                                                                Next
                                                            End If
                                                        Else
                                                            s.supportedSRVID.Clear()
                                                        End If
                                                        settingschanged = True
                                                    End If
                                                Case "remove"
                                                    deleteEntry = True
                                                    settingschanged = True
                                                Case Else
                                                    Output("Not Handled:" & l(0))
                                            End Select
                                        End If
                                    Next
                                    If deleteEntry Then
                                        CfgCardServers.CardServers.Remove(s)
                                    End If
                                    If settingschanged Then
                                        StopUDP()
                                        CfgCardServers.Save()
                                        StartUDP()
                                    End If
                                End If
                                Exit For
                            End If
                        Next
                    End If
                    sMessage.Append(PageHeader("" & Application.ProductName & " Server", True))
                    sMessage.Append(ButtonBar())

                    sMessage.Append("<table border=1>")
                    For Each s As clsSettingsCardServers.clsCardServer In CfgCardServers.CardServers
                        If s.Hostname.Equals(hostname) And s.Port.Equals(port) Then
                            sMessage.Append("<form action='server.html?hostname=" & hostname & "&port=" & port & "' method='post'>")
                            sMessage.Append("<tr><td>Active<td><td>")
                            sMessage.Append("<select name='active'>")
                            sMessage.Append("<option ")
                            If s.Active Then sMessage.Append("selected ")
                            sMessage.Append("value='1'>Yes</option>")
                            sMessage.Append("<option ")
                            If Not s.Active Then sMessage.Append("selected ")
                            sMessage.Append("value='0'>No</option>")
                            sMessage.Append("</select>")
                            sMessage.Append("</td></tr>")
                            If s.Nickname = String.Empty Then
                                s.Nickname = s.Hostname
                            End If
                            sMessage.Append("<tr><td>Nickname<td><td><input type='text' name='nickname' value='" & HEX2DEC(s.Nickname) & "'></td></tr>")
                            sMessage.Append("<tr><td>Hostname<td><td><input type='text' name='hostname' value='" & s.Hostname & "'></td></tr>")
                            sMessage.Append("<tr><td>Port<td><td><input type='text' name='port' value='" & s.Port & "'></td></tr>")

                            sMessage.Append("<tr><td>Allowed CAID(s)<td><td><input type='text' name='supportedCAIDs' value='")

                            Dim strTmp As String = String.Empty
                            If s.supportedCAID.Count > 0 Then
                                For Each iCAID In s.supportedCAID
                                    strTmp &= Hex(iCAID).PadLeft(4, CChar("0")) & ","
                                Next
                                sMessage.Append(strTmp.Substring(0, strTmp.Length - 1))
                            End If
                            sMessage.Append("'></td></tr>")

                            strTmp = String.Empty
                            sMessage.Append("<tr><td>Allowed SRVID(s)<td><td><input type='text' name='supportedSRVIDs' value='")

                            If s.supportedSRVID.Count > 0 Then
                                For Each iSRVID In s.supportedSRVID
                                    strTmp &= Hex(iSRVID).PadLeft(4, CChar("0")) & ","
                                Next
                                sMessage.Append(strTmp.Substring(0, strTmp.Length - 1))
                            End If
                            sMessage.Append("'></td></tr>")

                            sMessage.Append("<tr><td>Send Broadcasts<td><td>")
                            sMessage.Append("<select name='sendbroadcasts'>")
                            sMessage.Append("<option ")
                            If s.SendBroadcasts Then sMessage.Append("selected ")
                            sMessage.Append("value='1'>Yes</option>")
                            sMessage.Append("<option ")
                            If Not s.SendBroadcasts Then sMessage.Append("selected ")
                            sMessage.Append("value='0'>No</option>")
                            sMessage.Append("</select>")
                            sMessage.Append("</td></tr>")

                            sMessage.Append("<tr><td>Send EMMs<td><td>")
                            sMessage.Append("<select name='sendemms'>")
                            sMessage.Append("<option ")
                            If s.SendEMMs Then sMessage.Append("selected ")
                            sMessage.Append("value='1'>Yes</option>")
                            sMessage.Append("<option ")
                            If Not s.SendEMMs Then sMessage.Append("selected ")
                            sMessage.Append("value='0'>No</option>")
                            sMessage.Append("</select>")
                            sMessage.Append("</td></tr>")

                            sMessage.Append("<tr><td>Server is SpeedCS<td><td>")
                            sMessage.Append("<select name='IsSCS'>")
                            sMessage.Append("<option ")
                            If s.IsSCS Then sMessage.Append("selected ")
                            sMessage.Append("value='1'>Yes</option>")
                            sMessage.Append("<option ")
                            If Not s.IsSCS Then sMessage.Append("selected ")
                            sMessage.Append("value='0'>No</option>")
                            sMessage.Append("</select>")
                            sMessage.Append("</td></tr>")

                            sMessage.Append("<tr><td>Send ECMs<td><td>")
                            sMessage.Append("<select name='sendecms'>")
                            sMessage.Append("<option ")
                            If s.SendECMs Then sMessage.Append("selected ")
                            sMessage.Append("value='1'>Yes</option>")
                            sMessage.Append("<option ")
                            If Not s.SendECMs Then sMessage.Append("selected ")
                            sMessage.Append("value='0'>No</option>")
                            sMessage.Append("</select>")
                            sMessage.Append("</td></tr>")

                            sMessage.Append("<tr><td>Username<td><td><input type='text' name='username' value='" & HEX2DEC(s.Username) & "'></td></tr>")
                            sMessage.Append("<tr><td>Password<td><td><input type='text' name='password' value='" & HEX2DEC(s.Password) & "'></td></tr>")

                            sMessage.Append("<tr><td>AutoBlock Failed Requests<td><td>")
                            sMessage.Append("<select name='autoblocked'>")
                            sMessage.Append("<option ")
                            If s.AutoBlocked Then sMessage.Append("selected ")
                            sMessage.Append("value='1'>Yes</option>")
                            sMessage.Append("<option ")
                            If Not s.AutoBlocked Then sMessage.Append("selected ")
                            sMessage.Append("value='0'>No</option>")
                            sMessage.Append("</select>")
                            sMessage.Append("</td></tr>")

                            sMessage.Append("<tr><td>Eintrag l&ouml;schen<td><td><input type='checkbox' name='remove' value='true'></td></tr>")
                            sMessage.Append("<tr><td colspan=3><input type='submit' value='Save'></td></tr>")

                            If s.AutoBlocked Then
                                sMessage.Append("<tr><td colspan=3><table border=1><tr><td>autoblocked</td></tr>")
                                For Each srvidcaid As UInt32 In s.deniedSRVIDCAID
                                    sMessage.Append("<tr><td>")
                                    Dim output() As Byte = BitConverter.GetBytes(srvidcaid)
                                    Dim sid As String = Hex(output(0)).PadLeft(2, CChar("0")) & _
                                                        Hex(output(1)).PadLeft(2, CChar("0")) & _
                                                        ":" & _
                                                        Hex(output(2)).PadLeft(2, CChar("0")) & _
                                                        Hex(output(3)).PadLeft(2, CChar("0"))

                                    sMessage.Append(sid & "</td><td>")
                                    sMessage.Append(Services.GetServiceInfo(sid).Provider & " - " & Services.GetServiceInfo(sid).Name)
                                    sMessage.Append("</td></tr>")
                                Next
                            End If
                            sMessage.Append("</table></td></tr>")
                            sMessage.Append("</form>")
                            Exit For
                        End If
                    Next
                    sMessage.Append("</table>")
                    sMessage.Append("<br>")
                    sMessage.Append(PageFooter())
                    SendHeader(sHttpVersion, "", sMessage.Length, "  200 OK")
                Case "users.html"
                    sMessage.Append(PageHeader("" & Application.ProductName & " Users", True))
                    sMessage.Append(ButtonBar())
                    Dim lines() As String = Split(sbuffer.ToString, vbCrLf)
                    Dim hostname As String = ""
                    Dim port As Integer = 0
                    For Each l As String In lines
                        If Mid(LCase(l), 1, 3).Equals("get") Then
                            If GetValueFromUrl(l, "add").Length > 0 Then
                                Dim nu As New clsSettingsClients.clsClient
                                nu.Username = "New_user"
                                CfgClients.Clients.Add(nu)
                            End If
                            Exit For
                        End If
                    Next

                    sMessage.Append("<table class='buttonbar' border=2><tr class='buttonbar'><td class='buttonbar'><a href='users.html?add=true' class='buttonbar'>Add User</a></td>")
                    sMessage.Append("<td class='buttonbar'><a href='importusers.html'>Import mpcs.conf</a></td></tr></table>")
                    sMessage.Append("<table border=1>")
                    sMessage.Append("<tr>")
                    sMessage.Append("<th>Active</th><th>CRC</th><th>Username</th>")
                    sMessage.Append("</tr>")
                    For Each c As clsSettingsClients.clsClient In CfgClients.Clients
                        sMessage.Append("<tr>")
                        sMessage.Append("<td>" & c.active & "</td><td>" & c.ucrc.ToString("X6") & "</td><td>" & HEX2DEC(c.Username) & "</td><td><a href='user.html?username=" & c.Username & "'>Edit</a></td>")
                        sMessage.Append("</tr>")
                    Next
                    sMessage.Append("</table>")
                    sMessage.Append("<br>")
                    sMessage.Append(PageFooter())
                    SendHeader(sHttpVersion, "", sMessage.Length, "  200 OK")
                Case "user.html"
                    Dim lines() As String = Split(sbuffer.ToString, vbCrLf)
                    Dim username As String = ""
                    For Each l As String In lines
                        If Mid(LCase(l), 1, 3).Equals("get") Or Mid(LCase(l), 1, 4).Equals("post") Then
                            username = GetValueFromUrl(l, "username")
                            Exit For
                        End If
                    Next
                    If Mid(LCase(sbuffer.ToString), 1, 4).Equals("post") Then
                        For Each c As clsSettingsClients.clsClient In CfgClients.Clients
                            If c.Username.Equals(username) Then
                                'Speichern der neuen Einstellungen
                                Dim tmp() As String = Split(sbuffer.ToString, vbCrLf & vbCrLf)
                                If tmp.Length > 1 Then
                                    tmp = Split(tmp(1), "&")
                                    Dim settingschanged As Boolean = False
                                    Dim deleteEntry As Boolean = False
                                    For Each t As String In tmp
                                        Dim l() As String = Split(t, "=")
                                        If l.Length = 2 Then
                                            Select Case l(0)
                                                Case "username"
                                                    If c.Username.Equals(l(1)) = False Then
                                                        c.Username = l(1)
                                                        username = c.Username
                                                        settingschanged = True
                                                    End If
                                                Case "password"
                                                    If c.Password.Equals(l(1)) = False Then
                                                        c.Password = l(1)
                                                        settingschanged = True
                                                    End If
                                                Case "auserver"
                                                    If c.AUServer.Equals(l(1)) = False Then
                                                        c.AUServer = l(1)
                                                        settingschanged = True
                                                    End If
                                                Case "active"
                                                    If c.active.Equals(CBool(l(1))) = False Then
                                                        c.active = CBool(l(1))
                                                        settingschanged = True
                                                    End If
                                                Case "logemm"
                                                    If c.logemm.Equals(CBool(l(1))) = False Then
                                                        c.logemm = CBool(l(1))
                                                        settingschanged = True
                                                    End If
                                                Case "remove"
                                                    deleteEntry = True
                                                    settingschanged = True
                                                Case Else
                                                    Output("Not Handled:" & l(0))
                                            End Select
                                        End If
                                    Next
                                    If deleteEntry Then
                                        CfgClients.Clients.Remove(c)
                                    End If
                                    If settingschanged Then
                                        CfgClients.Save()
                                    End If
                                End If
                                Exit For
                            End If
                        Next
                    End If
                    sMessage.Append(PageHeader("" & Application.ProductName & " User", True))
                    sMessage.Append(ButtonBar())


                    sMessage.Append("<table border=1>")
                    For Each c As clsSettingsClients.clsClient In CfgClients.Clients
                        If c.Username.Equals(username) Then
                            sMessage.Append("<form action='user.html?username=" & username & "' method='post'>")
                            sMessage.Append("<tr><td>Active<td><td>")
                            sMessage.Append("<select name='active'>")
                            sMessage.Append("<option ")
                            If c.active Then sMessage.Append("selected ")
                            sMessage.Append("value='1'>Yes</option>")
                            sMessage.Append("<option ")
                            If Not c.active Then sMessage.Append("selected ")
                            sMessage.Append("value='0'>No</option>")
                            sMessage.Append("</select>")
                            sMessage.Append("</td></tr>")
                            sMessage.Append("<tr><td>AU Server<td><td>")
                            sMessage.Append("<select name='auserver'>")
                            sMessage.Append("<option value=''>None</option>")
                            For Each s As clsSettingsCardServers.clsCardServer In CfgCardServers.CardServers
                                If s.SendEMMs Then
                                    sMessage.Append("<option ")
                                    If c.AUServer.Equals(s.Nickname) Then sMessage.Append("selected ")
                                    sMessage.Append("value='" & HEX2DEC(s.Nickname) & "'>" & HEX2DEC(s.Nickname) & "</option>")
                                End If
                            Next
                            If c.AUServer.Equals("All") Then
                                sMessage.Append("<option selected value='All'>**All**</option>")
                            Else
                                sMessage.Append("<option value='All'>**All**</option>")
                            End If
                            sMessage.Append("</select>")
                            sMessage.Append("</td></tr>")
                            sMessage.Append("<tr><td>Log EMM<td><td>")
                            sMessage.Append("<select name='logemm'>")
                            sMessage.Append("<option ")
                            If c.logemm Then sMessage.Append("selected ")
                            sMessage.Append("value='1'>Yes</option>")
                            sMessage.Append("<option ")
                            If Not c.logemm Then sMessage.Append("selected ")
                            sMessage.Append("value='0'>No</option>")
                            sMessage.Append("</select>")
                            sMessage.Append("</td></tr>")
                            sMessage.Append("<tr><td>Username<td><td><input type='text' name='username' value='" & HEX2DEC(c.Username) & "'></td></tr>")
                            sMessage.Append("<tr><td>Password<td><td><input type='text' name='password' value='" & HEX2DEC(c.Password) & "'></td></tr>")
                            sMessage.Append("<tr><td>Eintrag l&ouml;schen<td><td><input type='checkbox' name='remove' value='true'></td></tr>")
                            sMessage.Append("<tr><td colspan=3><input type='submit' value='Save'></td></tr>")
                            sMessage.Append("</form>")
                            Exit For
                        End If
                    Next
                    sMessage.Append("</table>")
                    sMessage.Append("<br>")
                    sMessage.Append(PageFooter())
                    SendHeader(sHttpVersion, "", sMessage.Length, "  200 OK")
                Case "importusers.html"
                    Dim importcount = -1
                    If Mid(LCase(sbuffer.ToString), 1, 4).Equals("post") Then
                        Dim tmp() As String = Split(sbuffer.ToString, vbCrLf & vbCrLf)
                        If tmp.Length > 1 Then
                            'Output("in")
                            tmp = Split(tmp(1), "&")
                            'Dim settingschanged As Boolean = False
                            'Dim deleteEntry As Boolean = False
                            For Each t As String In tmp
                                Dim l() As String = Split(t, "=")
                                If l.Length = 2 Then
                                    Select Case l(0)
                                        Case "mpcsusers"
                                            importcount = ParseUser(HttpUtility.UrlDecode(l(1)))
                                            CfgClients.Save()
                                            Exit For
                                    End Select
                                End If
                            Next
                        End If
                    End If
                    sMessage.Append(PageHeader("" & Application.ProductName & " Import Users", True))
                    sMessage.Append(ButtonBar())
                    sMessage.Append("<form action='importusers.html' method='post'>")
                    sMessage.Append("<table border=1>")
                    sMessage.Append("<tr>")
                    sMessage.Append("<td><textarea cols=80 rows=20 name='mpcsusers'></textarea></td>")
                    'sMessage.Append("<td><input type='file' name='mpcsusersfile'></td>")
                    sMessage.Append("</tr>")
                    sMessage.Append("<tr>")
                    sMessage.Append("<td><input type='submit' value='Start Import'></td>")
                    sMessage.Append("</tr>")
                    sMessage.Append("</table>")
                    sMessage.Append("</form>")
                    If importcount <> -1 Then
                        sMessage.Append(importcount & " Users imported.")
                    End If
                    sMessage.Append(PageFooter())
                    SendHeader(sHttpVersion, "", sMessage.Length, "  200 OK")
                Case Else
                        sMessage.Append("<H2>404 Error! Request not supported...</H2>")
                        SendHeader(sHttpVersion, "", sMessage.Length, " 404 Not Found")
            End Select
            SendToBrowser(sMessage.ToString)
            mySocket.Close()
            mySocket = Nothing
            sbuffer = Nothing
        Catch ex As Exception
            Output(ex.Message & ex.StackTrace, LogDestination.file, LogSeverity.fatal)
            mySocket.Close()
        Finally
            'Output("Finally")
            'Threading.Thread.CurrentThread.Abort()
        End Try
    End Sub

    Private Sub SendHeader(ByVal sHttpVersion As String, _
                       ByVal sMIMEHeader As String, _
                       ByVal iTotBytes As Integer, _
                       ByVal sStatusCode As String)

        Dim sBuffer As String = ""

        ' if Mime type is not provided set default to text/html
        If (sMIMEHeader.Length = 0) Then sMIMEHeader = "text/html" ' // Default Mime Type is text/html

        sBuffer &= sHttpVersion & sStatusCode & vbNewLine
        sBuffer &= "Server: " & Application.ProductName & " Webinterface" & vbNewLine
        sBuffer &= "Content-Type: " & sMIMEHeader & vbNewLine
        sBuffer &= "Accept-Ranges: none" & vbNewLine
        'sBuffer &= "Transfer-Coding: none" & vbNewLine
        'sBuffer &= "Accept: text/plain;" & vbNewLine
        sBuffer &= "Content-Length: " & iTotBytes & vbNewLine & vbNewLine
        'Dim bSendData As Byte() = 
        SendToBrowser(Encoding.ASCII.GetBytes(sBuffer))

    End Sub

    Private Sub Send401()
        Dim sBuffer As String = ""
        sBuffer &= "HTTP/1.1 401 Unauthorized" & vbCrLf & _
                      "Date: " & getGTMDateTime(Now) & vbCrLf & _
                      "Pragma : no-cache" & vbCrLf & _
                      "WWW-Authenticate: BASIC Realm=" & Application.ProductName & " Secure Area" & vbCrLf & _
                      "Server: " & Application.ProductName & " Webinterface" & vbNewLine & _
                      "Cache-Control: no-cache, post-check=0, pre-check=0" & vbCrLf & _
                      "Connection: close" & vbCrLf

        Dim bSendData As Byte() = Encoding.ASCII.GetBytes(sBuffer)
        SendToBrowser(bSendData)
    End Sub

    Private Sub SendToBrowser(ByVal sData As String)
        SendToBrowser(Encoding.ASCII.GetBytes(sData))
    End Sub

    Private Sub SendToBrowser(ByVal bSendData As Byte())

        Try
            If (mySocket.Connected) Then
                Dim numbytes As Integer
                numbytes = mySocket.Send(bSendData, bSendData.Length, 0)
                If numbytes = -1 Then
                Else
                End If
            Else
            End If
        Catch sex As SocketException
            Select Case sex.SocketErrorCode
                Case SocketError.ConnectionReset
                    mySocket.Close()
                Case Else
                    Output("SendToBrowser() " & sex.SocketErrorCode.ToString)
            End Select
        Catch ex As Exception
            Output("SendToBrowser() " & ex.Message & vbCrLf & ex.StackTrace)
        End Try
    End Sub

    Private Function checkAuthorization(ByVal Header As String) As Boolean
        Dim login As String = "", i As Integer

        Try
            Header = Header.Replace(vbCr, vbLf) & vbLf
            i = InStr(Header.ToUpper(), "AUTHORIZATION")
            If i > 0 Then
                login = Strings.Left(Header, InStr(i + 1, Header, vbLf) - 1)
                login = Strings.Mid(login, InStrRev(login, " ") + 1)
                login = ByteArrToStr(Convert.FromBase64String(login))
                'Output(login)
                If Not CfgGlobals.AdminUsername & ":" & CfgGlobals.AdminPassword = login Then
                    Return False
                Else
                    Return True
                End If
            End If
        Catch ex As Exception
            'MsgBox(Header)
            'MsgBox(ex.Message & ex.StackTrace)
            'RaiseEvent Message(ex.Message)
            Return False
        End Try
    End Function

#End Region

#Region "Webpages"

#Region "Common Elements"

    Private Function PageHeader(ByVal title As String, Optional ByVal hasRefresh As Boolean = True) As String

        Dim s As New StringBuilder
        s.Append("<html>" & vbCrLf)
        s.Append("<head>" & vbCrLf)
        '<link rel='shortcut icon' href='favicon.ico' type='image/x-icon'><link rel='icon' href='favicon.ico' type='image/x-icon'>"
        s.Append("<title>" & title & "</title>" & vbCrLf)
        Dim stylesfilename As String = System.Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) & "\" & Application.ProductName & "\" & "\styles.css"
        If IO.File.Exists(stylesfilename) Then
            Using sr As New StreamReader(stylesfilename)
                s.Append(sr.ReadToEnd)
                sr.Close()
            End Using
        Else
            Try
                Using sr As New StreamReader(Reflection.Assembly.GetExecutingAssembly.GetManifestResourceStream("SpeedCS.styles.css"))
                    s.Append(sr.ReadToEnd)
                    sr.Close()
                End Using
            Catch ex As Exception
            End Try
        End If
        s.Append("</head>" & vbCrLf)
        s.Append("<body>")
        s.Append("<center>")
        Return s.ToString
    End Function

    Private Function ButtonBar() As String
        Dim s As String = ""
        s &= "<center>"
        s &= "<table border=2 cellspacing=10 cellpadding=10><tr class='buttonbar'>"
        s &= "<th class='buttonbar'><a href='index.html' class='buttonbar'>Statistics</a></th>"
        s &= "<th class='buttonbar'><a href='settings.html' class='buttonbar'>Settings</a></th>"
        s &= "<th class='buttonbar'><a href='clients.html' class='buttonbar'>Active Clients</a></th>"
        s &= "<th class='buttonbar'><a href='requests.html' class='buttonbar'>Requests</a></th>"
        s &= "<th class='buttonbar'><a href='users.html' class='buttonbar'>Users</a></th>"
        s &= "<th class='buttonbar'><a href='servers.html' class='buttonbar'>Servers</a></th>"
        s &= "<th class='buttonbar'><a href='readers.html' class='buttonbar'>Readers</a></th>"
        s &= "</tr>"
        s &= "</table>"
        s &= "</center>"
        s &= "<br>"
        Return s
    End Function

    Private Function PageFooter() As String
        Dim s As String = String.Empty
        'Dim s As String = "<br><br><table align = 'center' width = '100%'><tr><td><div class='footline'>"
        'Dim s As String = "<br><br><table align = 'center' ><tr><td align = 'center'>"
        's &= "<img src='logo.png'>"
        's &= " powered by MPCS microMon Version " & versionText & " | "
        's &= targetText & " | "
        's &= "request timestamp: " & Date.Now.ToString
        s &= "<small>" & Application.ProductName & " " & Application.ProductVersion & "</small>"
        's &= "</td></tr><table></body></html>"
        s &= "</center></body></html>"
        Return s
    End Function

#End Region

#Region "index.html"

    Private Sub GenerateIndex(ByVal sMessage As StringBuilder, ByVal sBuffer As StringBuilder)

        sMessage.Append(PageHeader(Application.ProductName & " Index", True))
        sMessage.Append(ButtonBar())
        sMessage.Append("<table border=1>")
        sMessage.Append("<tr><th>Computer</th><td>" & Environment.MachineName & "</td></tr>")
        sMessage.Append("<tr><th>OS</th><td>" & Environment.OSVersion.ToString & "</td></tr>")
        sMessage.Append("<tr><th>OS Running time</th><td>" & Environment.TickCount / 1000 & "Sec.</td></tr>")
        sMessage.Append("<tr><th>.NET Version</th><td>" & Environment.Version.ToString & "</td></tr>")
        sMessage.Append("<tr><th>Programversion</th><td>" & Application.ProductName & " " & Application.ProductVersion & "</td></tr>")
        sMessage.Append("<tr><th>RAM usage</th><td>" & Environment.WorkingSet / 1024 & "KB</td></tr>")
        sMessage.Append("<tr><th>Listen on Port</th><td>" & CfgGlobals.cs357xPort & "</td></tr>")
        sMessage.Append("<tr><th>Working Directory</th><td>" & CurrentDirectory & "</td></tr>")
        Dim filepath As String = Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), Application.ProductName)
        If Not filepath.EndsWith(InstanceDir) Then
            filepath = Path.Combine(filepath, InstanceDir)
        End If
        sMessage.Append("<tr><th>Config Directory</th><td>" & filepath & "\" & "</td></tr>")
        sMessage.Append("<tr><th>Domain</th><td>" & Environment.UserDomainName & "</td></tr>")
        sMessage.Append("<tr><th>Interactive</th><td>" & Environment.UserInteractive & "</td></tr>")
        sMessage.Append("<tr><th>Username</th><td>" & Environment.UserName & "</td></tr>")
        sMessage.Append("</table>")
        sMessage.Append("<br>")
    End Sub

#End Region

#Region "settings.html"

    Private Sub GenerateSettings(ByVal sMessage As StringBuilder, ByVal sBuffer As StringBuilder)
        'Output("GenerateSettings()")
        'Output(sBuffer.ToString)
        If Mid(LCase(sBuffer.ToString), 1, 4).Equals("post") Then
            'Speichern der neuen Einstellungen
            Dim tmp() As String = Split(sBuffer.ToString, vbCrLf & vbCrLf)
            If tmp.Length > 1 Then
                tmp = Split(tmp(1), "&")
                For Each t As String In tmp
                    Dim l() As String = Split(t, "=")
                    If l.Length = 2 Then
                        Dim savechanges As Boolean = False
                        Dim restartsyslog As Boolean = False
                        Select Case l(0)
                            Case "cs357xuse"
                                If CfgGlobals.cs357xUse <> CBool(Trim(l(1))) Then
                                    UdpClientManager.CloseUDPConnection()
                                    CfgGlobals.cs357xUse = CBool(Trim(l(1)))
                                    UdpClientManager.OpenUDPConnection()
                                    savechanges = True
                                End If
                            Case "cs357xport"
                                If CInt(l(1)) < 65535 Then
                                    If CfgGlobals.cs357xPort <> CInt(l(1)) Then
                                        UdpClientManager.CloseUDPConnection()
                                        CfgGlobals.cs357xPort = CInt(l(1))
                                        UdpClientManager.Port = CfgGlobals.cs357xPort
                                        UdpClientManager.OpenUDPConnection()
                                        savechanges = True
                                    End If
                                End If
                            Case "newcamduse"
                                If CfgGlobals.NewCamdUse <> CBool(Trim(l(1))) Then
                                    NewCamdServer.StopServer()
                                    CfgGlobals.NewCamdUse = CBool(Trim(l(1)))
                                    NewCamdServer.StartServer()
                                    savechanges = True
                                End If
                            Case "newcamdport"
                                If CInt(l(1)) < 65535 Then
                                    If CfgGlobals.NewCamdPort <> CInt(l(1)) Then
                                        NewCamdServer.StopServer()
                                        CfgGlobals.NewCamdPort = CInt(l(1))
                                        NewCamdServer.StartServer()
                                        savechanges = True
                                    End If
                                End If
                            Case "adminport"
                                If CInt(l(1)) < 65535 Then
                                    If CfgGlobals.AdminPort <> CInt(l(1)) Then
                                        CfgGlobals.AdminPort = CInt(l(1))
                                        savechanges = True
                                    End If
                                End If
                            Case "adminusername"
                                If CfgGlobals.AdminUsername <> Trim(l(1)) Then
                                    CfgGlobals.AdminUsername = Trim(l(1))
                                    savechanges = True
                                End If
                            Case "adminpassword"
                                If CfgGlobals.AdminPassword <> Trim(l(1)) Then
                                    CfgGlobals.AdminPassword = Trim(l(1))
                                    savechanges = True
                                End If
                            Case "syslogenabled"
                                If CfgGlobals.SysLogEnabled <> CBool(Trim(l(1))) Then
                                    CfgGlobals.SysLogEnabled = CBool(Trim(l(1)))
                                    savechanges = True
                                    restartsyslog = True
                                End If
                            Case "sysloghostname"
                                If CfgGlobals.SysLogHostname <> Trim(l(1)) Then
                                    CfgGlobals.SysLogHostname = Trim(l(1))
                                    savechanges = True
                                End If
                            Case "syslogport"
                                If CInt(l(1)) < 65535 Then
                                    If CfgGlobals.SysLogPort <> CUShort(l(1)) Then
                                        CfgGlobals.SysLogPort = CUShort(l(1))
                                        savechanges = True
                                    End If
                                End If

                        End Select
                        If savechanges Then CfgGlobals.Save()
                        If restartsyslog = True Then
                            UdpSysLog.CloseUDPConnection()
                            If CfgGlobals.SysLogEnabled Then UdpSysLog.OpenUDPConnection()
                        End If
                    End If
                Next
            End If
        End If
        sMessage.Append(PageHeader("" & Application.ProductName & " Settings", True))
        sMessage.Append(ButtonBar())
        sMessage.Append("<form action='settings.html' method='POST'>")
        sMessage.Append("<table border=1>")
        sMessage.Append("<tr class='head'><th colspan=2>cs357x Protocol</td></tr>")

        sMessage.Append("<tr><th>enabled</th><td>")
        sMessage.Append("<select name='cs357xuse'>")
        sMessage.Append("<option ")
        If CfgGlobals.cs357xUse Then sMessage.Append("selected ")
        sMessage.Append("value='1'>Yes</option>")
        sMessage.Append("<option ")
        If Not CfgGlobals.cs357xUse Then sMessage.Append("selected ")
        sMessage.Append("value='0'>No</option>")
        sMessage.Append("</select>")
        sMessage.Append("</td></tr>")

        sMessage.Append("<tr><th>Listenport</th><td><input size=4 type='text' name='cs357xport' value='" & CfgGlobals.cs357xPort & "'></td></tr>")

        sMessage.Append("<tr class='head'><th colspan=2>NewCamd Protocol</td></tr>")

        sMessage.Append("<tr><th>enabled</th><td>")
        sMessage.Append("<select name='newcamduse'>")
        sMessage.Append("<option ")
        If CfgGlobals.NewCamdUse Then sMessage.Append("selected ")
        sMessage.Append("value='1'>Yes</option>")
        sMessage.Append("<option ")
        If Not CfgGlobals.NewCamdUse Then sMessage.Append("selected ")
        sMessage.Append("value='0'>No</option>")
        sMessage.Append("</select>")
        sMessage.Append("</td></tr>")

        sMessage.Append("<tr><th>Listenport</th><td><input size=4 type='text' name='newcamdport' value='" & CfgGlobals.NewCamdPort & "'></td></tr>")
        sMessage.Append("<tr><th>Key</th><td><input size=28 type='text' name='newcamdkey' value='" & CfgGlobals.NewCamdKey & "'></td></tr>")
        'TODO: DES Key should be here too

        sMessage.Append("<tr class='head'><th colspan=2>Webinterface</td></tr>")
        sMessage.Append("<tr><th>TCP Port</th><td><input size=4 type='text' name='adminport' value='" & CfgGlobals.AdminPort & "'></td></tr>")
        sMessage.Append("<tr><th>Username</th><td><input type='text' name='adminusername' value='" & CfgGlobals.AdminUsername & "'></td></tr>")
        sMessage.Append("<tr><th>Password</th><td><input type='password' name='adminpassword' value='" & CfgGlobals.AdminPassword & "'></td></tr>")
        sMessage.Append("<tr class='head'><th colspan=2>Syslog</td></tr>")
        sMessage.Append("<tr><th>enabled</th><td>")
        sMessage.Append("<select name='syslogenabled'>")
        sMessage.Append("<option ")
        If CfgGlobals.SysLogEnabled Then sMessage.Append("selected ")
        sMessage.Append("value='1'>Yes</option>")
        sMessage.Append("<option ")
        If Not CfgGlobals.SysLogEnabled Then sMessage.Append("selected ")
        sMessage.Append("value='0'>No</option>")
        sMessage.Append("</select>")
        sMessage.Append("</td></tr>")

        sMessage.Append("<tr><th>Hostname</th><td><input type='text' name='sysloghostname' value='" & CfgGlobals.SysLogHostname & "'></td></tr>")
        sMessage.Append("<tr><th>UDP-Port</th><td><input size=4 type='text' name='syslogport' value='" & CfgGlobals.SysLogPort & "'></td></tr>")

        sMessage.Append("<input type='hidden' name='check' value='1'>")
        sMessage.Append("<tr><th colspan=2><input type='submit' value='Speichern'></th></tr>")
        sMessage.Append("</table>")
        sMessage.Append("</form>")
    End Sub

#End Region

#Region "readers.html"

    Private Sub GenerateReaders(ByVal sMessage As StringBuilder, ByVal sBuffer As StringBuilder)

        sMessage.Append(PageHeader("" & Application.ProductName & " Readers", True))
        sMessage.Append(ButtonBar())
        Dim lines() As String = Split(sBuffer.ToString, vbCrLf)
        Dim uniquename As String = ""
        For Each l As String In lines
            If Mid(LCase(l), 1, 3).Equals("get") Then
                If GetValueFromUrl(l, "add").Length > 0 Then
                    Dim nr As New clsSettingsCardReaders.clsCardReader
                    nr.UniqueName = "New_Reader"
                    CfgCardReaders.CardReaders.Add(nr)
                End If
                Exit For
            End If
        Next

        sMessage.Append("<a href='readers.html?add=true'>Add Reader</a>")
        sMessage.Append("<table border=1>")
        sMessage.Append("<tr>")
        sMessage.Append("<th>Active</th><th>Uniquename</th>")
        sMessage.Append("</tr>")
        For Each s As clsSettingsCardReaders.clsCardReader In CfgCardReaders.CardReaders
            sMessage.Append("<tr>")
            sMessage.Append("<td>" & s.Active & "</td><td>" & s.UniqueName & "</td><td><a href='reader.html?uniquename=" & s.UniqueName & "'>Edit</a></td>")
            sMessage.Append("</tr>")
        Next
        sMessage.Append("</table>")
        sMessage.Append("<br>")

    End Sub

    Private Sub GenerateReader(ByVal sMessage As StringBuilder, ByVal sBuffer As StringBuilder)
        Dim lines() As String = Split(sBuffer.ToString, vbCrLf)
        Dim uniquename As String = ""
        Dim port As Integer = 0
        For Each l As String In lines
            If Mid(LCase(l), 1, 3).Equals("get") Or Mid(LCase(l), 1, 4).Equals("post") Then
                uniquename = GetValueFromUrl(l, "uniquename")
                Exit For
            End If
        Next
        If Mid(LCase(sBuffer.ToString), 1, 4).Equals("post") Then
            For Each r As clsSettingsCardReaders.clsCardReader In CfgCardReaders.CardReaders
                If r.UniqueName.Equals(uniquename) Then
                    'Speichern der neuen Einstellungen
                    Dim tmp() As String = Split(sBuffer.ToString, vbCrLf & vbCrLf)
                    If tmp.Length > 1 Then
                        tmp = Split(tmp(1), "&")
                        Dim settingschanged As Boolean = False
                        Dim deleteEntry As Boolean = False
                        For Each t As String In tmp
                            Output(t)
                            Dim l() As String = Split(t, "=")
                            If l.Length = 2 Then
                                Select Case l(0)
                                    Case "uniquename"
                                        If r.UniqueName.Equals(l(1)) = False Then
                                            r.UniqueName = l(1)
                                            uniquename = r.UniqueName
                                            settingschanged = True
                                        End If
                                    Case "portname"
                                        If r.PortName.Equals(l(1)) = False Then
                                            r.PortName = l(1)
                                            settingschanged = True
                                        End If
                                    Case "baudrate"
                                        If r.Baudrate.Equals(l(1)) = False Then
                                            r.Baudrate = CUShort(l(1))
                                            settingschanged = True
                                        End If
                                    Case "databits"
                                        If r.DataBits.Equals(l(1)) = False Then
                                            r.DataBits = CUShort(l(1))
                                            settingschanged = True
                                        End If
                                    Case "stopbits"
                                        If r.StopBits.Equals(l(1)) = False Then
                                            r.StopBits = CUShort(l(1))
                                            settingschanged = True
                                        End If
                                    Case "timeout"
                                        If r.TimeOut.Equals(l(1)) = False Then
                                            r.TimeOut = CUShort(l(1))
                                            settingschanged = True
                                        End If
                                    Case "parity"
                                        If r.Parity.Equals(l(1)) = False Then
                                            Select Case CStr(l(1))
                                                Case "None"
                                                    r.Parity = Ports.Parity.None
                                                Case "Even"
                                                    r.Parity = Ports.Parity.Even
                                                Case "Odd"
                                                    r.Parity = Ports.Parity.Odd
                                                Case "Mark"
                                                    r.Parity = Ports.Parity.Mark
                                                Case "Space"
                                                    r.Parity = Ports.Parity.Space
                                            End Select
                                            settingschanged = True
                                        End If
                                    Case "active"
                                        If r.Active.Equals(CBool(l(1))) = False Then
                                            r.Active = CBool(l(1))
                                            settingschanged = True
                                        End If
                                    Case "remove"
                                        deleteEntry = True
                                        settingschanged = True
                                    Case Else
                                        Output("Not Handled:" & l(0))
                                End Select
                            End If
                        Next
                        If deleteEntry Then
                            CfgCardReaders.CardReaders.Remove(r)
                        End If
                        If settingschanged Then
                            'StopUDP()
                            CfgCardReaders.Save()
                            'StartUDP()
                        End If
                    End If
                    Exit For
                End If
            Next
        End If
        sMessage.Append(PageHeader("" & Application.ProductName & " Reader", True))
        sMessage.Append(ButtonBar())


        sMessage.Append("<table border=1>")
        For Each r As clsSettingsCardReaders.clsCardReader In CfgCardReaders.CardReaders
            If r.UniqueName.Equals(uniquename) Then
                sMessage.Append("<form action='reader.html?uniquename=" & uniquename & "' method='post'>")
                sMessage.Append("<tr><td>Active<td><td>")
                sMessage.Append("<select name='active'>")
                sMessage.Append("<option ")
                If r.Active Then sMessage.Append("selected ")
                sMessage.Append("value='1'>Yes</option>")
                sMessage.Append("<option ")
                If Not r.Active Then sMessage.Append("selected ")
                sMessage.Append("value='0'>No</option>")
                sMessage.Append("</select>")
                sMessage.Append("</td></tr>")
                sMessage.Append("<tr><td>Uniquename<td><td><input type='text' name='uniquename' value='" & r.UniqueName & "'></td></tr>")
                sMessage.Append("<tr><td>Portname<td><td><input type='text' name='portname' value='" & r.PortName & "'></td></tr>")
                sMessage.Append("<tr><td>Baudrate<td><td><input type='text' name='baudrate' value='" & r.Baudrate & "'></td></tr>")
                sMessage.Append("<tr><td>DataBits<td><td><input type='text' name='databits' value='" & r.DataBits & "'></td></tr>")
                sMessage.Append("<tr><td>Parity<td><td><input type='text' name='arity' value='" & r.Parity.ToString & "'></td></tr>")
                sMessage.Append("<tr><td>StopBits<td><td><input type='text' name='stopbits' value='" & r.StopBits & "'></td></tr>")
                sMessage.Append("<tr><td>Timeout<td><td><input type='text' name='timeout' value='" & r.TimeOut & "'></td></tr>")
                sMessage.Append("<tr><td>Eintrag l&ouml;schen<td><td><input type='checkbox' name='remove' value='true'></td></tr>")
                sMessage.Append("<tr><td colspan=2><input type='submit' value='Save'></td></tr>")
                sMessage.Append("</form>")
                Exit For
            End If
        Next
        sMessage.Append("</table>")
        sMessage.Append("<br>")
    End Sub

#End Region

#Region "clients.html"

    Private Sub GenerateClients(ByVal sMessage As StringBuilder, ByVal sBuffer As StringBuilder)

        sMessage.Append(PageHeader("" & Application.ProductName & " Clients", True))
        sMessage.Append(ButtonBar())
        sMessage.Append("<table border=1>")
        sMessage.Append("<tr class='head'>")
        sMessage.Append("<th>Username</th><th>UserCRC</th><th>Last Access</th><th>Last Channel</th><th>IP</th><th>Port</th><th>AU</th>")
        sMessage.Append("</tr>")

        For Each c As clsSettingsClients.clsClient In CfgClients.Clients
            If DateDiff(DateInterval.Second, c.lastrequest, Now) <= 120 Then
                sMessage.Append("<tr>")
                sMessage.Append("<td>" & HEX2DEC(c.Username) & "</td><td>" & c.ucrc.ToString("X6") & "</td><td>" & c.lastrequest & "</td><td>" & c.lastRequestedService.Provider & " - " & c.lastRequestedService.Name & "</td><td>" & c.SourceIp & "</td><td>" & c.SourcePort & "</td><td>" & c.AUServer & "</td>")
                sMessage.Append("</tr>")
            End If
        Next
        sMessage.Append("</table>")
        sMessage.Append("<br>")

    End Sub

#End Region

#Region "servers.html"

    Private Sub GenerateServers(ByVal sMessage As StringBuilder, ByVal sBuffer As StringBuilder)

        sMessage.Append(PageHeader("" & Application.ProductName & " Servers", True))
        sMessage.Append(ButtonBar())
        Dim lines() As String = Split(sBuffer.ToString, vbCrLf)
        Dim hostname As String = ""
        Dim port As Integer = 0
        For Each l As String In lines
            If Mid(LCase(l), 1, 3).Equals("get") Then
                If GetValueFromUrl(l, "add").Length > 0 Then
                    CfgCardServers.CardServers.Add(New clsSettingsCardServers.clsCardServer)
                End If
                Exit For
            End If
        Next

        sMessage.Append("<a href='servers.html?add=true'>Add Server</a>")
        sMessage.Append("<table border=1>")
        sMessage.Append("<tr>")
        sMessage.Append("<th>Active</th><th>Nickname</th><th>Hostname/IP</th><th>Port</th><th>Username</th><th>UserCRC</th>")
        sMessage.Append("</tr>")
        For Each s As clsSettingsCardServers.clsCardServer In CfgCardServers.CardServers
            sMessage.Append("<tr>")
            sMessage.Append("<td>" & s.Active & "</td><td>" & HEX2DEC(s.Nickname) & "</td><td>" & s.Hostname & "</td><td>" & s.Port & "</td><td>" & HEX2DEC(s.Username) & "</td><td>" & s.UCRC.ToString("X6") & "</td><td><a href='server.html?hostname=" & s.Hostname & "&port=" & s.Port & "'>Edit</a></td>")
            sMessage.Append("</tr>")
        Next
        sMessage.Append("</table>")
        sMessage.Append("<br>")
    End Sub

#End Region

#End Region

End Class