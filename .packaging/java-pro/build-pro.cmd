@echo off
REM Compiles the examples of PDFjet Pro against PDFjet.jar, which has PDFjet
REM Pro in it, and runs them.

if exist out-pro rmdir /s /q out-pro
mkdir out-pro
javac -encoding utf-8 -cp PDFjet.jar examples\pro\*.java -d out-pro || exit /b 1

for %%e in (SignDocument WriteInvoice DrawSwissQRBill DrawEPCQRCode HyphenateText) do java -cp PDFjet.jar;out-pro %%e || exit /b 1

REM SignLongTerm signs with the test PKI of examples\pro\ltv, whose OCSP
REM responder, CRLs and time stamp authority ltv-server.py answers for on
REM 127.0.0.1:19180, with Python 3 and openssl on the PATH. Start the server
REM in a window of its own, in this folder, and run the example in another:
echo.
echo SignLongTerm needs the test server of examples\pro\ltv, with Python 3 and openssl.
echo Start it in a window of its own, in this folder:
echo     python examples\pro\ltv\ltv-server.py examples\pro\ltv
echo and then: java -cp PDFjet.jar;out-pro SignLongTerm
