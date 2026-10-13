@echo off
REM Compiles the examples of PDFjet Pro against PDFjet.jar and PDFjet-Sign.jar
REM and runs them.

if exist out-pro rmdir /s /q out-pro
mkdir out-pro
javac -encoding utf-8 -cp PDFjet.jar;PDFjet-Sign.jar examples\pro\*.java -d out-pro || exit /b 1

for %%e in (SignDocument WriteInvoice DrawSwissQRBill) do java -cp PDFjet.jar;PDFjet-Sign.jar;out-pro %%e || exit /b 1
