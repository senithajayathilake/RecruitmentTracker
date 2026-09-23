@echo off
cd /d "%~dp0"
set "PLAYWRIGHT_USE_EDGE=1"
node "node_modules\@playwright\test\cli.js" test
echo.
pause
