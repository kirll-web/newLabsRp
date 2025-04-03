start "stop" stop.bat
timeout /t 1 /nobreak >nul 
start "start" start.bat
timeout /t 20 /nobreak >nul 
taskkill /f /im cmd.exe /fi "WINDOWTITLE eq stop*"  >nul 2>&1
taskkill /f /im cmd.exe /fi "WINDOWTITLE eq start*" >nul 2>&1
