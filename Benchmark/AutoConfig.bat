@ECHO off

call AutoConfigParams.bat

SET CMDHOME=%~dp0.

cd %CMDHOME%

SETLOCAL ENABLEDELAYEDEXPANSION

for /r "%CMDHOME%" %%P in ("*.tpl") do (
	SET TPLFilePath="%%~fP"
	SET TPLFilePathInter1=!TPLFilePath:~0,-5!"
	SET TPLFilePathInter2=!TPLFilePathInter1:CLOUDFILENAME=%CLOUDFILENAME%!
	SET TPLFilePathInter3=!TPLFilePathInter2:CONDUCTORDOMAINCLOUDAPP=%CONDUCTORDOMAINCLOUDAPP%!
	SET TPLFilePathOrig=!TPLFilePathInter3:DOMAINCLOUDAPP=%DOMAINCLOUDAPP%!
	
	cscript /nologo ReplaceFile.vbs !TPLFilePath! "CLOUDFILENAME" "%CLOUDFILENAME%" !TPLFilePathOrig!
	cscript /nologo ReplaceFile.vbs !TPLFilePathOrig! "CONDUCTORDOMAINCLOUDAPP" "%CONDUCTORDOMAINCLOUDAPP%" !TPLFilePathOrig!
	cscript /nologo ReplaceFile.vbs !TPLFilePathOrig! "DOMAINCLOUDAPP" "%DOMAINCLOUDAPP%" !TPLFilePathOrig!
	cscript /nologo ReplaceFile.vbs !TPLFilePathOrig! "MAINSTORAGEACCOUNTKEY" "%MAINSTORAGEACCOUNTKEY%" !TPLFilePathOrig!
	cscript /nologo ReplaceFile.vbs !TPLFilePathOrig! "MAINSTORAGEACCOUNT" "%MAINSTORAGEACCOUNT%" !TPLFilePathOrig!
	cscript /nologo ReplaceFile.vbs !TPLFilePathOrig! "GRAINSTORAGEACCOUNTKEY" "%GRAINSTORAGEACCOUNTKEY%" !TPLFilePathOrig!
	cscript /nologo ReplaceFile.vbs !TPLFilePathOrig! "GRAINSTORAGEACCOUNT" "%GRAINSTORAGEACCOUNT%" !TPLFilePathOrig!
	cscript /nologo ReplaceFile.vbs !TPLFilePathOrig! "INDEXINGSTORAGEACCOUNTKEY" "%INDEXINGSTORAGEACCOUNTKEY%" !TPLFilePathOrig!
	cscript /nologo ReplaceFile.vbs !TPLFilePathOrig! "INDEXINGSTORAGEACCOUNT" "%INDEXINGSTORAGEACCOUNT%" !TPLFilePathOrig!
	cscript /nologo ReplaceFile.vbs !TPLFilePathOrig! "INDEXINGWORKFLOWSTORAGEACCOUNTKEY" "%INDEXINGWORKFLOWSTORAGEACCOUNTKEY%" !TPLFilePathOrig!
	cscript /nologo ReplaceFile.vbs !TPLFilePathOrig! "INDEXINGWORKFLOWSTORAGEACCOUNT" "%INDEXINGWORKFLOWSTORAGEACCOUNT%" !TPLFilePathOrig!
	cscript /nologo ReplaceFile.vbs !TPLFilePathOrig! "WINDOWSAZUREPLUGINTUMBPRINTCONDUCTOR" "%WINDOWSAZUREPLUGINTUMBPRINTCONDUCTOR%" !TPLFilePathOrig!
	cscript /nologo ReplaceFile.vbs !TPLFilePathOrig! "REMOTEDESKTOPUSERCONDUCTOR" "%REMOTEDESKTOPUSERCONDUCTOR%" !TPLFilePathOrig!
	cscript /nologo ReplaceFile.vbs !TPLFilePathOrig! "REMOTEDESKTOPPASSCONDUCTOR" "%REMOTEDESKTOPPASSCONDUCTOR%" !TPLFilePathOrig!
	cscript /nologo ReplaceFile.vbs !TPLFilePathOrig! "WINDOWSAZUREPLUGINTUMBPRINT" "%WINDOWSAZUREPLUGINTUMBPRINT%" !TPLFilePathOrig!
	cscript /nologo ReplaceFile.vbs !TPLFilePathOrig! "REMOTEDESKTOPUSER" "%REMOTEDESKTOPUSER%" !TPLFilePathOrig!
	cscript /nologo ReplaceFile.vbs !TPLFilePathOrig! "REMOTEDESKTOPPASS" "%REMOTEDESKTOPPASS%" !TPLFilePathOrig!
	cscript /nologo ReplaceFile.vbs !TPLFilePathOrig! "DOCUMENTDBURL" "%DOCUMENTDBURL%" !TPLFilePathOrig!
	cscript /nologo ReplaceFile.vbs !TPLFilePathOrig! "DOCUMENTDBKEY" "%DOCUMENTDBKEY%" !TPLFilePathOrig!
	cscript /nologo ReplaceFile.vbs !TPLFilePathOrig! "DOCUMENTDBOFFERTYPEVERSION" "%DOCUMENTDBOFFERTYPEVERSION%" !TPLFilePathOrig!
	cscript /nologo ReplaceFile.vbs !TPLFilePathOrig! "DOCUMENTDBOFFERTYPE" "%DOCUMENTDBOFFERTYPE%" !TPLFilePathOrig!
	cscript /nologo ReplaceFile.vbs !TPLFilePathOrig! "DOCUMENTDBINDEXINGMODE" "%DOCUMENTDBINDEXINGMODE%" !TPLFilePathOrig!
	
	echo Handled !TPLFilePathOrig! ...
)

ENDLOCAL
