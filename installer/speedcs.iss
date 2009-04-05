;
;	Copyright (C) 2009 SpeedCS Team
;	http://streamboard.gmc.to
;
;  This Program is free software; you can redistribute it and/or modify
;  it under the terms of the GNU General Public License as published by
;  the Free Software Foundation; either version 2, or (at your option)
;  any later version.
;
;  This Program is distributed in the hope that it will be useful,
;  but WITHOUT ANY WARRANTY; without even the implied warranty of
;  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
;  GNU General Public License for more details.
;
;  You should have received a copy of the GNU General Public License
;  along with GNU Make; see the file COPYING.  If not, write to
;  the Free Software Foundation, 675 Mass Ave, Cambridge, MA 02139, USA.
;  http://www.gnu.org/copyleft/gpl.html

#include "isxdl.iss"

#define SourcePath "..\src\bin\Debug\" ; define variable

#define GetVersionMajorMinor(str FileName) Local[0] = GetFileVersion(SourcePath + FileName), DeleteToFirstPeriod(Local[0]) + '.' + DeleteToFirstPeriod(Local[0])
#define GetVersionMajorMinorNoDot(str FileName)  Local[0] = GetFileVersion(SourcePath + FileName),   DeleteToFirstPeriod(Local[0]) + DeleteToFirstPeriod(Local[0])
#define GetVersionRelease(str FileName)  Local[0] = GetFileVersion(SourcePath + FileName),   DeleteToFirstPeriod(Local[0]),DeleteToFirstPeriod(Local[0]),DeleteToFirstPeriod(Local[0])

#define MyAppVer GetVersionMajorMinor("speedcs.exe") ; define variable
#define MyAppReleaseVer GetVersionRelease("speedcs.exe") ; define variable
#define MyAppRevisionVer "$WCREV$" ; define variable
#define MyAppName "SpeedCS" ; define variable
#define MyAppDeveloper "SpeedCS Team" ; define variable
#define MyAppDeveloperLong "SpeedCS Team" ; define variable
#define MyAppYear "2009" ; define variable


[Setup]
AppName={#MyAppName}
AppVerName={#MyAppName} {#MyAppVer}.{#MyAppReleaseVer}.{#MyAppRevisionVer}
AppVersion={#MyAppVer}
AppCopyright=Copyright © {#MyAppYear} {#MyAppDeveloper}
AppPublisher={#MyAppDeveloper}
AppPublisherURL=http://streamboard.gmc.to
DefaultDirName={pf}\SpeedCS
UninstallDisplayIcon={app}\SpeedCS.exe
UninstallDisplayName={#MyAppName}
AppMutex=SpeedCS
DisableProgramGroupPage=1
MinVersion=4.1,4
DisableStartupPrompt=1
WindowVisible=0
WindowShowCaption=0
Compression=lzma/ultra
ExtraDiskSpaceRequired=1048576
OutputDir=output
OutputBaseFilename=speedcs-setup-{#MyAppVer}.{#MyAppReleaseVer}.{#MyAppRevisionVer}
ShowLanguageDialog=no
InfoBeforeFile=info.txt

[Files]
Source: german.ini; Flags: dontcopy
Source: {#SourcePath}speedcs.exe; DestDir: {app}; Flags: ignoreversion
Source: {#SourcePath}speedcs.pdb; DestDir: {app}; Flags: ignoreversion
Source: redist\speedcs.srvid; DestDir:  {commonappdata}\{#MyAppName};
Source: redist\styles.css; DestDir: {app}; Flags: onlyifdoesntexist
;Source: redist\crypting.dll; DestDir: {app}; Flags: ignoreversion
Source: redist\srvany.exe; DestDir: {app}; Flags: ignoreversion
Source: redist\instsrv.exe; DestDir: {app}; Flags: ignoreversion

[Tasks]
Name: desktopicon; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}";
Name: quicklaunchicon; Description: "{cm:CreateQuickLaunchIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Icons]
Name: {commonprograms}\{#MyAppName} Administration; Filename: http://localhost:8100; IconIndex: 0;
Name: {userdesktop}\{#MyAppName} Administration; Filename: http://localhost:8100; IconIndex: 0; Tasks: desktopicon
Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\{#MyAppName} Administration"; Filename: http://localhost:8100; Tasks: quicklaunchicon

[Languages]
Name: "en"; MessagesFile: "compiler:Default.isl"

[Run]
Filename: {ini:{tmp}\dep.ini,install,dotnetRedist20}; Parameters: /Q /T:{tmp}\dotnetfx; Description: .NET Extract; Flags: skipifdoesntexist; StatusMsg: Entpacke Microsoft .NET...
Filename: {tmp}\dotnetfx\dotnetfx.exe; Parameters: "/Q /C:""install /q"""; Description: .NET Install; StatusMsg: Installiere Microsoft .NET... (Dies kann einige Minuten benötigen); Flags: skipifdoesntexist

;Filename: net.exe; Parameters: "stop {#MyAppName}"; Flags: runhidden; StatusMsg:Stopping {#MyAppName} Service;
;Filename: {app}\instsrv.exe; Parameters: {#MyAppName} REMOVE; Flags: runhidden; StatusMsg:Remove Existing {#MyAppName} Service;
Filename: {app}\instsrv.exe; Parameters: "{#MyAppName} ""{app}\srvany.exe"" ""{app}\SpeedCS.exe"""; Flags: runhidden; StatusMsg:Install {#MyAppName} Service;
Filename: net.exe; Parameters: "start {#MyAppName}"; Description: "{cm:LaunchProgram,{#MyAppName}}"; Flags: runhidden; StatusMsg:Starting {#MyAppName} Service;
Filename: http://localhost:8100; Description: "{cm:LaunchProgram,{#MyAppName} Administration}"; Flags: postinstall nowait skipifsilent shellexec

[Registry]
Root: HKLM; Subkey: SYSTEM\CurrentControlSet\Services\{#MyAppName}\Parameters; ValueType: string; ValueName: Application; ValueData: {app}\SpeedCS.exe
Root: HKLM; Subkey: SYSTEM\CurrentControlSet\Services\{#MyAppName}\Parameters; ValueType: string; ValueName: AppDirectory; ValueData: {app}
Root: HKLM; Subkey: SYSTEM\CurrentControlSet\Services\{#MyAppName}; ValueType: string; ValueName: Description; ValueData: {#MyAppName}

[UninstallRun]
Filename: net.exe; Parameters: "stop {#MyAppName}"; Flags: runhidden; StatusMsg:Stopping {#MyAppName} Service;
Filename: {app}\instsrv.exe; Parameters: {#MyAppName} REMOVE; Flags: runhidden; StatusMsg:Removing {#MyAppName} Service;

[_ISTool]
EnableISX=true

[Code]
var
  mdacPath, jetPath, dotnetRedistPath: string;
  downloadNeeded: boolean;
  exclusiveNeeded: boolean;
  memoDependenciesNeeded: string;

const
  mdacURL = 'http://download.microsoft.com/download/MDAC26/Refresh/2.0/W98NT42KMeXP/EN-US/MDAC_TYP.EXE';
  jetURL = 'http://download.microsoft.com/download/dasdk/install/40SP3/WIN98Me/EN-US/Jet40Sp3_Comp.exe';
  dotnetRedistURL20 = 'http://download.microsoft.com/download/5/6/7/567758a3-759e-473e-bf8f-52154438565a/dotnetfx.exe';

function InitializeSetup(): Boolean;
var
  sRet: string;

begin
  Result := true;

ExtractTemporaryFile('german.ini');
isxdl_SetOption('language',ExpandConstant('{tmp}\german.ini'));

  // Check for required MDAC installation
  sRet := '';
  RegQueryStringValue(HKLM, 'Software\Microsoft\DataAccess', 'FullInstallVer', sRet);
  if (not exclusiveNeeded) and (sRet < '2.7') then begin
    memoDependenciesNeeded := memoDependenciesNeeded + '      Microsoft Data Access Components 2.7' #13;
    mdacPath := '\dependencies\MDAC_TYP.EXE';
    if not FileExists(mdacPath) then begin
      mdacPath := ExpandConstant('{tmp}\MDAC_TYP.EXE');
      if not FileExists(mdacPath) then begin
        isxdl_AddFile(mdacURL, mdacPath);
        downloadNeeded := true;
      end;
    end;
    SetIniString('install', 'mdac', mdacPath, ExpandConstant('{tmp}\dep.ini'));
  end;

  // Check for required Jet installation
  // Jet is not included in MDAC 2.5+.  It will be needed on NT4 installations.
  if (not exclusiveNeeded) and (not RegKeyExists(HKLM, 'Software\Microsoft\Jet\4.0')) then begin
    memoDependenciesNeeded := memoDependenciesNeeded + '      Jet 4.0 Database Components' #13;
    jetPath := '\dependencies\Jet40Sp3_Comp.exe';
    if not FileExists(jetPath) then begin
      jetPath := ExpandConstant('{tmp}\Jet40Sp3_Comp.exe');
      if not FileExists(jetPath) then begin
        isxdl_AddFile(jetURL, jetPath);
        downloadNeeded := true;
      end;
    end;
    SetIniString('install', 'jet', jetPath, ExpandConstant('{tmp}\dep.ini'));
  end;

  // Check for required netfx20 installation
  if (not exclusiveNeeded) and (not RegKeyExists(HKLM, 'Software\Microsoft\.NETFramework\v2.0.50727')) then begin
    memoDependenciesNeeded := memoDependenciesNeeded + '      .NET Framework 2.0' #13;
    dotnetRedistPath := '\dependencies\dotnetredist20.exe';
    if not FileExists(dotnetRedistPath) then begin
      dotnetRedistPath := ExpandConstant('{tmp}\dotnetredist20.exe');
      if not FileExists(dotnetRedistPath) then begin
        isxdl_AddFile(dotnetRedistURL20, dotnetRedistPath);
        downloadNeeded := true;
      end;
    end;
    SetIniString('install', 'dotnetRedist20', dotnetRedistPath, ExpandConstant('{tmp}\dep.ini'));
  end;

end;


function NextButtonClick(CurPage: Integer): Boolean;
var
  hWnd: Integer;
  ResultCode: Integer;
  
begin
  Result := true;

  if CurPage = wpReady then begin

    hWnd := StrToInt(ExpandConstant('{wizardhwnd}'));

    // don't try to init isxdl if it's not needed because it will error on < ie 3
    if downloadNeeded then
      if isxdl_DownloadFiles(hWnd) = 0 then Result := false;


      if Exec(ExpandConstant('net.exe'), 'stop {#MyAppName}', '', SW_HIDE,
       ewWaitUntilTerminated, ResultCode) then
    begin
      // handle success if necessary; ResultCode contains the exit code
    end
    else begin
     // handle failure if necessary; ResultCode contains the error code
    end;
  end;
end;

function ShouldSkipPage(CurPage: Integer): Boolean;
begin
  if (CurPage = wpSelectDir) and exclusiveNeeded then Result := true;
end;

function UpdateReadyMemo(Space, NewLine, MemoUserInfoInfo, MemoDirInfo, MemoTypeInfo, MemoComponentsInfo, MemoGroupInfo, MemoTasksInfo: String): String;
var
  s: string;

begin
  if memoDependenciesNeeded <> '' then s := s + 'benötigte Abhängigkeiten:' + NewLine + memoDependenciesNeeded + NewLine;
  s := s + MemoDirInfo + NewLine + NewLine;

  Result := s
end;
