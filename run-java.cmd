@echo off
REM Windows batch script for building and running Java examples

REM Check if argument is provided
if "%1"=="" (
    echo Please provide an example number:
    echo run-java.cmd 33
    exit /b 1
)

REM Very important!!
call clean.cmd

REM Create output directory if it doesn't exist
if not exist "out\production" mkdir "out\production"

REM Compile the PDFjet library
javac -O -encoding utf-8 -Xlint ^
    com\pdfjet\*.java ^
    com\pdfjet\barcodes\*.java ^
    com\pdfjet\pdf417\*.java ^
    com\pdfjet\qrcode\*.java ^
    com\pdfjet\corefonts\*.java ^
    com\pdfjet\fonts\*.java ^
    com\pdfjet\encryption\*.java ^
    -d out\production

REM Compile and run the Example program
javac -encoding utf-8 -Xlint -cp out\production examples\Example_%1.java -d out\production
java -cp out\production examples.Example_%1

REM Open the resulting PDF using the default PDF viewer
start Example_%1.pdf
