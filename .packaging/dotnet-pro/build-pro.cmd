@echo off
REM Builds the example projects of PDFjet Pro against PDFjet.dll, which
REM has PDFjet Pro in it, and runs them.

for %%e in (SignDocument WriteInvoice DrawSwissQRBill DrawEPCQRCode HyphenateText) do dotnet run --project examples\pro\%%e -c release || exit /b 1

REM SignLongTerm signs with the test PKI of examples\pro\ltv, whose OCSP
REM responder, CRLs and time stamp authority ltv-server.py answers for on
REM 127.0.0.1:19180, with Python 3 and openssl on the PATH. Start the server
REM in a window of its own, in this folder, and run the example in another:
echo.
echo SignLongTerm needs the test server of examples\pro\ltv, with Python 3 and openssl.
echo Start it in a window of its own, in this folder:
echo     python examples\pro\ltv\ltv-server.py examples\pro\ltv
echo and then: dotnet run --project examples\pro\SignLongTerm -c release
