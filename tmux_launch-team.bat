@echo off
set "WIN_PATH=%~dp0"
if "%WIN_PATH:~-1%"=="\" set "WIN_PATH=%WIN_PATH:~0,-1%"

wsl -d Ubuntu -e bash -c "export CLAUDE_CODE_MODEL=claude-4-5-sonnet && cd \"$(wslpath '%WIN_PATH%')\" && bash tmux_launch-team.sh"
pause