@echo off
setlocal
for /f "tokens=2" %%V in ('findstr /b "m_EditorVersion:" "%~dp0unity\D22Game\ProjectSettings\ProjectVersion.txt"') do set "D22_VERSION=%%V"
if not defined UNITY_EDITOR set "UNITY_EDITOR=%ProgramFiles%\Unity\Hub\Editor\%D22_VERSION%\Editor\Unity.exe"
if not exist "%UNITY_EDITOR%" (
  echo Install Unity %D22_VERSION% in Unity Hub, or set UNITY_EDITOR to Unity.exe.
  pause
  exit /b 1
)
start "D22 Unity" "%UNITY_EDITOR%" -projectPath "%~dp0unity\D22Game"
