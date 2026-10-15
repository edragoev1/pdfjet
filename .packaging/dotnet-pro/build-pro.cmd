@echo off
REM Builds the example projects of PDFjet Pro against PDFjet.dll, which
REM has PDFjet Pro in it, and runs them.

for %%e in (SignDocument WriteInvoice DrawSwissQRBill DrawEPCQRCode) do dotnet run --project examples\pro\%%e -c release || exit /b 1
