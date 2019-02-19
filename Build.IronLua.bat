@ECHO OFF
SETLOCAL
SET PATH=%PATH%;%WINDIR%\Microsoft.NET\Framework\v4.0.30319;%WINDIR%\Microsoft.NET\Framework\v3.5

SET TARGET=%1
SET CONFIGURATION=Debug

:getopts
IF "%1"=="" (GOTO :defaultTarget) else (goto :main)
:defaultTarget
SET TARGET=Build
ECHO   Using default target: %TARGET%

:main
msbuild Build.IronLua.proj /t:"%TARGET%" /p:BaseConfiguration=%CONFIGURATION% /verbosity:normal /nologo
