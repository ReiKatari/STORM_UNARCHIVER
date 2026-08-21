@echo off
cd /d "%~dp0"
echo Starting STORM UNARCHIVER...
dotnet run --project Sources\StormUnarchiver\StormUnarchiver.csproj
pause
