@echo off
cd /d "%~dp0"
echo Starting STORM UNARCHIVER...
dotnet run --project StormUnarchiver\StormUnarchiver.csproj
pause
