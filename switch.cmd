@echo off
REM This script switches between Java versions on Windows.
REM Adjust the paths below to match your installed JDKs.

REM Check if the script is run as an administrator
net session >nul 2>&1
if errorlevel 1 (
    echo This script needs to be run as an administrator.
    exit /b 1
)

REM Define the paths to your Java installations
set JAVA_8=C:\Program Files\Eclipse Adoptium\jdk-8.0.504.1-hotspot
set JAVA_11=C:\Program Files\Eclipse Adoptium\jdk-11.0.32.1-hotspot
set JAVA_17=C:\Program Files\Eclipse Adoptium\jdk-17.0.20.1-hotspot
set JAVA_21=C:\Program Files\Eclipse Adoptium\jdk-21.0.12.1-hotspot
set JAVA_25=C:\Program Files\Eclipse Adoptium\jdk-25.0.4.1-hotspot

REM Prompt the user to select a Java version
echo Select a Java version to switch to:
echo 1) Java 8
echo 2) Java 11
echo 3) Java 17
echo 4) Java 21
echo 5) Java 25

set /p choice=Enter your choice (1/2/3/4/5): 

REM Set JAVA_HOME based on the user's input
if "%choice%"=="1" (
    echo Switching to Java 8...
    set JAVA_HOME=%JAVA_8%
) else if "%choice%"=="2" (
    echo Switching to Java 11...
    set JAVA_HOME=%JAVA_11%
) else if "%choice%"=="3" (
    echo Switching to Java 17...
    set JAVA_HOME=%JAVA_17%
) else if "%choice%"=="4" (
    echo Switching to Java 21...
    set JAVA_HOME=%JAVA_21%
) else if "%choice%"=="5" (
    echo Switching to Java 25...
    set JAVA_HOME=%JAVA_25%
) else (
    echo Invalid choice. Please run the script again and choose a valid option.
    exit /b 1
)

REM Update the PATH to include the chosen Java version
set PATH=%JAVA_HOME%\bin;%PATH%

REM Show the currently active Java version
echo Java version after switch:
"%JAVA_HOME%\bin\java" -version
