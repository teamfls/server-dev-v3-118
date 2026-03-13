@echo off
title Point Blank 
echo ========================================
echo  Login Point Blank, Auto Create Account
echo ========================================
echo.
set /p USERNAME="Enter username: "

REM Input password dengan bintang (PowerShell sudah ada prompt sendiri)
call :InputPassword PASSWORD

echo.
echo Login successful
echo Starting PointBlank.exe with username and password...
echo.
start PointBlank.exe %USERNAME% %PASSWORD%
exit /b

:InputPassword
setlocal EnableExtensions EnableDelayedExpansion
set "psCommand=powershell -Command "$pword = read-host 'Enter password' -AsSecureString ; ^
    $BSTR=[System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($pword); ^
    [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($BSTR)""

for /f "usebackq delims=" %%p in (`%psCommand%`) do set "password=%%p"
endlocal & set "%~1=%password%"
exit /b