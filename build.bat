@echo off
setlocal
CALL "%VS90COMNTOOLS%\vsvars32.bat"

cd src
ren "My Project\AssemblyInfo.vb" "AssemblyInfo.tmp"
..\installer\svn\subwcrev.exe ..\ "My Project\AssemblyInfo.svn" "My Project\AssemblyInfo.vb"
msbuild.exe speedcs.sln /t:Rebuild /v:m /p:Configuration=Release /p:OutDir=bin\Debug\
del "My Project\AssemblyInfo.vb"
ren "My Project\AssemblyInfo.tmp" "AssemblyInfo.vb"
cd ..

del installer\output\speedcs-setup-*.exe /Q

cd installer
svn\subwcrev.exe ..\ speedcs.iss speedcs_svn.iss
bin\iscc.exe speedcs_svn.iss
del speedcs_svn.iss
svn\subwcrev.exe ..\ packsetup.bat packsetup_svn.bat
call packsetup_svn.bat
del packsetup_svn.bat
cd..

:END
pause