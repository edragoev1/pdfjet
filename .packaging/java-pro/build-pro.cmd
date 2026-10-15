@echo off
REM Compiles the examples of PDFjet Pro against PDFjet.jar, which has PDFjet
REM Pro in it, and runs them.

if exist out-pro rmdir /s /q out-pro
mkdir out-pro
javac -encoding utf-8 -cp PDFjet.jar examples\pro\*.java -d out-pro || exit /b 1

for %%e in (SignDocument WriteInvoice DrawSwissQRBill DrawEPCQRCode) do java -cp PDFjet.jar;out-pro %%e || exit /b 1
