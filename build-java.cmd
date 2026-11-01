@echo off
:: The folder of this script, which has the files the examples read, from wherever it is run.
cd /d "%~dp0"

:: All the classes, of the subpackages too, so that a class that is removed is
:: not left in PDFjet.jar. 2>nul keeps rmdir quiet when there are none, as on
:: the first build.
rmdir /s /q out\production 2>nul
mkdir out\production

:: --release 8 builds Java 8 class files, so PDFjet.jar and the examples run on
:: Java 8 and later whichever JDK builds them, and rejects any API newer than
:: Java 8. Java 8's javac has no --release option and builds Java 8 class files
:: anyway, so the option is left out there.
set RELEASE=--release 8
javac --release 8 -version >nul 2>&1 || set RELEASE=

:: Compile the PDFjet library
:: -Werror fails the build on any warning and then writes no class files
javac -O -encoding utf-8 %RELEASE% -Xlint -Xlint:-options -Werror ^
    com\pdfjet\*.java ^
    com\pdfjet\barcodes\*.java ^
    com\pdfjet\pdf417\*.java ^
    com\pdfjet\qrcode\*.java ^
    com\pdfjet\datamatrix\*.java ^
    com\pdfjet\internal\*.java ^
    com\pdfjet\fonts\*.java ^
    com\pdfjet\encryption\*.java ^
    -d out\production

:: Create the JAR file
jar cf PDFjet.jar -C out\production .

:: Compile the Example files (loop from 1 to 50)
for /L %%i in (1,1,50) do (
    if %%i lss 10 (
        javac -O -encoding utf-8 %RELEASE% -Xlint -Xlint:-options -Werror -cp PDFjet.jar examples\Example_0%%i.java -d out\production
    ) else (
        javac -O -encoding utf-8 %RELEASE% -Xlint -Xlint:-options -Werror -cp PDFjet.jar examples\Example_%%i.java -d out\production
    )
)

:: Run the Example files (loop from 1 to 50)
for /L %%i in (1,1,50) do (
    if %%i lss 10 (
        java -cp .;PDFjet.jar;out\production examples.Example_0%%i
    ) else (
        java -cp .;PDFjet.jar;out\production examples.Example_%%i
    )
)
