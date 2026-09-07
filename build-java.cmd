@echo off

:: Remove the .class files from the output directories
del /f /q out\production\com\pdfjet\*.class
del /f /q out\production\com\pdfjet\fonts\*.class
del /f /q out\production\examples\*.class

:: Create the output directory if it doesn't exist
if not exist "out\production" mkdir "out\production"

:: Compile the PDFjet library
javac -O -encoding utf-8 -Xlint ^
    com\pdfjet\*.java ^
    com\pdfjet\barcodes\*.java ^
    com\pdfjet\pdf417\*.java ^
    com\pdfjet\qrcode\*.java ^
    com\pdfjet\fonts\*.java ^
    com\pdfjet\encryption\*.java ^
    -d out\production

:: Create the JAR file
jar cf PDFjet.jar -C out\production .

:: Compile the Example files (loop from 1 to 50)
for /L %%i in (1,1,50) do (
    if %%i lss 10 (
        javac -O -encoding utf-8 -Xlint -cp PDFjet.jar examples\Example_0%%i.java -d out\production
    ) else (
        javac -O -encoding utf-8 -Xlint -cp PDFjet.jar examples\Example_%%i.java -d out\production
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
