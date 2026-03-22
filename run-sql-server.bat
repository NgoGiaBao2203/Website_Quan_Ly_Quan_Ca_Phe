@echo off
echo Checking Docker status...

docker info >nul 2>&1
IF %ERRORLEVEL% NEQ 0 (
    echo.
    echo Docker Desktop is NOT running.
    echo Please start Docker Desktop first.
    pause
    exit /b
)

echo Docker is running.

set CONTAINER_NAME=sqlserver2022
set SA_PASSWORD=YourStrong!Passw0rd
set SQL_PORT=1433

echo Checking if container "%CONTAINER_NAME%" exists...

docker inspect %CONTAINER_NAME% >nul 2>&1
IF %ERRORLEVEL% EQU 0 (
    echo Container exists.

    for /f "delims=" %%i in ('docker inspect -f "{{.State.Running}}" %CONTAINER_NAME%') do set RUNNING=%%i

    if "%RUNNING%"=="true" (
        echo Container is already running.
    ) else (
        echo Starting existing container...
        docker start %CONTAINER_NAME%
        echo Container started.
    )

) ELSE (
    echo Container does not exist. Creating new one...

    docker run -e "ACCEPT_EULA=Y" ^
               -e "SA_PASSWORD=%SA_PASSWORD%" ^
               -p %SQL_PORT%:1433 ^
               --name %CONTAINER_NAME% ^
               -d mcr.microsoft.com/mssql/server:2022-latest

    echo New SQL Server container created and started.
)

echo =====================================
echo SQL Server started successfully!
echo =====================================

pause